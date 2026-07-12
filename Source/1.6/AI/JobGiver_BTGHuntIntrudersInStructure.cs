using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Pursuit node for the bounded defender duty. The combat node above this one
    // (JobGiver_BTGDefendStructure) only engages targets the pawn can currently
    // see: vanilla JobGiver_AIFightEnemy target acquisition requires line of
    // sight (TargetScanFlags.NeedLOSToPawns), and with chaseTarget=false an
    // acquired target is dropped the moment the fresh scan loses sight of it.
    // Without this node, intruders looting out-of-sight rooms are simply
    // ignored while the garrison drifts back to eating, sleeping, and idling.
    //
    // This node fires when a live hostile stands inside the structure footprint
    // but nothing is visible: the defender paths toward the nearest reachable
    // one (vanilla JobGiver_AIGotoNearestHostile pattern - a Goto with
    // checkOverrideOnExpire, so the think tree re-runs as the pawn closes in
    // and the combat node takes over the moment the target comes into view).
    // Targets outside the structure rect union are invisible to this scan, so
    // defenders sweep their own halls but never pursue out into vacuum.
    public class JobGiver_BTGHuntIntrudersInStructure : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
                return null;

            Thing closest = null;
            float closestDistSq = float.MaxValue;

            List<IAttackTarget> targets = map.attackTargetsCache.GetPotentialTargetsFor(pawn);
            for (int i = 0; i < targets.Count; i++)
            {
                IAttackTarget target = targets[i];
                if (target.ThreatDisabled(pawn))
                    continue;
                if (!AttackTargetFinder.IsAutoTargetable(target))
                    continue;

                Thing thing = target.Thing;
                if (!StructureBoundsCache.Contains(map, thing.Position))
                    continue;

                float distSq = thing.Position.DistanceToSquared(pawn.Position);
                if (distSq < closestDistSq && pawn.CanReach(thing, PathEndMode.OnCell, Danger.Deadly))
                {
                    closestDistSq = distSq;
                    closest = thing;
                }
            }

            if (closest == null)
                return null;

            // Already on top of (or adjacent to) the target: no move to make -
            // the combat node above takes the shot on the next think tick.
            if (closest.PositionHeld == pawn.PositionHeld
                || ReachabilityImmediate.CanReachImmediate(pawn, closest, PathEndMode.Touch))
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.Goto, closest);
            // Distance-scaled expiry with override checks: the tree re-evaluates
            // as the pawn closes in, handing off to the combat node on first
            // sight instead of blindly finishing the walk.
            job.intervalScalingTarget = TargetIndex.A;
            job.checkOverrideOnExpire = true;
            job.expireRequiresEnemiesNearby = true;
            job.collideWithPawns = true;
            return job;
        }
    }
}
