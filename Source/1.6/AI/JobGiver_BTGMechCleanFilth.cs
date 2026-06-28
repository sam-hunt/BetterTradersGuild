using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Cleansweeper-mech filth cleaning, confined strictly to the one layout room the mech
    // was spawned into (see CleanArea): it never scans, reserves, or paths to filth in any
    // other room or outside the walls. That room is the mech's "work area" - the role the
    // player's painted Home area plays for a player cleansweeper.
    //
    // Builds a single BTG_Clean job (vanilla Clean minus the player Home-area gate; see
    // JobDriver_BTGCleanFilth) whose TargetA queue is the nearest in-room reachable filth
    // plus the cluster around it (capped, nearest-first) - exactly the shape vanilla
    // WorkGiver_CleanFilth hands its driver, so one job sweeps a whole mess before the duty
    // tree re-evaluates. When no qualifying filth remains this returns null and the standby
    // node sends the mech home to dormant self-charge.
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

            List<CellRect> rects = CleanArea.GetRects(pawn);
            if (rects == null)
                return null;

            List<Thing> allFilth = map.listerThings.ThingsInGroup(ThingRequestGroup.Filth);
            if (allFilth.Count == 0)
                return null;

            // Cheap first pass: keep only visible filth inside the mech's room. No
            // reservation/reachability cost yet.
            List<Thing> candidates = new List<Thing>();
            for (int i = 0; i < allFilth.Count; i++)
            {
                Thing f = allFilth[i];
                if (!(f is Filth filth) || filth.TicksSinceThickened < MinTicksSinceThickened)
                    continue;
                if (f.Fogged())
                    continue;
                if (!CleanArea.Contains(rects, f.Position))
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
                    job = JobMaker.MakeJob(Jobs.BTG_Clean);
                job.AddQueuedTarget(TargetIndex.A, f);

                if (job.GetTargetQueue(TargetIndex.A).Count >= MaxQueuedFilth)
                    break;
            }
            return job;
        }
    }
}
