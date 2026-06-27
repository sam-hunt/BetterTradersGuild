using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Greenhouse
{
    // Custom RoomContentsWorker for Greenhouse.
    //
    // Populates the greenhouse with:
    // - Rice in hydroponics basins (food production; rice's fast 3-day grow cycle keeps
    //   the agrihand mech's harvest/replant loop visibly busy)
    // - Daylilies in decorative plant pots (aesthetics)
    // - Harvested crops on shelves (rice or cotton)
    public class RoomContents_Greenhouse : RoomContentsWorker
    {
        // Main room generation method. Spawns XML-defined prefabs (hydroponics basins,
        // plant pots, shelves), then populates them with appropriate contents.
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            if (room.rects == null || room.rects.Count == 0)
            {
                base.FillRoom(map, room, faction, threatPoints);
                return;
            }

            // 1. Call base to spawn XML prefabs (hydroponics basins, plant pots, shelves)
            //    IMPORTANT: We need containers to exist before we can populate them
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Spawn rice in every hydroponics basin with varied growth. Rice's fast
            //    3-day grow cycle (vs potato 5.5 / corn 14) means crops mature often, so
            //    the agrihand mech's harvest/haul/replant loop is on display regularly.
            ThingDef hydroPlant = Things.Plant_Rice;
            float hydroGrowth = Rand.Range(0.7f, 1.0f);

            // 3. Spawn daylilies in decorative plant pots
            //    Lower growth for young/budding appearance
            float potGrowth = Rand.Range(0.25f, 0.65f);

            // Process all rects in the room (supports L-shaped or multi-rect rooms)
            foreach (CellRect roomRect in room.rects)
            {
                RoomPlantHelper.SpawnPlantsInHydroponics(map, roomRect, hydroPlant, hydroGrowth);
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Daylily, potGrowth);

                // 4. Fill shelves with harvested crops (rice or cotton)
                FillShelvesWithCrops(map, roomRect);

                // 5. Connect sun lamps to the conduit network under the room walls
                RoomEdgeConnector.ConnectBuildingsToConduitNetwork(map, roomRect, Things.SunLamp);
            }
        }

        // Fills steel shelves with harvested crops - randomly either rice or cotton per shelf.
        // Rice matches what the agrihand mech harvests and shelves, so the starting stock
        // reads as the product of the same crop loop.
        private void FillShelvesWithCrops(Map map, CellRect roomRect)
        {
            var shelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            foreach (var shelf in shelves)
            {
                // 50/50 chance: rice or cotton
                if (Rand.Bool)
                {
                    // Rice: 2 stacks of 25-40 each
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.RawRice, Rand.Range(25, 40));
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.RawRice, Rand.Range(25, 40));
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
