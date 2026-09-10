using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    // Places hidden conduits and VE pipes under walls during layout generation.
    //
    // PURPOSE:
    // Creates a station-wide power and resource network by placing hidden infrastructure
    // under all walls in the generated structure. This ensures all electrical devices
    // are connected to power sources (LifeSupportUnits) and VE pipe networks are unified.
    //
    // TECHNICAL APPROACH:
    // - Iterates room rects' edge cells (walls are on rect edges, not interior)
    // - O(perimeter) instead of O(area) - efficient for large structures
    // - Places HiddenConduit under walls/doors (vanilla power network)
    // - Also places any VE hidden pipes from HiddenPipeHelper
    //
    // LEARNING NOTE (Room Rect Edges):
    // Room rects INCLUDE their walls. The edge cells of each rect correspond to
    // the room's walls, so we iterate just the edges instead of checking every cell.
    // For a 20x20 room: edges = 76 cells vs interior = 400 cells (5x fewer checks).
    public static class LayoutConduitPlacer
    {
        // Places hidden conduits (and VE hidden pipes) under all wall and door cells.
        //
        // BEHAVIOR:
        // - HiddenConduit under all walls and doors (invisible, clean aesthetics)
        // - Also spawns any VE hidden pipes at same locations
        // - Tracks processed cells to avoid duplicates at shared walls
        // map: The map being generated
        // sketch: The LayoutStructureSketch containing structure data
        // Returns: Number of conduit positions placed
        public static int PlaceHiddenConduits(Map map, LayoutStructureSketch sketch)
        {
            StructureLayout layout = sketch.structureLayout;

            // Iterate edge cells only (walls are on edges, not interior). Rooms share
            // walls, so the same cell can come up more than once; the cell pass dedups.
            IEnumerable<IntVec3> EdgeCells()
            {
                foreach (LayoutRoom room in layout.Rooms)
                {
                    if (room.rects == null)
                        continue;

                    // Corridors have multiple rects
                    foreach (CellRect rect in room.rects)
                    {
                        foreach (IntVec3 edgeCell in rect.EdgeCells)
                            yield return edgeCell;
                    }
                }
            }

            return PlaceHiddenConduits(map, EdgeCells(), includeHiddenPipes: true);
        }

        // Places a HiddenConduit under every wall and door cell among the candidate
        // cells (other cells are skipped, so a whole rect can be passed). Optionally also
        // spawns the VE hidden pipes. Used by the layout path above (room edge cells) and
        // by GenStep_PlaceWallConduits on prefab-built stations that have no
        // LayoutStructureSketch (whole SpawnRect).
        // Returns: Number of conduit positions placed
        public static int PlaceHiddenConduits(Map map, IEnumerable<IntVec3> candidateCells, bool includeHiddenPipes)
        {
            IReadOnlyList<ThingDef> hiddenPipeDefs = includeHiddenPipes
                ? HiddenPipeHelper.GetSupportedHiddenPipeDefs()
                : System.Array.Empty<ThingDef>();

            int placedCount = 0;

            // Track cells we've already processed (rooms can share walls)
            HashSet<IntVec3> processedCells = new HashSet<IntVec3>();

            foreach (IntVec3 cell in candidateCells)
            {
                if (!processedCells.Add(cell))
                    continue;

                // Bounds check (defensive)
                if (!cell.InBounds(map))
                    continue;

                // Check what's at this cell
                Building edifice = cell.GetEdifice(map);
                if (edifice == null)
                    continue;

                // Only place conduits under walls and doors
                if (!edifice.def.IsDoor && edifice.def.building?.isPlaceOverableWall != true)
                    continue;

                // A prefab may already carry a transmitter here (nothing in BTG's or VGE2's
                // does today, but a duplicate would just be a wasted thing)
                if (HasConduit(map, cell))
                    continue;

                // Create and spawn the conduit
                Thing conduit = ThingMaker.MakeThing(Things.HiddenConduit);
                GenSpawn.Spawn(conduit, cell, map);
                placedCount++;

                // Also spawn any VE hidden pipes at this location
                foreach (ThingDef hiddenPipeDef in hiddenPipeDefs)
                {
                    Thing pipe = ThingMaker.MakeThing(hiddenPipeDef);
                    GenSpawn.Spawn(pipe, cell, map);
                }
            }

            return placedCount;
        }

        private static bool HasConduit(Map map, IntVec3 cell)
        {
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing.def == Things.HiddenConduit)
                    return true;
            }
            return false;
        }
    }
}
