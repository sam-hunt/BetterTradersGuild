using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Greenhouse
{
    // Custom RoomContentsWorker for Greenhouse.
    //
    // Populates the greenhouse with harvested crops on shelves (rice or cotton) and wires
    // the sun lamps to power. The basins and pots themselves are sown afterwards by the
    // BTG_SowPlantGrowers GenStep from the room def's planting rules (rice in every basin,
    // whose fast 3-day grow cycle keeps the agrihand mech's harvest/replant loop visibly
    // busy; young decorative plants in the pots).
    public class RoomContents_Greenhouse : RoomContentsWorker
    {
        // Main room generation method. Spawns XML-defined prefabs (hydroponics basins,
        // plant pots, shelves), then populates the shelves.
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

            // Process all rects in the room (supports L-shaped or multi-rect rooms)
            foreach (CellRect roomRect in room.rects)
            {
                // 2. Fill shelves with harvested crops (rice or cotton)
                FillShelvesWithCrops(map, roomRect);

                // 3. Connect sun lamps to the conduit network under the room walls
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
