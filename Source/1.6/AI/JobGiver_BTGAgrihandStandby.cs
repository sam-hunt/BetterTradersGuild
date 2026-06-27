using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    /// <summary>
    /// Lowest-priority idle state for the agrihand mech: walk back to near its anchor
    /// point and enter the dormant self-charge pose (JobDefOf.SelfShutdown). The shutdown
    /// spot is rooted at the anchor point (not the mech's current cell), so once there is
    /// nothing to harvest, haul, or sow the mech returns home before winding down, and it
    /// is validated to lie inside the structure bounds.
    ///
    /// Wake mechanism is identical to the cleansweeper/paramedic standby and verified
    /// against the decompiled tick path (see JobGiver_BTGMechMedicStandby for the full
    /// trace): the SelfShutdown toil has no tickAction, so the wake is driven entirely by
    /// the job's finite expiryInterval + checkOverrideOnExpire. A shut-down mech still
    /// ticks (self-shutdown is not Pawn.Suspended), the expiry fires ~every 250 ticks with
    /// no enemies required, CheckForJobOverride re-walks the duty tree, and when a crop has
    /// matured (or produce needs shelving) the harvest/haul/sow node returns a job whose
    /// def differs from SelfShutdown so the override is allowed and StartJob springs the
    /// mech up. forceSleep is left false (vanilla JobGiver_SelfShutdown sets it true, which
    /// is why that giver cannot simply be reused as an idle here).
    /// </summary>
    public class JobGiver_BTGAgrihandStandby : ThinkNode_JobGiver
    {
        // Re-evaluate the duty tree at least this often while idle, so the agrihand notices
        // a newly-matured crop without any per-tick scanning.
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
            IntVec3 anchor = FarmArea.GetAnchor(pawn);
            IntVec3 root = anchor.IsValid ? anchor : pawn.Position;

            IntVec3 spot;
            if (!RCellFinder.TryFindNearbyMechSelfShutdownSpot(root, pawn, map, out spot, false)
                || !StructureBoundsCache.Contains(map, spot))
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
