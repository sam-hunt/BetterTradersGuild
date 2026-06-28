using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.MapGeneration;
using RimWorld;
using Verse;

namespace BetterTradersGuild.LayoutWorkers.Settlement
{
    // Extends VE pipe networks from the main settlement structure out to external
    // landing pads so docked vessels can be resupplied (chemfuel, nutrient paste,
    // oxygen, astrofuel).
    //
    // CONNECTION MODEL:
    // The structure's fluid network lives as hidden (underground) pipes laid under
    // every wall/door by LayoutConduitPlacer. To connect a pad we trace a path from
    // the pad back to that network and lay pipe along it. The path's GOAL is the
    // first existing hidden pipe, NOT "any wall": some walls (e.g. vanilla NarrowHalls
    // partitions, which aren't part of the structure sketch) never get a hidden pipe,
    // so stopping at the nearest wall could terminate beside dead infrastructure and
    // never join the network.
    //
    // PATHING IS TERRAIN-BASED, NOT PAWN-BASED:
    // Pipes run *under* walls and doors, so edifices do not block the path. The only
    // obstacle is impassable terrain (Space). This lets a single A* walk the pad's
    // OrbitalPlatform strip, dip under a pipe-less perimeter wall if needed, and
    // continue through the interior until it reaches the live network.
    //
    // COSMETICS:
    // Visible pipes are laid on the exterior approach; once the path crosses a wall
    // into the structure we switch to the hidden (invisible) variants so no pipe is
    // routed visibly through the interior.
    //
    // LEARNING NOTE (Why custom A*?):
    // RimWorld's PathFinder is pawn-centric (TraverseParms, doors, danger), FloodFiller
    // is BFS with no parent tracking, and GenClosest/Reachability only answer
    // nearest/reachable. None support terrain-constrained pathing with full path
    // collection, which is what we need. Manhattan heuristic toward the structure
    // center keeps the search driving inward at the perimeter.
    //
    // LEARNING NOTE (External landing pads):
    // Pads are NOT part of LayoutStructureSketch - vanilla GenStep_OrbitalPlatform
    // generates them separately, so we detect them post-generation via beacon markers.
    public static class LandingPadPipeExtender
    {
        // Extends pipes from the structure's hidden network to all external landing pads.
        public static void ExtendPipesToLandingPads(Map map, LayoutStructureSketch sketch)
        {
            if (Terrains.OrbitalPlatform == null) return;

            // Visible pipes for the exterior approach.
            List<ThingDef> visiblePipeDefs = BuildVisiblePipeDefsList();
            if (visiblePipeDefs.Count == 0) return;

            // Hidden pipes are what the under-wall network is actually made of. They are
            // all co-located (LayoutConduitPlacer stacks every def at the same cell), so
            // the first one is a sufficient stop-goal for the path search and the full
            // set is what we lay once inside the structure.
            IReadOnlyList<ThingDef> hiddenPipeDefs = HiddenPipeHelper.GetSupportedHiddenPipeDefs();
            if (hiddenPipeDefs.Count == 0) return;
            ThingDef networkPipeDef = hiddenPipeDefs[0];

            if (sketch?.structureLayout == null) return;

            CellRect structureRect = sketch.structureLayout.container;

            List<LandingPadDetector.LandingPadInfo> landingPads = LandingPadDetector.DetectOutsideRect(map, structureRect);
            if (landingPads.Count == 0) return;

            // Track all placed positions so pads sharing path sections don't double up.
            HashSet<IntVec3> placedCells = new HashSet<IntVec3>();

            foreach (LandingPadDetector.LandingPadInfo pad in landingPads)
            {
                List<IntVec3> path = FindTerrainPathToStructure(map, pad, structureRect, networkPipeDef);
                if (path.Count > 0)
                {
                    PlacePipesAlongPath(map, path, visiblePipeDefs, hiddenPipeDefs, placedCells);
                }
            }
        }

        // Builds list of visible VE pipe ThingDefs for installed mods.
        // Uses DefRefs/Things.cs for centralized def resolution.
        // These are used on the exterior approach (before the path enters the structure).
        private static List<ThingDef> BuildVisiblePipeDefsList()
        {
            List<ThingDef> visiblePipes = new List<ThingDef>();

            // VE Chemfuel visible pipes
            if (Things.VCHE_ChemfuelPipe != null)
                visiblePipes.Add(Things.VCHE_ChemfuelPipe);

            // VE Nutrient Paste visible pipes
            if (Things.VNPE_NutrientPastePipe != null)
                visiblePipes.Add(Things.VNPE_NutrientPastePipe);

            // VE Gravships visible pipes
            if (Things.VGE_OxygenPipe != null)
                visiblePipes.Add(Things.VGE_OxygenPipe);
            if (Things.VGE_AstrofuelPipe != null)
                visiblePipes.Add(Things.VGE_AstrofuelPipe);

            return visiblePipes;
        }

