using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom room contents worker for the Mech Station.
    ///
    /// Post-processes spawned prefabs:
    /// 1. Fills steel shelves (BTG_SteelShelf_Edge) with mech supplies:
    ///    - Steel (20-30 units) for repairs
    ///    - Components (2-3 units) for maintenance
    /// </summary>
    public class RoomContents_MechStation : RoomContentsWorker
    {
        // Supply constants
        private const string STEEL_DEFNAME = "Steel";
        private const string COMPONENT_DEFNAME = "ComponentIndustrial";

        /// <summary>
        /// Main room generation method for the mech station.
        /// Spawns XML-defined prefabs, then fills shelves with mech supplies.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (empty shelves, rechargers, gestators)
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Post-process spawned prefabs
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                FillSupplyShelves(map, roomRect);
            }
        }

        /// <summary>
        /// Finds all 2-cell wide shelves in the room and fills them with mech supplies.
        /// </summary>
        private void FillSupplyShelves(Map map, CellRect roomRect)
        {
            List<Building_Storage> supplyShelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, "Shelf", 2);

            // Fill each supply shelf with mech supplies
            foreach (Building_Storage shelf in supplyShelves)
            {
                // Steel for repairs (20-30 units)
                RoomShelfHelper.AddItemsToShelf(map, shelf, STEEL_DEFNAME, Rand.RangeInclusive(20, 30));

                // Components for maintenance (2-3 units)
                RoomShelfHelper.AddItemsToShelf(map, shelf, COMPONENT_DEFNAME, Rand.RangeInclusive(2, 3));
            }
        }
    }
}
