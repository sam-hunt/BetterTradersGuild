using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace BetterTradersGuild.AI.Civilians
{
    // Launchable discovery + custom "fly away and vanish" lift-off for the sheltering-
    // civilian escape (LordToil_BTGEscape).
    //
    // BTG settlements generate a functional PassengerShuttle (CompShuttle) in the shuttle
    // bay and several TransportPods (CompLaunchable_TransportPod) in the pod bay; either can
    // carry escapees. We deliberately do NOT use vanilla CompLaunchable.TryLaunch: it needs
    // fuel + a player-chosen world destination and would spawn a faction-owned traveling
    // transporter on the world map - meaningless for "non-combatants fled an orbital
    // platform", and fragile on space tiles. Instead, once a launchable's escapees are
    // aboard, LiftOff removes them from play (they escaped), tears the craft down, and plays
    // a takeoff effect. Boarding itself still uses ordinary carry/enter jobs so it looks normal.
    public static class LaunchableEscapeHelper
    {
        // All loadable launchables on the map (shuttle + pods + anything else carrying a
        // CompTransporter), ranked: shuttle(s) first, then transport pods, then the rest.
        public static List<Thing> AllLaunchables(Map map)
        {
            var result = new List<Thing>();
            if (map == null)
                return result;

            List<Thing> things = map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t.Spawned && t.TryGetComp<CompTransporter>() != null)
                    result.Add(t);
            }
            result.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
            return result;
        }

        // Lower = preferred: shuttle (holds the whole family) over pods over any other craft.
        private static int Rank(Thing t)
        {
            if (t.TryGetComp<CompShuttle>() != null)
                return 0;
            if (t.TryGetComp<CompLaunchable_TransportPod>() != null)
                return 1;
            return 2;
        }

        // Cheap existence check - no list build or sort (called every tick by the escape ->
        // stranded transition trigger).
        public static bool AnyLaunchable(Map map)
        {
            if (map == null)
                return false;

            List<Thing> things = map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].Spawned && things[i].TryGetComp<CompTransporter>() != null)
                    return true;
            }
            return false;
        }

        // The launchable a walker should head for: the nearest reachable shuttle if any
        // shuttle exists, otherwise the nearest reachable launchable of any kind. Null when
        // none is reachable - the lord then falls through to the stranded phase (which is
        // gated on no launchable EXISTING, so a merely-unreachable one keeps them trying).
        public static Thing PreferredLaunchable(Pawn pawn)
        {
            List<Thing> all = AllLaunchables(pawn.Map);
            if (all.Count == 0)
                return null;

            // If any shuttle exists, the whole family aims for it (it seats everyone);
            // pods are only used when no shuttle remains.
            bool anyShuttle = all.Exists(t => t.TryGetComp<CompShuttle>() != null);

            Thing best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                Thing t = all[i];
                if (anyShuttle && t.TryGetComp<CompShuttle>() == null)
                    continue;
                if (!pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float distSq = (pawn.Position - t.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = t;
                }
            }
            return best;
        }

        // True if the pawn is currently held inside any launchable on the map (already boarded).
        public static bool IsAboardAnyLaunchable(Pawn pawn, Map map)
        {
            List<Thing> all = AllLaunchables(map);
            for (int i = 0; i < all.Count; i++)
            {
                CompTransporter t = all[i].TryGetComp<CompTransporter>();
                if (t?.innerContainer != null && t.innerContainer.Contains(pawn))
                    return true;
            }
            return false;
        }

        // Custom lift-off: the launchable's occupants have escaped. Detach any lord walkers
        // aboard from their lord (so it shrinks and self-cleans), vanish every occupant
        // (escapees leave the map with no corpses), deregister a shuttle's TransportShip, and
        // tear the craft down with a takeoff effect. Idempotent-safe per launchable.
        public static void LiftOff(Thing launchable)
        {
            if (launchable == null || launchable.Destroyed)
                return;

            Map map = launchable.Map;
            IntVec3 cell = launchable.Position;
            CompTransporter transporter = launchable.TryGetComp<CompTransporter>();

            if (transporter?.innerContainer != null)
            {
                List<Pawn> occupants = transporter.innerContainer.OfType<Pawn>().ToList();
                for (int i = 0; i < occupants.Count; i++)
                    occupants[i].GetLord()?.RemovePawn(occupants[i]);
                transporter.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            }

            // A shuttle's TransportShip must be disposed or it lingers in the world's
            // TransportShipManager and logs a "null ship objects" error on next save.
            // Dispose deregisters it and, for a non-player shuttle, destroys the building too.
            CompShuttle shuttle = launchable.TryGetComp<CompShuttle>();
            if (shuttle?.shipParent != null)
            {
                try
                {
                    shuttle.shipParent.Dispose();
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[Better Traders Guild] Shuttle ship teardown failed: {ex.Message}");
                }
            }

            PlayTakeoffEffect(cell, map);

            if (!launchable.Destroyed)
                launchable.Destroy(DestroyMode.Vanish);
        }

        private static void PlayTakeoffEffect(IntVec3 cell, Map map)
        {
            if (map == null)
                return;

            Vector3 v = cell.ToVector3Shifted();
            SoundDefOf.Gravship_Launch.PlayOneShot(SoundInfo.InMap(new TargetInfo(cell, map)));
            for (int i = 0; i < 6; i++)
                FleckMaker.ThrowDustPuffThick(v, map, 2.5f, new Color(1f, 1f, 1f, 0.7f));
            FleckMaker.ThrowSmoke(v, map, 3f);
        }
    }
}
