using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.LordJobs.Civilians;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.AI.Civilians
{
    // Launchable discovery + destination-free "fly away" lift-off for the sheltering-civilian
    // escape (LordToil_BTGEscape).
    //
    // BTG settlements generate a functional PassengerShuttle (CompShuttle) in the shuttle
    // bay and several TransportPods (CompLaunchable_TransportPod) in the pod bay; either can
    // carry escapees. We deliberately do NOT use vanilla CompLaunchable.TryLaunch: it needs
    // fuel + a player-chosen world destination and would spawn a faction-owned traveling
    // transporter on the world map - meaningless for "non-combatants fled an orbital
    // platform", and fragile on space tiles. Instead, once a launchable's escapees are aboard,
    // LiftOff launches it with NO destination tile: the vanilla leaving skyfaller plays the
    // real take-off animation, and on reaching the sky the occupants ExitMap (they escaped -
    // no corpses, no traveling world object, no return trip) and the craft is torn down.
    // Boarding itself still uses ordinary carry/enter jobs so it looks normal.
    public static class LaunchableEscapeHelper
    {
        // Chemfuel a departing transport pod "spends" on take-off, drained from the adjacent
        // pod launcher it draws fuel from. The escape launch itself bypasses vanilla's fuel
        // requirement (see LiftOff), so this is purely a visible after-the-fact mark: a player
        // who later takes the settlement finds a fired pod's launcher partly drained instead of
        // full. Shuttles have no fueling port and are unaffected.
        private const float EscapeFuelDrain = 50f;

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

        // The launchable a walker should head for: the nearest REACHABLE shuttle if one is
        // reachable, otherwise the nearest reachable launchable of any kind. Null when nothing
        // is reachable. A shuttle that merely EXISTS but is blocked (fire, hostiles, a sealed
        // corridor) must not suppress a reachable pod - that used to leave every walker
        // targeting an unreachable shuttle forever, idling instead of boarding a pod.
        public static Thing PreferredLaunchable(Pawn pawn)
        {
            List<Thing> all = AllLaunchables(pawn.Map);
            if (all.Count == 0)
                return null;

            Thing bestShuttle = null;
            float bestShuttleDistSq = float.MaxValue;
            Thing bestAny = null;
            float bestAnyDistSq = float.MaxValue;

            for (int i = 0; i < all.Count; i++)
            {
                Thing t = all[i];
                if (!pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float distSq = (pawn.Position - t.Position).LengthHorizontalSquared;
                if (distSq < bestAnyDistSq)
                {
                    bestAnyDistSq = distSq;
                    bestAny = t;
                }

                if (t.TryGetComp<CompShuttle>() != null && distSq < bestShuttleDistSq)
                {
                    bestShuttleDistSq = distSq;
                    bestShuttle = t;
                }
            }

            // A reachable shuttle always wins (it seats everyone); otherwise fall back to
            // whatever reachable launchable is nearest, matching the old any-kind fallback.
            return bestShuttle ?? bestAny;
        }

        // True if any living, un-downed pawn in the list can currently reach some launchable on
        // the map - used by the lord's escape/stranded transition so it demotes only when
        // nobody can actually get to a launchable, not merely when the nearest one exists but is
        // blocked. Mirrors the walker-skip idiom in LordToil_BTGEscape.AnyWalkerStillBoundFor.
        public static bool AnyLaunchableReachable(List<Pawn> pawns, Map map)
        {
            if (map == null || pawns == null)
                return false;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.Dead != false || p.Downed)
                    continue;
                if (PreferredLaunchable(p) != null)
                    return true;
            }
            return false;
        }

        // True while any infant this walker's escape group is responsible for still lies
        // spawned on the map with at least one living group member able to reach it. A
        // carried or already-loaded infant is despawned (carry tracker / transporter
        // container), so it drops out of the spawned scan on its own. The board giver
        // uses this to hold non-carriers until every infant is actually in someone's
        // arms, so the group departs together instead of trickling out while the
        // caretaker is still walking to the crib. The any-member reach gate mirrors the
        // lord's walker-skip idiom: an infant nobody can reach must not stall the
        // departure forever (without the gate it would simply have been left behind).
        public static bool AnyInfantAwaitingCarry(Pawn walker)
        {
            Lord lord = walker.GetLord();
            if (lord == null)
                return false;
            var shelterJob = lord.LordJob as LordJob_BTGShelterCivilians;

            List<Pawn> facPawns = walker.Map.mapPawns.SpawnedPawnsInFaction(walker.Faction);
            for (int i = 0; i < facPawns.Count; i++)
            {
                Pawn baby = facPawns[i];
                if (!(baby.DevelopmentalStage.Baby() || baby.DevelopmentalStage.Newborn()))
                    continue;
                if (shelterJob?.ShouldCarryInfant(baby) == false)
                    continue;

                List<Pawn> owned = lord.ownedPawns;
                for (int j = 0; j < owned.Count; j++)
                {
                    Pawn carrier = owned[j];
                    if (carrier == baby || carrier.Dead || !carrier.Spawned || carrier.Downed)
                        continue;
                    if (!carrier.RaceProps.Humanlike
                        || carrier.DevelopmentalStage.Baby() || carrier.DevelopmentalStage.Newborn())
                        continue;
                    if (!carrier.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        continue;
                    if (carrier.CanReach(baby, PathEndMode.ClosestTouch, Danger.Deadly))
                        return true;
                }
            }
            return false;
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

        // Custom lift-off with no world destination: the launchable's occupants have escaped.
        // Detach the occupants from their lord first (so the escape lord shrinks and self-cleans
        // instead of tracking pawns that are effectively gone), then launch. A destination-less
        // launch makes the vanilla leaving skyfaller play the real take-off animation and, on
        // reaching the sky, ExitMap the occupants (they leave play cleanly - no corpses, no
        // traveling world object) and tear the craft down.
        //
        // A shuttle goes through its TransportShip's ShipJob_FlyAway - vanilla's own fly-away
        // path, which also drives the leaving-skyfaller drawing (PassengerShuttleLeaving reads
        // the stored shuttle), building teardown, and TransportShip disposal. Pods have no
        // TransportShip, so we spawn the leaving skyfaller ourselves. Both start synchronously,
        // so the craft is despawned by the time this returns; only the rise animation is left to
        // play out. Idempotent-safe per launchable (a destroyed launchable is ignored).
        public static void LiftOff(Thing launchable)
        {
            if (launchable?.Destroyed != false)
                return;

            CompTransporter transporter = launchable.TryGetComp<CompTransporter>();
            if (transporter?.innerContainer == null)
                return;

            DetachOccupantsFromLords(transporter);

            // Shuttle: hand it to vanilla's fly-away job. dropMode None keeps the escapees aboard
            // rather than dumping them back onto the platform; with no destination tile set on the
            // job, ShipJob_FlyAway launches without creating a traveling world object.
            CompShuttle shuttle = launchable.TryGetComp<CompShuttle>();
            if (shuttle?.shipParent != null)
            {
                ShipJob_FlyAway flyAway = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
                flyAway.dropMode = TransportShipDropMode.None;
                shuttle.shipParent.ForceJob(flyAway);
                return;
            }

            // Pod / other launchable: no TransportShip, so spawn the leaving skyfaller ourselves
            // (the destination-free tail of ShipJob_FlyAway / CompLaunchable.TryLaunch, minus the
            // fuel and world travel). Drain the pod's launcher first, while it's still spawned.
            DrainFuelingPortSource(launchable);
            LaunchWithoutDestination(launchable, transporter);
        }

        // Best-effort: spend EscapeFuelDrain chemfuel from a transport pod's attached pod
        // launcher (its fueling-port source). No-op when there is nothing to drain - a shuttle
        // or other craft without a CompLaunchable_TransportPod, or a lone pod with no adjacent
        // launcher. CompRefuelable.ConsumeFuel clamps at zero and fires its own out-of-fuel
        // notification, so a launcher with under 50 chemfuel simply empties.
        private static void DrainFuelingPortSource(Thing launchable)
        {
            CompLaunchable_TransportPod podLaunchable = launchable.TryGetComp<CompLaunchable_TransportPod>();
            CompRefuelable fuelSource = podLaunchable?.FuelingPortSource;
            fuelSource?.ConsumeFuel(EscapeFuelDrain);
        }

        // Remove every launchable occupant from its lord so the escape lord shrinks and self-
        // cleans right away. When the craft's takeoff finishes the occupants ExitMap with no
        // lord left to notify.
        private static void DetachOccupantsFromLords(CompTransporter transporter)
        {
            List<Pawn> occupants = transporter.innerContainer.OfType<Pawn>().ToList();
            for (int i = 0; i < occupants.Count; i++)
                occupants[i].GetLord()?.RemovePawn(occupants[i]);
        }

        private static void LaunchWithoutDestination(Thing launchable, CompTransporter transporter)
        {
            Map map = launchable.Map;
            if (map == null)
                return;

            IntVec3 cell = launchable.Position;
            Rot4 rotation = launchable.Rotation;

            // Move the occupants into the active transporter the skyfaller carries up and out.
            ActiveTransporter carrier = (ActiveTransporter)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
            carrier.Contents = new ActiveTransporterInfo();
            carrier.Contents.innerContainer.TryAddRangeOrTransfer(
                transporter.GetDirectlyHeldThings(), canMergeWithExistingStacks: true, destroyLeftover: true);
            carrier.Rotation = rotation;

            ThingDef leavingSkyfaller =
                launchable.TryGetComp<CompLaunchable>()?.Props?.skyfallerLeaving ?? ThingDefOf.DropPodLeaving;

            // Tear the (now-empty) launchable down before the skyfaller takes its cell.
            transporter.CleanUpLoadingVars(map);
            if (!launchable.Destroyed)
                launchable.Destroy(DestroyMode.Vanish);

            // createWorldObject = false -> on reaching the sky the skyfaller ExitMaps its
            // occupants (they escape) and destroys itself, with no traveling world object and no
            // return trip. destinationTile stays Invalid (its default), so no tile is needed.
            FlyShipLeaving skyfaller = (FlyShipLeaving)SkyfallerMaker.MakeSkyfaller(leavingSkyfaller, carrier);
            skyfaller.createWorldObject = false;
            GenSpawn.Spawn(skyfaller, cell, map);
        }
    }
}
