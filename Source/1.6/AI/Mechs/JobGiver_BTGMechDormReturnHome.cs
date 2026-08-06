using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // First node of the dorm duty: walk the mech back into its home room before the
    // standby node parks it. Dorm mechs have no work that takes them out, but combat
    // self-defense (the constant think tree) or player interference can displace them;
    // without this they would self-shutdown wherever that left them, because the
    // standby node only parks against a wall of the rect the mech is standing in and
    // shuts down in place anywhere outside its room.
    //
    // Returns null the moment the mech is already inside its home rects, handing off
    // to the standby node to pick the actual wall-park / self-shutdown spot.
    public class JobGiver_BTGMechDormReturnHome : ThinkNode_JobGiver
    {
        // Re-walk the duty tree at least this often while heading home.
        private const int WalkRecheckTicks = 250;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Map == null)
                return null;

            List<CellRect> rects = HomeRoomArea.GetRects(pawn);
            if (rects == null || HomeRoomArea.Contains(rects, pawn.Position))
                return null; // no home known, or already home - let standby park it

            if (!MechReturnHome.TryFindHomeCell(pawn, HomeRoomArea.GetAnchor(pawn), rects, out IntVec3 dest))
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job.expiryInterval = WalkRecheckTicks;
            job.checkOverrideOnExpire = true;
            return job;
        }
    }
}
