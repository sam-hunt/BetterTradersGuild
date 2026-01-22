using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.PodLaunchBay
{
    public class RoomContents_PodLaunchBay : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method for the pod launch bay.
        /// Spawns XML-defined prefabs, then fills shelves with pod supplies.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (empty shelves, pod launchers)
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Post-process spawned prefabs (all rects)
            if (room.rects != null && room.rects.Count > 0)
            {
                foreach (CellRect roomRect in room.rects)
                {
                    FillSupplyShelves(map, roomRect);
                    MalfunctioningPodReplacer.ReplaceSomePodsWithMalfunctioning(map, roomRect);

                    // Connect firefoam poppers to chemfuel pipes (does nothing if VE Chemfuel not installed)
                    RoomEdgeConnector.ConnectBuildingsToInfrastructure(map, roomRect, Things.FirefoamPopper, Things.VCHE_UndergroundChemfuelPipe);
                }
            }
        }

        /// <summary>
        /// Finds all 2-cell wide shelves in the room and fills them with pod supplies.
        /// </summary>
        private void FillSupplyShelves(Map map, CellRect roomRect)
        {
            List<Building_Storage> supplyShelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            foreach (Building_Storage shelf in supplyShelves)
            {
                // Always chemfuel
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Chemfuel, Rand.RangeInclusive(50, 75));
                // Sometimes steel
                if (Rand.Chance(0.65f))
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Steel, Rand.RangeInclusive(50, 75));
            }
        }
    }
}
