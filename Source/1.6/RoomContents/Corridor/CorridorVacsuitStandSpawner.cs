using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    /// <summary>
    /// Spawns outfit stands near corridor airlocks, some populated with vacsuits.
    ///
    /// Algorithm:
    /// 1. Find all VacBarriers in the corridor (centers of airlock walls)
    /// 2. Determine airlock wall orientation from VacBarrier rotation
    /// 3. Place outfit stands in corner positions on the "inner" side of the airlock
    /// 4. Rotate stands to face into the corridor
    /// 5. Randomly populate ~50% of stands with vacsuit + helmet
    ///
    /// Stands are permanent fixtures, but some may be empty (implying vacsuits
    /// were used by crew or worn out over time). This provides emergency vacuum
    /// protection gear near corridor exits to space.
    /// </summary>
    public static class CorridorVacsuitStandSpawner
    {
        /// <summary>
        /// Chance for each outfit stand to be populated with vacsuit apparel (0.0 - 1.0).
        /// Stands always spawn, but some may be empty (implying vacsuits were used or worn out).
        /// </summary>
        private const float PopulateChancePerStand = 0.5f;

        /// <summary>
        /// Distance from the VacBarrier to place the outfit stand (on the inner side).
        /// </summary>
        private const int StandDepthFromBarrier = 1;

        /// <summary>
        /// Spawns outfit stands with vacsuits near VacBarrier airlocks in the corridor.
        /// Call this AFTER CorridorAirlockSpawner.SpawnAirlocks().
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="room">The corridor LayoutRoom.</param>
        /// <returns>List of spawned outfit stands for further processing (e.g., painting).</returns>
        public static List<Building_OutfitStand> SpawnVacsuitStands(Map map, LayoutRoom room)
        {
            List<Building_OutfitStand> placedStands = new List<Building_OutfitStand>();

            if (room.rects == null || room.rects.Count == 0)
                return placedStands;

            // Find all VacBarriers (centers of airlock walls)
            List<Building> vacBarriers = FindVacBarriersInRoom(map, room);

            if (vacBarriers.Count == 0)
                return placedStands;

            foreach (Building barrier in vacBarriers)
            {
                // Spawn stands in corner positions near this airlock (each position has independent chance)
                SpawnStandsNearAirlock(map, room, barrier, placedStands);
            }

            // Populate all placed stands with vacsuits
            if (placedStands.Count > 0)
            {
                PopulateStandsWithVacsuits(map, placedStands);
            }

            return placedStands;
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
        /// Spawns outfit stands in corner positions near an airlock VacBarrier.
        /// </summary>
        private static void SpawnStandsNearAirlock(
            Map map,
            LayoutRoom room,
            Building vacBarrier,
            List<Building_OutfitStand> placedStands)
        {
            // VacBarrier rotation tells us wall orientation:
            // - North rotation = wall runs East-West (horizontal)
            // - East rotation = wall runs North-South (vertical)
            bool wallIsHorizontal = vacBarrier.Rotation == Rot4.North;

            // Determine the "inner" direction (away from space, toward room interior)
            // The airlock spawner places walls inward from blast doors, so we need to
            // determine which side is "inner" by checking which side has more corridor cells
            IntVec3 innerDirection = DetermineInnerDirection(map, room, vacBarrier, wallIsHorizontal);

            // Calculate stand positions in corners on the inner side
            List<StandPlacement> placements = CalculateStandPlacements(
                vacBarrier.Position,
                wallIsHorizontal,
                innerDirection);

            foreach (StandPlacement placement in placements)
            {
                if (!IsValidStandPosition(map, room, placement.Position))
                    continue;

                // Always spawn the outfit stand (it's a permanent fixture)
                Building_OutfitStand stand = SpawnOutfitStand(map, placement.Position, placement.Rotation);
                if (stand != null)
                {
                    placedStands.Add(stand);
                }
            }
        }

        /// <summary>
        /// Placement data for an outfit stand.
        /// </summary>
        private struct StandPlacement
        {
            public IntVec3 Position;
            public Rot4 Rotation;
        }

        /// <summary>
        /// Determines which side of the VacBarrier is the "inner" side (toward room interior).
        /// </summary>
        private static IntVec3 DetermineInnerDirection(
            Map map,
            LayoutRoom room,
            Building vacBarrier,
            bool wallIsHorizontal)
        {
            IntVec3 pos = vacBarrier.Position;

            // Check both perpendicular directions and count how many cells are in the room
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

            int count1 = CountRoomCellsInDirection(map, room, pos, dir1, 5);
            int count2 = CountRoomCellsInDirection(map, room, pos, dir2, 5);

            // The direction with more room cells is the "inner" direction
            return count1 >= count2 ? dir1 : dir2;
        }

        /// <summary>
        /// Counts how many cells in a direction are within the room.
        /// </summary>
        private static int CountRoomCellsInDirection(
            Map map,
            LayoutRoom room,
            IntVec3 start,
            IntVec3 direction,
            int maxDistance)
        {
            int count = 0;
            for (int i = 1; i <= maxDistance; i++)
            {
                IntVec3 cell = start + (direction * i);
                if (!cell.InBounds(map))
                    break;

                if (IsInRoom(cell, room))
                    count++;
                else
                    break;
            }
            return count;
        }

        /// <summary>
        /// Checks if a cell is within any of the room's rects.
        /// </summary>
        private static bool IsInRoom(IntVec3 cell, LayoutRoom room)
        {
            foreach (CellRect rect in room.rects)
            {
                if (rect.Contains(cell))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the positions and rotations for outfit stands near an airlock.
        /// Places stands in the corners on the inner side of the airlock wall.
        /// Each stand faces toward the corridor center (inward from the wall it's against).
        /// </summary>
        private static List<StandPlacement> CalculateStandPlacements(
            IntVec3 barrierPos,
            bool wallIsHorizontal,
            IntVec3 innerDirection)
        {
            List<StandPlacement> placements = new List<StandPlacement>();

            // Calculate corner positions
            // For a horizontal wall (E-W), corners are at x +/- 1, offset by innerDirection
            // For a vertical wall (N-S), corners are at z +/- 1, offset by innerDirection

            IntVec3 innerOffset = innerDirection * StandDepthFromBarrier;

            if (wallIsHorizontal)
            {
                // Wall runs East-West (corridor runs E-W at this point)
                // Stands are placed at x +/- 1 from barrier center
                // Each stand faces toward corridor center based on which edge it's on

                // West stand (x-1) faces East (toward corridor center)
                placements.Add(new StandPlacement
                {
                    Position = barrierPos + innerOffset + new IntVec3(-1, 0, 0),
                    Rotation = Rot4.East
                });
                // East stand (x+1) faces West (toward corridor center)
                placements.Add(new StandPlacement
                {
                    Position = barrierPos + innerOffset + new IntVec3(1, 0, 0),
                    Rotation = Rot4.West
                });
            }
            else
            {
                // Wall runs North-South (corridor runs N-S at this point)
                // Stands are placed at z +/- 1 from barrier center
                // Each stand faces toward corridor center based on which edge it's on

                // South stand (z-1) faces North (toward corridor center)
                placements.Add(new StandPlacement
                {
                    Position = barrierPos + innerOffset + new IntVec3(0, 0, -1),
                    Rotation = Rot4.North
                });
                // North stand (z+1) faces South (toward corridor center)
                placements.Add(new StandPlacement
                {
                    Position = barrierPos + innerOffset + new IntVec3(0, 0, 1),
                    Rotation = Rot4.South
                });
            }

            return placements;
        }

        /// <summary>
        /// Validates that a position is suitable for placing an outfit stand.
        /// </summary>
        private static bool IsValidStandPosition(Map map, LayoutRoom room, IntVec3 pos)
        {
            if (!pos.InBounds(map))
                return false;

            if (!IsInRoom(pos, room))
                return false;

            // Check for existing buildings that would block placement
            Building edifice = pos.GetEdifice(map);
            if (edifice != null)
                return false;

            // Check terrain is walkable
            TerrainDef terrain = pos.GetTerrain(map);
            if (terrain == null || !terrain.passability.Equals(Traversability.Standable))
                return false;

            return true;
        }

        /// <summary>
        /// Spawns an outfit stand at the specified position with the given rotation.
        /// </summary>
        private static Building_OutfitStand SpawnOutfitStand(Map map, IntVec3 pos, Rot4 rotation)
        {
            ThingDef standDef = Things.Building_OutfitStand;
            if (standDef == null)
            {
                Log.Warning("[Better Traders Guild] Building_OutfitStand def not found");
                return null;
            }

            // Use steel as the stuff material
            ThingDef stuffDef = Things.Steel;

            Thing stand = ThingMaker.MakeThing(standDef, stuffDef);
            if (stand == null)
            {
                Log.Warning("[Better Traders Guild] Failed to create outfit stand");
                return null;
            }

            GenSpawn.Spawn(stand, pos, map, rotation);

            return stand as Building_OutfitStand;
        }

        /// <summary>
        /// Populates all placed outfit stands with vacsuit and helmet.
        /// </summary>
        private static void PopulateStandsWithVacsuits(Map map, List<Building_OutfitStand> stands)
        {
            List<ThingDef> vacsuitSet = new List<ThingDef>();

            if (Things.Apparel_Vacsuit != null)
            {
                vacsuitSet.Add(Things.Apparel_Vacsuit);
            }

            if (Things.Apparel_VacsuitHelmet != null)
            {
                vacsuitSet.Add(Things.Apparel_VacsuitHelmet);
            }

            if (vacsuitSet.Count == 0)
            {
                Log.Warning("[Better Traders Guild] Vacsuit defs not found, cannot populate stands");
                return;
            }

            // Populate each stand individually (some may be empty - vacsuits used or worn out)
            foreach (Building_OutfitStand stand in stands)
            {
                // Each stand has independent chance to be populated
                if (!Rand.Chance(PopulateChancePerStand))
                    continue;

                foreach (ThingDef apparelDef in vacsuitSet)
                {
                    Apparel apparel = CreateVacsuitApparel(apparelDef);
                    if (apparel == null)
                        continue;

                    bool success = stand.AddApparel(apparel);
                    if (!success)
                    {
                        apparel.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a vacsuit apparel item with Normal-Good quality.
        /// </summary>
        private static Apparel CreateVacsuitApparel(ThingDef apparelDef)
        {
            if (apparelDef == null)
                return null;

            // Determine stuff if required
            ThingDef stuffDef = null;
            if (apparelDef.MadeFromStuff)
            {
                stuffDef = GenStuff.DefaultStuffFor(apparelDef);
            }

            Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuffDef);
            if (apparel == null)
                return null;

            // Set quality to Normal or Good
            CompQuality compQuality = apparel.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                QualityCategory quality = Rand.Chance(0.6f)
                    ? QualityCategory.Normal
                    : QualityCategory.Good;
                compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
            }

            return apparel;
        }
    }
}
