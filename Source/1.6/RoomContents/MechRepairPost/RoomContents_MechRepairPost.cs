using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.MechRepairPost
{
    /// <summary>
    /// Custom room contents worker for the Mech Repair Post.
    ///
    /// Note: This is distinct from MechSecurityPost which contains volatile
    /// Ancient gestator tanks that release hostile mechanoids.
    /// </summary>
    public class RoomContents_MechRepairPost : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0) return;

            foreach (CellRect roomRect in room.rects)
                FillSupplyShelves(map, roomRect);

        }

        private void FillSupplyShelves(Map map, CellRect roomRect)
        {
            List<Building_Storage> supplyShelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            foreach (Building_Storage shelf in supplyShelves)
            {
                // Steel
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Steel, Rand.RangeInclusive(30, 50));
                // Components
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.ComponentIndustrial, Rand.RangeInclusive(2, 3));
            }
        }
    }
}
