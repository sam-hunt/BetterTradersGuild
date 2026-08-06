using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Re-arms an unarmed bounded defender after it recovers from being downed.
    //
    // Vanilla remembers the primary a pawn drops on downing (mindState.droppedWeapon,
    // set by MakeDowned) but only ever acts on it for colonists: the sole
    // JobGiver_PickupDroppedWeapon node sits inside ThinkNode_ConditionalColonist in
    // Humanlike.xml, so a faction defender never evaluates it. Worse, the memory
    // itself is wiped by Pawn.DeSpawn the instant the paramedic mech picks the
    // casualty up (the rescue drivers stash and restore it - see JobDriver_BTGRescue),
    // and it can also die legitimately (weapon destroyed, looted). So this node works
    // in two phases:
    //
    //   1. Own weapon first: if mindState.droppedWeapon still stands anywhere on the
    //      map, walk back and re-equip it via the vanilla helper (which checks
    //      reachability/reservation and carries ignoreForbidden - MakeDowned forbids
    //      the drop).
    //   2. Fallback: nearest decent unreserved weapon INSIDE the structure footprint
    //      (StructureBoundsCache rect union - same bounds every other garrison node
    //      uses), so a defender whose own gun is gone re-arms from wherever the
    //      halls provide, armory shelves included. Junk melee "weapons" below
    //      bare-fist DPS + 2 (wood logs, beer) are rejected with the same threshold
    //      vanilla's opportunistic pickup uses.
    //
    // Sits ABOVE combat in the duty tree: an unarmed defender contributes nothing at
    // range and would otherwise fist-charge intruders; fetching a gun IS its combat
    // move. Armed pawns fall through in O(1) (Primary != null), so the whole garrison
    // skips this node in the common case. The critical-wound withdraw-and-tend node
    // stays above. Jobs expire-and-recheck so the tree stays responsive mid-fetch.
    // No-op for mechs (weapons are built in) via the Humanlike gate.
    public class JobGiver_BTGRecoverWeapon : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike || pawn.equipment == null)
                return null;
            if (pawn.equipment.Primary != null)
                return null;
            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
                return null;
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;
            if (pawn.GetRegion() == null)
                return null;

            // Phase 1: the weapon this pawn dropped when it went down, wherever it lies.
            Thing dropped = pawn.mindState?.droppedWeapon;
            if (dropped?.Spawned == true && dropped.Map == pawn.Map && !dropped.IsBurning())
            {
                Job job = JobGiver_PickupDroppedWeapon.PickupWeaponJob(pawn, dropped, ignoreForbidden: true);
                if (job != null)
                    return WithRecheck(job);
            }

            // Phase 2: nearest acceptable weapon inside the structure footprint.
            Map map = pawn.Map;
            Thing weapon = GenClosest.ClosestThingReachable(
                pawn.Position, map, ThingRequest.ForGroup(ThingRequestGroup.Weapon),
                PathEndMode.Touch, TraverseParms.For(pawn), 9999f,
                t => !t.IsBurning()
                     && StructureBoundsCache.Contains(map, t.Position)
                     && ShouldEquip(t, pawn)
                     && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly),
                lookInHaulSources: true);
            if (weapon == null)
                return null;

            Job equip = JobMaker.MakeJob(JobDefOf.Equip, weapon);
            equip.ignoreForbidden = true;
            return WithRecheck(equip);
        }

        // Re-run the tree periodically mid-fetch so the nodes above (critical-wound
        // withdrawal) can preempt; an unchanged result continues the walk seamlessly.
        private static Job WithRecheck(Job job)
        {
            job.expiryInterval = 200;
            job.checkOverrideOnExpire = true;
            return job;
        }

        private static bool ShouldEquip(Thing weapon, Pawn pawn)
        {
            if (weapon.def.IsRangedWeapon && pawn.WorkTagIsDisabled(WorkTags.Shooting))
                return false;
            if (weapon.def.IsMeleeWeapon
                && weapon.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS) < MinMeleeWeaponDpsThreshold)
                return false;
            // Rejects biocode/bladelink locks to other pawns, among others.
            return EquipmentUtility.CanEquip(weapon, pawn);
        }

        // Vanilla JobGiver_PickUpOpportunisticWeapon's junk-melee cutoff: bare-fist
        // DPS + 2. Below this a "weapon" (wood log, beer bottle) beats nothing.
        private static float MinMeleeWeaponDpsThreshold
        {
            get
            {
                foreach (Tool tool in ThingDefOf.Human.tools)
                {
                    if (tool.linkedBodyPartsGroup == BodyPartGroupDefOf.LeftHand
                        || tool.linkedBodyPartsGroup == BodyPartGroupDefOf.RightHand)
                        return tool.power / tool.cooldownTime + 2f;
                }
                return 2f;
            }
        }
    }
}
