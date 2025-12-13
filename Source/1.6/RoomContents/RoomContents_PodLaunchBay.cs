using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom room contents worker for the Pod Launch Bay.
    ///
    /// Post-processes spawned prefabs:
    /// 1. Fills steel shelves (BTG_SteelShelf_Edge) with pod supplies:
    ///    - 65% chance: Chemfuel (50-75 units) for pod fuel
    ///    - 65% chance: Steel (50-75 units) for repairs
    /// </summary>
    public class RoomContents_PodLaunchBay : RoomContentsWorker
    {
        // Supply constants
        private const float SPAWN_CHANCE = 0.65f;
        private const int MIN_STACK = 50;
        private const int MAX_STACK = 75;

        /// <summary>
        /// Main room generation method for the pod launch bay.
        /// Spawns XML-defined prefabs, then fills shelves with pod supplies.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (empty shelves, pod launchers)
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Post-process spawned prefabs
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                FillSupplyShelves(map, roomRect);
            }
        }

        /// <summary>
        /// Finds all 2-cell wide shelves in the room and fills them with pod supplies.
        /// </summary>
        private void FillSupplyShelves(Map map, CellRect roomRect)
        {
            List<Building_Storage> supplyShelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, "Shelf", 2);

            // Fill each supply shelf with chemfuel and steel
            foreach (Building_Storage shelf in supplyShelves)
            {
                // Chemfuel for pod fuel (65% chance)
                if (Rand.Chance(SPAWN_CHANCE))
                {
                    RoomShelfHelper.AddItemsToShelf(map, shelf, "Chemfuel", Rand.RangeInclusive(MIN_STACK, MAX_STACK));
                }

                // Steel for repairs (65% chance)
                if (Rand.Chance(SPAWN_CHANCE))
                {
                    RoomShelfHelper.AddItemsToShelf(map, shelf, "Steel", Rand.RangeInclusive(MIN_STACK, MAX_STACK));
                }
            }
        }
    }
}
