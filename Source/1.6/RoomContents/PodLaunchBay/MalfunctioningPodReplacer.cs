using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using Verse;

namespace BetterTradersGuild.RoomContents.PodLaunchBay
{
    /// <summary>
    /// Helper for replacing transport pods with malfunctioning variants.
    /// Malfunctioning pods can contain random loot, corpses, or even a hostile pawn when opened.
    /// </summary>
    public static class MalfunctioningPodReplacer
    {
        private const float MALFUNCTION_CHANCE = 0.20f;
        private const int MAX_MALFUNCTIONING = 2;

        /// <summary>
        /// Finds all TransportPods in the room and randomly replaces some with MalfunctioningTransportPods.
        /// Uses 20% chance per pod, capped at 2 max, with at least 1 guaranteed.
        /// </summary>
        public static void ReplaceSomePodsWithMalfunctioning(Map map, CellRect roomRect)
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
