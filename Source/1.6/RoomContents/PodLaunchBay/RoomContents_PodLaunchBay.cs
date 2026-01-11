using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.PodLaunchBay
{
    /// <summary>
    /// Custom room contents worker for the Pod Launch Bay.
    ///
    /// Post-processes spawned prefabs:
    /// 1. Fills steel shelves (BTG_SteelShelf_Edge) with pod supplies:
    ///    - 65% chance: Chemfuel (50-75 units) for pod fuel
    ///    - 65% chance: Steel (50-75 units) for repairs
    /// 2. Replaces some TransportPods with MalfunctioningTransportPods:
    ///    - 20% chance per pod to become malfunctioning (contains random loot/corpses/hostile pawn)
    /// </summary>
    public class RoomContents_PodLaunchBay : RoomContentsWorker
    {
        // Supply constants
        private const float SPAWN_CHANCE = 0.65f;
        private const int MIN_STACK = 50;
        private const int MAX_STACK = 75;

        // Malfunctioning pod constraints
        private const float MALFUNCTION_CHANCE = 0.20f;
        private const int MAX_MALFUNCTIONING = 2;

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
                ReplaceSomePodsWithMalfunctioning(map, roomRect);

                // Connect firefoam poppers to chemfuel pipes (does nothing if VE Chemfuel not installed)
                RoomEdgeConnector.ConnectBuildingsToInfrastructure(map, roomRect, Things.FirefoamPopper, Things.VCHE_UndergroundChemfuelPipe);
            }
        }

        /// <summary>
        /// Finds all 2-cell wide shelves in the room and fills them with pod supplies.
        /// </summary>
        private void FillSupplyShelves(Map map, CellRect roomRect)
        {
            List<Building_Storage> supplyShelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            // Fill each supply shelf with chemfuel and steel
            foreach (Building_Storage shelf in supplyShelves)
            {
                // Chemfuel for pod fuel (65% chance)
                if (Rand.Chance(SPAWN_CHANCE))
                {
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Chemfuel, Rand.RangeInclusive(MIN_STACK, MAX_STACK));
                }

                // Steel for repairs (65% chance)
                if (Rand.Chance(SPAWN_CHANCE))
                {
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Steel, Rand.RangeInclusive(MIN_STACK, MAX_STACK));
                }
            }
        }

        /// <summary>
        /// Finds all TransportPods in the room and randomly replaces some with MalfunctioningTransportPods.
        /// Uses 20% chance per pod, capped at 2 max, with at least 1 guaranteed.
        /// Malfunctioning pods can contain random loot, corpses, or even a hostile pawn when opened.
        /// </summary>
        private void ReplaceSomePodsWithMalfunctioning(Map map, CellRect roomRect)
        {
            // Find all TransportPod buildings in the room
            // Use HashSet to avoid duplicates from multi-cell buildings
            HashSet<Thing> transportPods = new HashSet<Thing>();
            foreach (IntVec3 cell in roomRect)
            {
                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing.def == Things.TransportPod)
                    {
                        transportPods.Add(thing);
                    }
                }
            }

            if (transportPods.Count == 0)
                return;

            // Roll 20% chance per pod, collect candidates
            List<Thing> podsToReplace = new List<Thing>();
            foreach (Thing pod in transportPods)
            {
                if (Rand.Chance(MALFUNCTION_CHANCE))
                {
                    podsToReplace.Add(pod);
                }
            }

            // Guarantee at least 1 malfunctioning pod
            if (podsToReplace.Count == 0)
            {
                podsToReplace.Add(transportPods.RandomElement());
            }

            // Cap at 2 maximum
            if (podsToReplace.Count > MAX_MALFUNCTIONING)
            {
                podsToReplace = podsToReplace.InRandomOrder().Take(MAX_MALFUNCTIONING).ToList();
            }

            foreach (Thing pod in podsToReplace)
            {
                IntVec3 position = pod.Position;
                Rot4 rotation = pod.Rotation;

                // Despawn the normal transport pod
                pod.Destroy(DestroyMode.Vanish);

                // Spawn a malfunctioning transport pod in its place
                Thing malfunctioningPod = ThingMaker.MakeThing(Things.MalfunctioningTransportPod);
                GenSpawn.Spawn(malfunctioningPod, position, map, rotation);
            }
        }
    }
}
