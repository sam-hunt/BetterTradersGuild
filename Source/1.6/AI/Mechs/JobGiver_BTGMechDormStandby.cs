using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // The dorm duty's resting state (Lifter, Fabricor): park against a nearby wall of
    // the home room and enter the dormant self-charge pose (JobDefOf.SelfShutdown).
    // The shutdown spot is rooted at a wall-adjacent cell near the mech (see
    // MechIdlePark), not the room centre it is pinned to, so it tucks itself out of
    // the way; it is validated to lie inside the mech's own room (see HomeRoomArea).
    //
    // Wake mechanism is identical to the paramedic standby and verified against the
    // decompiled tick path (see JobGiver_BTGMechMedicStandby for the full trace): the
    // SelfShutdown toil has no tickAction, so the wake is driven entirely by the job's
    // finite expiryInterval + checkOverrideOnExpire, which re-walks the duty tree
    // ~every 250 ticks. Today the only node above is the return-home walk, so the mech
    // effectively sleeps until disturbed; any future work nodes added to BTG_MechDorm
    // (hauling, crafting) will wake it through the same override path with no changes
    // here. forceSleep is left false (vanilla JobGiver_SelfShutdown sets it true,
    // which is why that giver cannot simply be reused as an idle here).
    public class JobGiver_BTGMechDormStandby : ThinkNode_JobGiver
    {
        // Re-evaluate the duty tree at least this often while dormant.
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
            List<CellRect> rects = HomeRoomArea.GetRects(pawn);
            IntVec3 root = MechIdlePark.RootFor(pawn, rects, pawn.Position);

            IntVec3 spot;
            if (!RCellFinder.TryFindNearbyMechSelfShutdownSpot(root, pawn, map, out spot, false)
                || !HomeRoomArea.Contains(rects, spot))
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
