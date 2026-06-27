using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.PodLaunchBay
{
    public class RoomContents_PodLaunchBay : RoomContentsWorker
    {
        // Main room generation method for the pod launch bay.
        // Spawns XML-defined prefabs, then fills shelves with pod supplies.
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
                    FillPodLauncherFuel(map, roomRect);

                    // Connect firefoam poppers to chemfuel pipes (does nothing if VE Chemfuel not installed)
                    RoomEdgeConnector.ConnectBuildingsToInfrastructure(map, roomRect, Things.FirefoamPopper, Things.VCHE_UndergroundChemfuelPipe);
                }
            }
        }

        // Sets pod launcher fuel levels. Launchers with a malfunctioning pod get 45%
        // (heavy use led to the malfunction), others get 25%. Must run after
        // MalfunctioningPodReplacer. Sets the private fuel field directly (via
        // RefuelableReflection), avoiding the difficulty multiplier baked into Refuel(float).
        private void FillPodLauncherFuel(Map map, CellRect roomRect)
        {
            foreach (Building launcher in RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, Things.PodLauncher))
            {
                CompRefuelable fuelComp = launcher.TryGetComp<CompRefuelable>();
                if (fuelComp == null) continue;

                bool hasMalfunctioningPod = GenAdj.CellsAdjacent8Way(launcher)
                    .Any(c => c.InBounds(map) && c.GetThingList(map)
                        .Any(t => t.def == Things.MalfunctioningTransportPod));

                float fuelPct = hasMalfunctioningPod ? 0.4f : 0.2f;
                float targetFuel = fuelComp.Props.fuelCapacity * fuelPct;
                RefuelableReflection.TrySetFuel(fuelComp, targetFuel);
            }
        }

        // Finds all 2-cell wide shelves in the room and fills them with pod supplies.
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
