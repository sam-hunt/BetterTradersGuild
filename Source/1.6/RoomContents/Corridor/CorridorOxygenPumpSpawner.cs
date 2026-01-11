using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    /// <summary>
    /// Spawns oxygen pumps in corridor airlock areas (between VacBarrier and blast door).
    ///
    /// Algorithm mirrors CorridorVacsuitStandSpawner but places on the "outer" side:
    /// 1. Find all VacBarriers in the corridor (centers of airlock walls)
    /// 2. Determine the "outer" direction (toward blast door, away from room interior)
    /// 3. Place pumps at fixed positions: barrierPos + outerDirection + sideOffset (±1)
    /// 4. Rotate pumps to face toward corridor center
    ///
    /// This creates symmetry with vacsuit stands on the inner side of each airlock.
    /// </summary>
    public static class CorridorOxygenPumpSpawner
    {
        /// <summary>
        /// Distance from the VacBarrier to place the oxygen pump (on the outer side).
        /// Matches StandDepthFromBarrier in CorridorVacsuitStandSpawner.
        /// </summary>
        private const int PumpDepthFromBarrier = 1;

        /// <summary>
        /// Spawns oxygen pumps in the airlock area (outer side of VacBarrier).
        /// Call this AFTER CorridorAirlockSpawner.SpawnAirlocks().
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="room">The corridor LayoutRoom.</param>
        /// <returns>List of spawned oxygen pumps for further processing (e.g., painting).</returns>
        public static List<Building> SpawnOxygenPumps(Map map, LayoutRoom room)
        {
            List<Building> placedPumps = new List<Building>();

            if (room.rects == null || room.rects.Count == 0)
                return placedPumps;

            // Find all VacBarriers (centers of airlock walls)
            List<Building> vacBarriers = FindVacBarriersInRoom(map, room);

            foreach (Building barrier in vacBarriers)
            {
                SpawnPumpsNearAirlock(map, room, barrier, placedPumps);
            }

            return placedPumps;
        }

        /// <summary>
        /// Finds all VacBarrier buildings within the corridor room.
        /// </summary>
        private static List<Building> FindVacBarriersInRoom(Map map, LayoutRoom room)
        {
            List<Building> barriers = new List<Building>();
            HashSet<IntVec3> visitedCells = new HashSet<IntVec3>();

            foreach (CellRect rect in room.rects)
            {
                foreach (IntVec3 cell in rect.Cells)
                {
                    if (!cell.InBounds(map) || visitedCells.Contains(cell))
                        continue;

                    visitedCells.Add(cell);

                    Building edifice = cell.GetEdifice(map);
                    if (edifice != null && edifice.def == Things.VacBarrier)
                    {
                        barriers.Add(edifice);
                    }
                }
            }

            return barriers;
        }

        /// <summary>
        /// Spawns oxygen pumps at fixed positions near an airlock VacBarrier.
        /// Mirrors CorridorVacsuitStandSpawner but on the outer side.
        /// </summary>
        private static void SpawnPumpsNearAirlock(
            Map map,
            LayoutRoom room,
            Building vacBarrier,
            List<Building> placedPumps)
        {
            // VacBarrier rotation tells us wall orientation:
            // - North rotation = airlock wall runs East-West (horizontal)
            // - East rotation = airlock wall runs North-South (vertical)
            bool wallIsHorizontal = vacBarrier.Rotation == Rot4.North;

            // Determine the "outer" direction (toward blast door, away from room interior)
            IntVec3 outerDirection = DetermineOuterDirection(map, room, vacBarrier, wallIsHorizontal);
            IntVec3 outerOffset = outerDirection * PumpDepthFromBarrier;

            // Place pumps at ±1 offset perpendicular to corridor, mirroring vacsuit stand placement
            if (wallIsHorizontal)
            {
                // Airlock wall runs E-W, corridor runs N-S at this point
                // Pumps at x ±1 from barrier center, facing toward corridor center
                TrySpawnPump(map, vacBarrier.Position + outerOffset + new IntVec3(-1, 0, 0), Rot4.West, placedPumps);
                TrySpawnPump(map, vacBarrier.Position + outerOffset + new IntVec3(1, 0, 0), Rot4.East, placedPumps);
            }
            else
            {
                // Airlock wall runs N-S, corridor runs E-W at this point
                // Pumps at z ±1 from barrier center, facing toward corridor center
                TrySpawnPump(map, vacBarrier.Position + outerOffset + new IntVec3(0, 0, -1), Rot4.South, placedPumps);
                TrySpawnPump(map, vacBarrier.Position + outerOffset + new IntVec3(0, 0, 1), Rot4.North, placedPumps);
            }
        }

        /// <summary>
        /// Attempts to spawn an oxygen pump at the specified position.
        /// </summary>
        private static void TrySpawnPump(Map map, IntVec3 pos, Rot4 rotation, List<Building> placedPumps)
        {
            if (!pos.InBounds(map))
                return;

            // Check for blocking things at this position
            Building edifice = pos.GetEdifice(map);
            if (edifice != null)
                return;

            ThingDef pumpDef = Things.OxygenPump;
            if (pumpDef == null)
                return;

            Thing pump = ThingMaker.MakeThing(pumpDef);
            if (pump == null)
                return;

            GenSpawn.Spawn(pump, pos, map, rotation);

            if (pump is Building building)
            {
                placedPumps.Add(building);
            }
        }

        /// <summary>
        /// Determines which side of the VacBarrier is the "outer" side (toward blast door).
        /// The direction with fewer room cells is the outer direction.
        /// </summary>
        private static IntVec3 DetermineOuterDirection(
            Map map,
            LayoutRoom room,
            Building vacBarrier,
            bool wallIsHorizontal)
        {
            IntVec3 pos = vacBarrier.Position;

            IntVec3 dir1, dir2;
            if (wallIsHorizontal)
            {
                dir1 = IntVec3.North;
                dir2 = IntVec3.South;
            }
            else
            {
                dir1 = IntVec3.East;
                dir2 = IntVec3.West;
            }

            int count1 = CountRoomCellsInDirection(room, pos, dir1, 5);
            int count2 = CountRoomCellsInDirection(room, pos, dir2, 5);

            return count1 < count2 ? dir1 : dir2;
        }

        /// <summary>
        /// Counts how many cells in a direction are within the room.
        /// </summary>
        private static int CountRoomCellsInDirection(
            LayoutRoom room,
            IntVec3 start,
            IntVec3 direction,
            int maxDistance)
        {
            int count = 0;
            for (int i = 1; i <= maxDistance; i++)
            {
                IntVec3 cell = start + (direction * i);
                bool inRoom = false;
                foreach (CellRect rect in room.rects)
                {
                    if (rect.Contains(cell))
                    {
                        inRoom = true;
                        break;
                    }
                }
                if (inRoom)
                    count++;
                else
                    break;
            }
            return count;
        }
    }
}