        // Finds a path from the landing pad edge to the structure's hidden pipe network.
        private static List<IntVec3> FindTerrainPathToStructure(
            Map map,
            LandingPadDetector.LandingPadInfo padInfo,
            CellRect structureRect,
            ThingDef networkPipeDef)
        {
            IntVec3 structureCenter = structureRect.CenterCell;

            // Start from the pad edge facing the structure.
            IntVec3 startCell = FindClosestPadEdgeToStructure(padInfo.BoundingRect, structureCenter);

            return FindTerrainPathAStar(map, startCell, structureCenter, networkPipeDef, maxIterations: 5000);
        }

        // Finds the cell on the pad's bounding rect edge closest to the structure center.
        private static IntVec3 FindClosestPadEdgeToStructure(CellRect padRect, IntVec3 structureCenter)
        {
            // Check which direction the structure is from the pad
            IntVec3 padCenter = padRect.CenterCell;
            int dx = structureCenter.x - padCenter.x;
            int dz = structureCenter.z - padCenter.z;

            // Pick edge based on direction
            if (System.Math.Abs(dx) > System.Math.Abs(dz))
            {
                // Structure is more horizontal (left/right)
                if (dx > 0)
                {
                    // Structure is to the right, use right edge
                    return new IntVec3(padRect.maxX, 0, padRect.CenterCell.z);
                }
                else
                {
                    // Structure is to the left, use left edge
                    return new IntVec3(padRect.minX, 0, padRect.CenterCell.z);
                }
            }
            else
            {
                // Structure is more vertical (up/down)
                if (dz > 0)
                {
                    // Structure is above, use top edge
                    return new IntVec3(padRect.CenterCell.x, 0, padRect.maxZ);
                }
                else
                {
                    // Structure is below, use bottom edge
                    return new IntVec3(padRect.CenterCell.x, 0, padRect.minZ);
                }
            }
        }

        // A* from the pad toward the structure, stopping at the first existing hidden pipe.
        //
        // Traversal is terrain-based: any cell whose terrain is not impassable (i.e. not
        // Space) is walkable, regardless of edifices, because pipes run under walls/doors.
        // The Manhattan heuristic aims at the structure center to drive the search inward.
        private static List<IntVec3> FindTerrainPathAStar(
            Map map,
            IntVec3 startCell,
            IntVec3 targetCenter,
            ThingDef networkPipeDef,
            int maxIterations = 5000)
        {
            TerrainDef terrainDef = Terrains.OrbitalPlatform;

            // If the start cell isn't on platform terrain, search nearby rings for the pad strip.
            if (!startCell.InBounds(map) || map.terrainGrid.TerrainAt(startCell) != terrainDef)
            {
                IntVec3? foundStart = null;
                for (int radius = 1; radius <= 5 && foundStart == null; radius++)
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(startCell, radius, false))
                    {
                        if (cell.InBounds(map) && map.terrainGrid.TerrainAt(cell) == terrainDef)
                        {
                            foundStart = cell;
                            break;
                        }
                    }
                }

                if (foundStart == null)
                    return new List<IntVec3>();

                startCell = foundStart.Value;
            }

            Dictionary<IntVec3, IntVec3> cameFrom = new Dictionary<IntVec3, IntVec3>();
            Dictionary<IntVec3, int> gScore = new Dictionary<IntVec3, int>(); // Cost from start
            Dictionary<IntVec3, int> fScore = new Dictionary<IntVec3, int>(); // gScore + heuristic

            HashSet<IntVec3> openSet = new HashSet<IntVec3>();
            HashSet<IntVec3> closedSet = new HashSet<IntVec3>();

            gScore[startCell] = 0;
            fScore[startCell] = ManhattanDistance(startCell, targetCenter);
            openSet.Add(startCell);
            cameFrom[startCell] = startCell;

            int iterations = 0;
            IntVec3? reachedCell = null;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                IntVec3 current = GetLowestFScore(openSet, fScore);
                openSet.Remove(current);
                closedSet.Add(current);

                // Goal: a cell on or beside the existing under-wall pipe network.
                if (IsAtOrAdjacentToPipe(map, current, networkPipeDef))
                {
                    reachedCell = current;
                    break;
                }

