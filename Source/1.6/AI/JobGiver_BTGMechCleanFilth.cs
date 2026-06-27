using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    /// <summary>
    /// Cleansweeper-mech filth cleaning, confined to a moderate radius around the
    /// mech's anchor point AND to the settlement structure footprint (see CleanArea):
    /// it never scans, reserves, or paths to a filth outside the walls or beyond its
    /// home radius.
    ///
    /// Builds a single vanilla JobDefOf.Clean job whose TargetA queue is the nearest
    /// in-range reachable filth plus the cluster around it (capped, nearest-first) -
    /// exactly the shape JobDriver_CleanFilth consumes, so one job sweeps a whole mess
    /// before the duty tree re-evaluates. When no qualifying filth remains this returns
    /// null and the standby node sends the mech home to dormant self-charge.
    /// </summary>
    public class JobGiver_BTGMechCleanFilth : ThinkNode_JobGiver
    {
        // Matches vanilla WorkGiver_CleanFilth's per-job cap: clean a cluster, then
        // re-walk the tree rather than queueing an unbounded sweep.
        private const int MaxQueuedFilth = 15;

        // Vanilla's "let filth settle" gate (WorkGiver_CleanFilth.MinTicksSinceThickened):
        // ignore filth that was last thickened within this many ticks. Stops the mech
        // chasing a mess that is still actively accumulating, and keeps us in step with
        // whatever else vanilla relies on this delay for.
        private const int MinTicksSinceThickened = 600;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
                return null;

            IntVec3 anchor = CleanArea.GetAnchor(pawn);
            if (!anchor.IsValid)
                return null;

            float radiusSq = CleanArea.Radius * CleanArea.Radius;

            List<Thing> allFilth = map.listerThings.ThingsInGroup(ThingRequestGroup.Filth);
            if (allFilth.Count == 0)
                return null;

            // Cheap first pass: keep only visible filth inside the home radius and the
            // structure bounds. No reservation/reachability cost yet.
            List<Thing> candidates = new List<Thing>();
            for (int i = 0; i < allFilth.Count; i++)
            {
                Thing f = allFilth[i];
                if (!(f is Filth filth) || filth.TicksSinceThickened < MinTicksSinceThickened)
                    continue;
                if (f.Fogged())
                    continue;
                if ((f.Position - anchor).LengthHorizontalSquared > radiusSq)
                    continue;
                if (!StructureBoundsCache.Contains(map, f.Position))
                    continue;
                candidates.Add(f);
            }
            if (candidates.Count == 0)
                return null;

            // Nearest-first: the closest reachable filth becomes the primary target and
            // the queued cluster stays tight around the mech.
            IntVec3 pos = pawn.Position;
            candidates.Sort((a, b) => a.Position.DistanceToSquared(pos).CompareTo(b.Position.DistanceToSquared(pos)));

            Job job = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing f = candidates[i];
                if (!pawn.CanReserveAndReach(f, PathEndMode.Touch, Danger.Deadly))
                    continue;

                if (job == null)
                    job = JobMaker.MakeJob(JobDefOf.Clean);
                job.AddQueuedTarget(TargetIndex.A, f);

                if (job.GetTargetQueue(TargetIndex.A).Count >= MaxQueuedFilth)
                    break;
            }
            return job;
        }
    }
}
