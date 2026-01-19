using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    /// <summary>
    /// Spawns airlock walls with centered VacBarriers inside corridors at blast door exits.
    ///
    /// Algorithm:
    /// 1. For each rect in room.rects, scan edge cells for AncientBlastDoors
    /// 2. For each blast door, determine which cardinal edge it's on
    /// 3. Calculate position several cells inward from the door
    /// 4. If position would block side room doors, move further inward
    /// 5. Spawn a 3-cell wide wall perpendicular to the corridor with centered VacBarrier
    ///
    /// This creates atmospheric airlocks at corridor exits to space, improving
    /// both aesthetics and realism for orbital settlements.
    /// </summary>
    public static class CorridorAirlockSpawner
    {
        /// <summary>
        /// Minimum distance in cells from the blast door to place the airlock wall.
        /// Should be far enough to not block the blast door opening.
        /// </summary>
        private const int MinAirlockDepth = 2;

        /// <summary>
        /// Maximum distance to try placing the airlock before giving up.
        /// Prevents searching too far into the corridor.
        /// </summary>
        private const int MaxAirlockDepth = 8;

        /// <summary>
        /// Width of corridors (excluding walls). Standard orbital corridors are 3 cells wide.
        /// </summary>
        private const int CorridorWidth = 3;

        /// <summary>
        /// Spawns airlock walls at all blast door exits in the corridor.
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="room">The corridor LayoutRoom with potentially multiple rects.</param>
        public static void SpawnAirlocks(Map map, LayoutRoom room)
        {
            if (room.rects == null || room.rects.Count == 0)
                return;

            ThingDef wallDef = Things.OrbitalAncientFortifiedWall;
            ThingDef barrierDef = Things.VacBarrier;

            // Track all blast doors found to avoid duplicates at rect intersections
            HashSet<IntVec3> processedDoors = new HashSet<IntVec3>();

            foreach (CellRect rect in room.rects)
            {
                var blastDoors = FindBlastDoorsOnEdges(map, rect);

                foreach (var doorInfo in blastDoors)
                {
                    if (processedDoors.Contains(doorInfo.Position))
                        continue;

                    processedDoors.Add(doorInfo.Position);
                    SpawnAirlockAtDoor(map, room, doorInfo, wallDef, barrierDef);
                }
            }
        }

        /// <summary>
        /// Information about a blast door's position and the edge it's on.
        /// </summary>
        private struct BlastDoorInfo
        {
            public IntVec3 Position;
            public CardinalEdge Edge;
            public CellRect SourceRect;
        }

        /// <summary>
        /// Cardinal edge of a rect.
        /// </summary>
        private enum CardinalEdge
        {
            North,  // maxZ edge
            South,  // minZ edge
            East,   // maxX edge
            West    // minX edge
        }

        /// <summary>
        /// Finds all AncientBlastDoors on the edges of a rect.
        /// </summary>
        private static List<BlastDoorInfo> FindBlastDoorsOnEdges(Map map, CellRect rect)
        {
            var result = new List<BlastDoorInfo>();

            foreach (IntVec3 cell in rect.EdgeCells)
            {
                if (!cell.InBounds(map))
                    continue;

                // Check for blast door at this cell
                Building edifice = cell.GetEdifice(map);
                if (edifice == null || edifice.def != Things.AncientBlastDoor)
                    continue;

                // Determine which edge this cell is on
                CardinalEdge? edge = DetermineEdge(cell, rect);
                if (!edge.HasValue)
                    continue;

                result.Add(new BlastDoorInfo
                {
                    Position = cell,
                    Edge = edge.Value,
                    SourceRect = rect
                });
            }

            return result;
        }

        /// <summary>
        /// Determines which cardinal edge a cell is on within a rect.
        /// Returns null if the cell is on a corner (ambiguous).
        /// </summary>
        private static CardinalEdge? DetermineEdge(IntVec3 cell, CellRect rect)
        {
            bool onNorth = cell.z == rect.maxZ;
            bool onSouth = cell.z == rect.minZ;
            bool onEast = cell.x == rect.maxX;
            bool onWest = cell.x == rect.minX;

            // Corner cells are ambiguous - skip them
            int edgeCount = (onNorth ? 1 : 0) + (onSouth ? 1 : 0) + (onEast ? 1 : 0) + (onWest ? 1 : 0);
            if (edgeCount != 1)
                return null;

            if (onNorth) return CardinalEdge.North;
            if (onSouth) return CardinalEdge.South;
            if (onEast) return CardinalEdge.East;
            if (onWest) return CardinalEdge.West;

            return null;
        }

        /// <summary>
        /// Spawns an airlock wall at a specific blast door location.
        /// Progressively moves the airlock further inward if it would block side room doors.
        /// </summary>
        private static void SpawnAirlockAtDoor(
            Map map,
            LayoutRoom room,
            BlastDoorInfo doorInfo,
            ThingDef wallDef,
            ThingDef barrierDef)
        {
            // Calculate the direction vector pointing inward from the door
            IntVec3 inwardDir = GetInwardDirection(doorInfo.Edge);

            // Determine wall orientation (perpendicular to corridor direction)
            bool wallIsHorizontal = doorInfo.Edge == CardinalEdge.North || doorInfo.Edge == CardinalEdge.South;

            // Try progressively deeper positions until we find one that doesn't block doors
            for (int depth = MinAirlockDepth; depth <= MaxAirlockDepth; depth++)
            {
                IntVec3 wallCenter = doorInfo.Position + (inwardDir * depth);
                List<IntVec3> wallCells = CalculateWallCells(wallCenter, wallIsHorizontal);

                // Check if this position is valid (within bounds, etc.)
                if (!ValidateWallCells(map, room, wallCells))
                    return;

                // Check if any wall cells would block a side room door
                if (WouldBlockDoor(map, wallCells))
                    continue;

                // Found a valid position - spawn the airlock
                SpawnAirlockWalls(map, wallCells, wallIsHorizontal, wallDef, barrierDef);
                return;
            }
            // Exhausted all depths without finding a valid position
        }

        /// <summary>
        /// Calculates the wall cells for an airlock at a given center position.
        /// </summary>
        private static List<IntVec3> CalculateWallCells(IntVec3 wallCenter, bool wallIsHorizontal)
        {
            List<IntVec3> wallCells = new List<IntVec3>();
            int halfWidth = CorridorWidth / 2;

            for (int offset = -halfWidth; offset <= halfWidth; offset++)
            {
                IntVec3 wallCell;
                if (wallIsHorizontal)
                    wallCell = new IntVec3(wallCenter.x + offset, 0, wallCenter.z);
                else
                    wallCell = new IntVec3(wallCenter.x, 0, wallCenter.z + offset);

                wallCells.Add(wallCell);
            }

            return wallCells;
        }

        /// <summary>
        /// Checks if any of the proposed wall cells contain a door (side room entrance).
        /// </summary>
        private static bool WouldBlockDoor(Map map, List<IntVec3> wallCells)
        {
            foreach (IntVec3 cell in wallCells)
            {
                if (!cell.InBounds(map))
                    continue;

                Building edifice = cell.GetEdifice(map);
                if (edifice != null && edifice.def.IsDoor)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Spawns the airlock walls with centered VacBarrier.
        /// </summary>
        private static void SpawnAirlockWalls(
            Map map,
            List<IntVec3> wallCells,
            bool wallIsHorizontal,
            ThingDef wallDef,
            ThingDef barrierDef)
        {
            int centerIndex = wallCells.Count / 2;

            for (int i = 0; i < wallCells.Count; i++)
            {
                IntVec3 cell = wallCells[i];

                // Clear any existing things at this cell (except floor)
                ClearCellForWall(map, cell);

                if (i == centerIndex)
                {
                    // Spawn VacBarrier at center
                    Thing barrier = ThingMaker.MakeThing(barrierDef);
                    // VacBarrier rotation: horizontal wall = North, vertical wall = East
                    Rot4 rotation = wallIsHorizontal ? Rot4.North : Rot4.East;
                    GenSpawn.Spawn(barrier, cell, map, rotation);
                }
                else
                {
                    // Spawn wall
                    Thing wall = ThingMaker.MakeThing(wallDef);
                    GenSpawn.Spawn(wall, cell, map);
                }
            }
        }

        /// <summary>
        /// Gets the direction vector pointing inward from an edge.
        /// </summary>
        private static IntVec3 GetInwardDirection(CardinalEdge edge)
        {
            switch (edge)
            {
                case CardinalEdge.North: return IntVec3.South; // From maxZ, go toward minZ
                case CardinalEdge.South: return IntVec3.North; // From minZ, go toward maxZ
                case CardinalEdge.East: return IntVec3.West;   // From maxX, go toward minX
                case CardinalEdge.West: return IntVec3.East;   // From minX, go toward maxX
                default: return IntVec3.Zero;
            }
        }

        /// <summary>
        /// Validates that all proposed wall cells are within corridor bounds.
        /// Note: Door blocking is checked separately by WouldBlockDoor() to allow
        /// progressive depth searching.
        /// </summary>
        private static bool ValidateWallCells(Map map, LayoutRoom room, List<IntVec3> wallCells)
        {
            foreach (IntVec3 cell in wallCells)
            {
                if (!cell.InBounds(map))
                    return false;

                // Check cell is within one of the room's rects
                bool inRoom = false;
                foreach (CellRect rect in room.rects)
                {
                    if (rect.Contains(cell))
                    {
                        inRoom = true;
                        break;
                    }
                }
                if (!inRoom)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Clears a cell of buildings/items in preparation for wall placement.
        /// </summary>
        private static void ClearCellForWall(Map map, IntVec3 cell)
        {
            // Remove any existing edifice (walls, furniture, etc.)
            Building edifice = cell.GetEdifice(map);
            if (edifice != null && !edifice.def.IsDoor)
            {
                edifice.Destroy(DestroyMode.Vanish);
            }

            // Remove any items or LifeSupportUnits on the cell
            List<Thing> things = cell.GetThingList(map);
            for (int i = things.Count - 1; i >= 0; i--)
            {
                Thing thing = things[i];
                if (thing.def.category == ThingCategory.Item ||
                    thing.def == Things.LifeSupportUnit)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