                foreach (IntVec3 neighbor in CardinalNeighbors(current))
                {
                    if (!neighbor.InBounds(map))
                        continue;
                    if (closedSet.Contains(neighbor))
                        continue;
                    // Pipes run under walls/doors; only impassable terrain (Space) blocks them.
                    if (!IsPipeTraversableTerrain(map, neighbor))
                        continue;

                    int tentativeGScore = gScore[current] + 1;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (gScore.TryGetValue(neighbor, out int existingG) && tentativeGScore >= existingG)
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + ManhattanDistance(neighbor, targetCenter);
                }
            }

            // Reconstruct path if we reached the network.
            if (reachedCell == null)
                return new List<IntVec3>();

            List<IntVec3> path = new List<IntVec3>();
            IntVec3 pathCell = reachedCell.Value;

            while (cameFrom.ContainsKey(pathCell) && cameFrom[pathCell] != pathCell)
            {
                path.Add(pathCell);
                pathCell = cameFrom[pathCell];
            }
            path.Add(pathCell); // Add start cell

            path.Reverse();
            return path;
        }

        // Manhattan distance heuristic for A* - sum of absolute differences in x and z.
        // Admissible for grid movement (never overestimates).
        private static int ManhattanDistance(IntVec3 a, IntVec3 b)
        {
            return System.Math.Abs(a.x - b.x) + System.Math.Abs(a.z - b.z);
        }

        // Finds the cell with the lowest fScore in the open set.
        // Simple O(n) scan - suitable for the expected set sizes.
        private static IntVec3 GetLowestFScore(HashSet<IntVec3> openSet, Dictionary<IntVec3, int> fScore)
        {
            IntVec3 best = default;
            int bestScore = int.MaxValue;

            foreach (IntVec3 cell in openSet)
            {
                int score = fScore.TryGetValue(cell, out int f) ? f : int.MaxValue;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = cell;
                }
            }

            return best;
        }

        // True if a pipe can be laid/run through this cell - i.e. its terrain is not
        // impassable (Space). Edifices are intentionally ignored: pipes run under walls.
        private static bool IsPipeTraversableTerrain(Map map, IntVec3 cell)
        {
            TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
            return terrain != null && terrain.passability != Traversability.Impassable;
        }

        // True if this cell contains, or is cardinally adjacent to, an existing pipe of
        // the given def - used to detect arrival at the structure's hidden network.
        private static bool IsAtOrAdjacentToPipe(Map map, IntVec3 cell, ThingDef pipeDef)
        {
            if (HasThingAt(map, cell, pipeDef))
                return true;

            foreach (IntVec3 neighbor in CardinalNeighbors(cell))
            {
                if (!neighbor.InBounds(map))
                    continue;
                if (HasThingAt(map, neighbor, pipeDef))
                    return true;
            }
            return false;
        }

        // Places pipes along a traced path. Visible pipes are used on the exterior
        // approach; once the path crosses a wall into the structure we switch to the
        // hidden (invisible) variants so no pipe is routed visibly through the interior.
        private static int PlacePipesAlongPath(
            Map map,
            List<IntVec3> path,
            IReadOnlyList<ThingDef> visiblePipeDefs,
            IReadOnlyList<ThingDef> hiddenPipeDefs,
            HashSet<IntVec3> placedCells)
        {
            int placedCount = 0;
            bool insideStructure = false;

            foreach (IntVec3 cell in path)
            {
                // Flip to hidden pipes once the path crosses the first wall (impassable edifice).
                // Evaluated before the dedup check so the flip is never skipped on shared cells.
                if (!insideStructure)
                {
                    Building edifice = cell.GetEdifice(map);
                    if (edifice != null && edifice.def.passability == Traversability.Impassable)
                        insideStructure = true;
                }

                // Skip if we already placed here (multiple pads may share path sections).
                if (!placedCells.Add(cell))
                    continue;

                IReadOnlyList<ThingDef> defsToPlace = insideStructure ? hiddenPipeDefs : visiblePipeDefs;
                foreach (ThingDef pipeDef in defsToPlace)
                {
                    // Skip if this pipe type already exists at this cell
                    if (HasThingAt(map, cell, pipeDef))
                        continue;

                    Thing pipe = ThingMaker.MakeThing(pipeDef);
                    GenSpawn.Spawn(pipe, cell, map);
                }
                placedCount++;
            }

            return placedCount;
        }

        // Checks if a cell already has a thing of the specified type.
        private static bool HasThingAt(Map map, IntVec3 cell, ThingDef def)
        {
            List<Thing> thingsAtCell = map.thingGrid.ThingsListAt(cell);
            foreach (Thing thing in thingsAtCell)
            {
                if (thing.def == def)
                    return true;
            }
            return false;
        }

        // Gets the four cardinal neighbors of a cell (N, S, E, W).
        // RimWorld's GenAdj.CellsAdjacentCardinal expects a Thing, so we use this for IntVec3.
        private static IEnumerable<IntVec3> CardinalNeighbors(IntVec3 cell)
        {
            yield return new IntVec3(cell.x, 0, cell.z + 1); // North
            yield return new IntVec3(cell.x, 0, cell.z - 1); // South
            yield return new IntVec3(cell.x + 1, 0, cell.z); // East
            yield return new IntVec3(cell.x - 1, 0, cell.z); // West
        }
    }
}
