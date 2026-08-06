using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Sits between the feed node and the standby self-charge: once the medic has no
    // tend/rescue/feed work left, walk it back to its MedicalBay before it winds down.
    // Without this the mech would self-shutdown wherever an out-of-room trip ended -
    // a rescue fetch whose casualty died or got up mid-walk, a longjump that landed it
    // deep in the structure - because the standby node only parks it against a wall of
    // the rect it happens to be standing in, and vetoes any spot outside the medbay by
    // shutting down in place. Returning home first keeps the medic asleep in the medbay
    // where its casualties arrive.
    //
    // Because every work node outranks this one, the mech only makes the trip home once
    // no medical work remains anywhere. checkOverrideOnExpire lets a new casualty
    // mid-walk pull it straight back to the tend/rescue nodes without finishing the
    // walk home.
    //
    // Returns null the moment the mech is already inside its medbay rects, handing off
    // to the standby node to pick the actual wall-park / self-shutdown spot.
    public class JobGiver_BTGMechMedicReturnHome : ThinkNode_JobGiver
    {
        // Re-walk the duty tree at least this often while heading home, so a fresh
        // casualty can interrupt the trip.
        private const int WalkRecheckTicks = 250;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Map == null)
                return null;

            List<CellRect> rects = MedicRoomBounds.GetRects(pawn);
            if (rects == null || MedicRoomBounds.Contains(rects, pawn.Position))
                return null; // no home known, or already home - let standby park it

            if (!MechReturnHome.TryFindHomeCell(pawn, MedicRoomBounds.GetAnchor(pawn), rects, out IntVec3 dest))
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job.expiryInterval = WalkRecheckTicks;
            job.checkOverrideOnExpire = true;
            return job;
        }
    }
}
