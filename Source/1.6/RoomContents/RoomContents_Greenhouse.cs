using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Greenhouse.
    ///
    /// Populates the greenhouse with:
    /// - Rice in hydroponics basins (food production)
    /// - Daylilies in decorative plant pots (aesthetics)
    /// - Harvested crops on shelves (corn or cotton)
    ///
    /// LEARNING NOTE: This worker calls base.FillRoom() FIRST because the XML prefabs
    /// spawn the hydroponics basins, plant pots, and shelves, and we need those to exist
    /// before we can populate them.
    /// </summary>
    public class RoomContents_Greenhouse : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs (hydroponics basins,
        /// plant pots, shelves), then populates them with appropriate contents.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (hydroponics basins, plant pots, shelves)
            //    IMPORTANT: We need containers to exist before we can populate them
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            CellRect roomRect = room.rects.First();

            // 2. Spawn rice plants in hydroponics basins with varied growth
            ThingDef ricePlant = DefDatabase<ThingDef>.GetNamed("Plant_Rice", false);
            float riceGrowth = Rand.Range(0.7f, 1.0f);
            RoomPlantHelper.SpawnPlantsInHydroponics(map, roomRect, ricePlant, riceGrowth);

            // 3. Spawn daylilies in decorative plant pots (uses pot's default if null)
            //    Lower growth for young/budding appearance
            float potGrowth = Rand.Range(0.25f, 0.65f);
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, null, potGrowth);

            // 4. Fill shelves with harvested crops (corn or cotton)
            FillShelvesWithCrops(map, roomRect);

            // 5. Connect sun lamps to the conduit network under the room walls
            ConnectSunLampsToConduitNetwork(map, roomRect);
        }

        /// <summary>
        /// Fills steel shelves with harvested crops - randomly either corn or cotton per shelf.
        /// </summary>
        private void FillShelvesWithCrops(Map map, CellRect roomRect)
        {
            var shelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, "Shelf", 2);

            foreach (var shelf in shelves)
            {
                // 50/50 chance: corn or cotton
                if (Rand.Bool)
                {
                    // Corn: 2 stacks of 25-40 each
                    RoomShelfHelper.AddItemsToShelf(map, shelf, "RawCorn", Rand.Range(25, 40));
                    RoomShelfHelper.AddItemsToShelf(map, shelf, "RawCorn", Rand.Range(25, 40));
                }
                else
                {
                    // Cotton (cloth): 35-55 stack
                    RoomShelfHelper.AddItemsToShelf(map, shelf, "Cloth", Rand.Range(35, 55));
                }
            }
        }

        /// <summary>
        /// Finds all sun lamps in the room and runs hidden conduits from each
        /// to the nearest room edge, connecting them to the wall conduit network.
        /// </summary>
        private void ConnectSunLampsToConduitNetwork(Map map, CellRect roomRect)
        {
            ThingDef hiddenConduitDef = DefDatabase<ThingDef>.GetNamed("HiddenConduit", false);
            if (hiddenConduitDef == null)
                return;

            var sunLamps = RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, "SunLamp");

            foreach (Building sunLamp in sunLamps)
            {
                RoomEdgeConnector.ConnectToNearestEdge(map, sunLamp.Position, roomRect, hiddenConduitDef);
            }
        }
    }
}
