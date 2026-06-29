using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Lowest-priority idle state for the cleansweeper mech: park against a nearby wall and
    // enter the dormant self-charge pose (JobDefOf.SelfShutdown). The shutdown spot is
    // rooted at a wall-adjacent cell near the mech (see MechIdlePark), not the room centre
    // it is pinned to, so when the last filth is gone the mech tucks itself out of the way
    // before winding down; it is validated to lie inside the mech's own room (see CleanArea).
    //
    // Wake mechanism is identical to the paramedic standby and verified against the
    // decompiled tick path (see JobGiver_BTGMechMedicStandby for the full trace): the
    // SelfShutdown toil has no tickAction, so the wake is driven entirely by the job's
    // finite expiryInterval + checkOverrideOnExpire. A shut-down mech still ticks
    // (self-shutdown is not Pawn.Suspended), the expiry fires ~every 250 ticks with no
    // enemies required, CheckForJobOverride re-walks the duty tree, and when fresh
    // filth has appeared in range the clean node returns a Clean job whose def differs
    // from SelfShutdown so the override is allowed and StartJob springs the mech up.
    // forceSleep is left false (vanilla JobGiver_SelfShutdown sets it true, which is
    // why that giver cannot simply be reused as an idle here).
    public class JobGiver_BTGMechCleanStandby : ThinkNode_JobGiver
    {
        // Re-evaluate the duty tree at least this often while idle, so the cleansweeper
        // notices new filth without any per-tick scanning.
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

            Map map = pawn.Map;
            List<CellRect> rects = CleanArea.GetRects(pawn);
            IntVec3 root = MechIdlePark.RootFor(pawn, rects, pawn.Position);

            IntVec3 spot;
            if (!RCellFinder.TryFindNearbyMechSelfShutdownSpot(root, pawn, map, out spot, false)
                || !CleanArea.Contains(rects, spot))
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
