using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Lowest-priority idle state for the paramedic mech: park against a nearby wall of the
    // MedicalBay and enter the dormant self-charge pose (JobDefOf.SelfShutdown, the same job
    // vanilla mechs use when they wind down), tucking itself out of the way instead of
    // shutting down mid-room.
    //
    // Wake mechanism (verified against the decompiled tick path): the SelfShutdown
    // toil itself has NO tickAction and completeMode Never (Toils_LayDown.SelfShutdown),
    // so it never re-checks anything on its own. The wake is driven entirely by the
    // job's finite expiryInterval + checkOverrideOnExpire:
    //   * a shut-down mech still ticks - self-shutdown is not Pawn.Suspended, so
    //     Pawn.TickInterval keeps calling JobTrackerTickInterval;
    //   * JobTrackerTickInterval fires the expiry ~every 250 ticks (no enemies
    //     required - expireRequiresEnemiesNearby defaults false) and, because
    //     checkOverrideOnExpire is set, calls CheckForJobOverride();
    //   * CheckForJobOverride re-walks the full think tree (our duty node included);
    //     when a casualty exists a tend/rescue job is returned, its def differs from
    //     SelfShutdown so ShouldStartJobFromThinkTree allows the override, and StartJob
    //     cleanly cleans up the shutdown (its finish-action resets asleep=false).
    // Net: stays dormant with zero per-tick cost when idle, springs up within ~4-8s
    // of a casualty appearing. (forceSleep is left false; vanilla JobGiver_SelfShutdown
    // sets it true, which is why it cannot simply be reused as an idle here.)
    //
    // The shutdown spot is constrained to the medbay rects; if none is found there
    // the mech shuts down on its current cell (it is always standing in the medbay
    // when idle). Non-mech / no-Biotech pawns fall back to a short maintain-posture
    // wait, which re-checks the tree on the same expiry.
    public class JobGiver_BTGMechMedicStandby : ThinkNode_JobGiver
    {
        // Re-evaluate the duty tree at least this often while idle, so the medic
        // notices a new casualty without any per-tick scanning.
        private const int IdleRecheckTicks = 250;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || !pawn.RaceProps.IsMechanoid)
            {
                Job wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                wait.expiryInterval = IdleRecheckTicks;
                wait.checkOverrideOnExpire = true;
                return wait;
            }

            List<CellRect> rects = MedicRoomBounds.GetRects(pawn);
            IntVec3 root = MechIdlePark.RootFor(pawn, rects, pawn.Position);

            IntVec3 spot;
            if (!RCellFinder.TryFindNearbyMechSelfShutdownSpot(root, pawn, pawn.Map, out spot, false)
                || (rects != null && !MedicRoomBounds.Contains(rects, spot)))
            {
                spot = pawn.Position;
            }

            Job job = JobMaker.MakeJob(JobDefOf.SelfShutdown, spot);
            job.forceSleep = false;
            job.expiryInterval = IdleRecheckTicks;
            job.checkOverrideOnExpire = true;
            return job;
        }
    }
}
