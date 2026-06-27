using System.Collections.Generic;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Helper for scanning edge blockers that prevent prefab placement.
    // Edge blockers include:
    // - Doors (cells that must remain accessible)
    // - Non-wall edge cells (for reduced rects where edges are interior space)
    public static class RoomEdgeBlockersHelper
    {
        // Scans a LayoutRoom's perimeter and returns all door positions as blockers.
        // Use this for initial placement in a full room where walls are expected on all edges.
        // Scans all rects in the room to find all edge blockers.
        // room: The layout room to scan
        // map: The map containing the room
        // Returns: List of edge blocker positions (doors) from all rects
        public static List<DoorPosition> GetEdgeBlockers(LayoutRoom room, Map map)
        {
            var blockers = new List<DoorPosition>();

            if (room.rects == null || room.rects.Count == 0)
                return blockers;

            // Scan all rects in the room for edge blockers
            foreach (CellRect roomRect in room.rects)
            {
                blockers.AddRange(GetEdgeBlockersForRect(roomRect, map, wallsOnly: true));
            }

            return blockers;
        }

        // Scans a CellRect's perimeter and returns all edge blockers.
        // Use this for iterative placement in reduced rects where some edges may be interior space.
        // rect: The rectangular area to scan
        // map: The map containing the rect
        // wallsOnly: If true, only detect doors. If false, also detect non-wall edges.
        // Returns: List of edge blocker positions
        public static List<DoorPosition> GetEdgeBlockers(CellRect rect, Map map, bool wallsOnly = false)
        {
            return GetEdgeBlockersForRect(rect, map, wallsOnly);
        }

        // Core implementation for scanning edge blockers.
        // When wallsOnly=true: only doors count as blockers (original behavior).
        // When wallsOnly=false: doors AND non-wall cells count as blockers (for reduced rects).
        private static List<DoorPosition> GetEdgeBlockersForRect(CellRect rect, Map map, bool wallsOnly)
        {
            var blockers = new List<DoorPosition>();

            foreach (IntVec3 cell in rect.EdgeCells)
            {
                if (!cell.InBounds(map))
                    continue;

                Building edifice = cell.GetEdifice(map);

                // Always block on doors
                if (edifice != null && edifice.def.IsDoor)
                {
                    blockers.Add(new DoorPosition { X = cell.x, Z = cell.z });
                    continue;
                }

                // When not wallsOnly, also block on non-wall edges
                // (interior space that shouldn't have prefabs placed against it)
                if (!wallsOnly)
                {
                    bool isWall = edifice != null &&
                                  edifice.def.building != null &&
                                  edifice.def.building.isPlaceOverableWall;

                    // Also treat as wall if it's a wall-like impassable building
                    if (!isWall && edifice != null)
                    {
                        isWall = edifice.def.passability == Traversability.Impassable &&
                                 edifice.def.fillPercent >= 1f &&
                                 !edifice.def.IsDoor;
                    }

                    if (!isWall)
                    {
                        blockers.Add(new DoorPosition { X = cell.x, Z = cell.z });
                    }
                }
            }

            return blockers;
        }
    }
}
