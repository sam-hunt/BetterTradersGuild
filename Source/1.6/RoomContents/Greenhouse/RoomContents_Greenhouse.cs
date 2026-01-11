using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.Greenhouse
{
    /// <summary>
    /// Custom RoomContentsWorker for Greenhouse.
    ///
    /// Populates the greenhouse with:
    /// - Rice or potatoes in hydroponics basins (food production)
    /// - Daylilies in decorative plant pots (aesthetics)
    /// - Harvested crops on shelves (corn or cotton)
    /// </summary>
    public class RoomContents_Greenhouse : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs (hydroponics basins,
        /// plant pots, shelves), then populates them with appropriate contents.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            if (room.rects == null || room.rects.Count == 0)
            {
                base.FillRoom(map, room, faction, threatPoints);
                return;
            }

            CellRect roomRect = room.rects.First();

            // 1. Call base to spawn XML prefabs (hydroponics basins, plant pots, shelves)
            //    IMPORTANT: We need containers to exist before we can populate them
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Spawn plants in hydroponics basins with varied growth
            //    Randomly pick rice or potatoes for the entire room (consistent species per room)
            var hydroPlantOptions = new List<ThingDef> { Things.Plant_Rice, Things.Plant_Potato };
            ThingDef hydroPlant = hydroPlantOptions.RandomElementByWeight(p => 1f);
            float hydroGrowth = Rand.Range(0.7f, 1.0f);
            RoomPlantHelper.SpawnPlantsInHydroponics(map, roomRect, hydroPlant, hydroGrowth);

            // 3. Spawn daylilies in decorative plant pots
            //    Lower growth for young/budding appearance
            float potGrowth = Rand.Range(0.25f, 0.65f);
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Daylily, potGrowth);

            // 4. Fill shelves with harvested crops (corn or cotton)
            FillShelvesWithCrops(map, roomRect);

            // 5. Connect sun lamps to the conduit network under the room walls
            RoomEdgeConnector.ConnectBuildingsToConduitNetwork(map, roomRect, Things.SunLamp);
        }

        /// <summary>
        /// Fills steel shelves with harvested crops - randomly either corn or cotton per shelf.
        /// </summary>
        private void FillShelvesWithCrops(Map map, CellRect roomRect)
        {
            var shelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            foreach (var shelf in shelves)
            {
                // 50/50 chance: corn or cotton
                if (Rand.Bool)
                {
                    // Corn: 2 stacks of 25-40 each
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.RawCorn, Rand.Range(25, 40));
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.RawCorn, Rand.Range(25, 40));
                }
                else
                {
                    // Cotton (cloth): 35-55 stack
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Cloth, Rand.Range(35, 55));
                }
            }
        }
    }
}
