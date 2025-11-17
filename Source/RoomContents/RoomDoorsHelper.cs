using System.Collections.Generic;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using static BetterTradersGuild.RoomContents.PlacementCalculator;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Helper for scanning room doors from RimWorld map.
    /// Single responsibility: extract door positions from a LayoutRoom for use in placement calculations.
    /// </summary>
    public static class RoomDoorsHelper
    {
        /// <summary>
        /// Scans a room and returns all door positions.
        /// </summary>
        /// <param name="room">The layout room to scan</param>
        /// <param name="map">The map containing the room</param>
        /// <returns>List of door positions</returns>
        public static List<DoorPosition> GetDoorPositions(LayoutRoom room, Map map)
        {
            var doors = new List<DoorPosition>();

            if (room.rects == null || room.rects.Count == 0)
                return doors;

            // Get room bounds
            CellRect roomRect = room.rects[0];

            // Scan all perimeter cells for doors
            foreach (IntVec3 cell in roomRect.EdgeCells)
            {
                if (!cell.InBounds(map))
                    continue;

                Building edifice = cell.GetEdifice(map);
                if (edifice != null && edifice.def.IsDoor)
                {
                    doors.Add(new DoorPosition { X = cell.x, Z = cell.z });
                }
            }

            return doors;
        }
    }
}
