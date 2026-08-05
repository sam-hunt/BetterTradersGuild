using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Sits between the structure-wide pot planting and the standby self-charge: once the
    // agrihand has no crop or pot work left anywhere, walk it back to its home room (the
    // greenhouse it is anchored to) before it winds down. Without this the mech would
    // self-shutdown wherever it finished its last pot - stranded in a corridor or bedroom -
    // because the standby node only parks it against a wall of the rect it happens to be
    // standing in. Returning home first keeps every agrihand asleep in the greenhouse where
    // its primary duty (rice farming) re-triggers.
    //
    // Because the pot-planting node outranks this one, the mech paths pot-to-pot while any
    // empty pot remains and only makes the trip home once its whole structure-wide round is
    // done. checkOverrideOnExpire lets a crop maturing mid-walk pull it straight back to the
    // harvest node without finishing the walk home.
    //
    // Returns null the moment the mech is already inside its home rects, handing off to the
    // standby node to pick the actual wall-park / self-shutdown spot.
    public class JobGiver_BTGAgrihandReturnHome : ThinkNode_JobGiver
    {
        // Re-walk the duty tree at least this often while heading home, so higher-priority
        // farm work (a freshly-matured crop) can interrupt the trip.
        private const int WalkRecheckTicks = 250;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
                return null;

            List<CellRect> rects = FarmArea.GetRects(pawn);
            if (rects == null || FarmArea.Contains(rects, pawn.Position))
                return null; // no home known, or already home - let standby park it

            if (!TryFindHomeCell(pawn, rects, out IntVec3 dest))
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job.expiryInterval = WalkRecheckTicks;
            job.checkOverrideOnExpire = true;
            return job;
        }

        // Prefer the anchor (the lord's greenhouse centre); if it is unstandable or
        // unreachable, fall back to the nearest standable, reachable cell in the home rects.
        private static bool TryFindHomeCell(Pawn pawn, List<CellRect> rects, out IntVec3 dest)
        {
            Map map = pawn.Map;
            IntVec3 anchor = FarmArea.GetAnchor(pawn);
            if (anchor.IsValid && anchor.Standable(map)
                && pawn.CanReach(anchor, PathEndMode.OnCell, Danger.Deadly))
            {
                dest = anchor;
                return true;
            }

            IntVec3 from = anchor.IsValid ? anchor : pawn.Position;
            IntVec3 best = IntVec3.Invalid;
            int bestDistSq = int.MaxValue;
            for (int i = 0; i < rects.Count; i++)
            {
                foreach (IntVec3 c in rects[i])
                {
                    if (!c.Standable(map))
                        continue;
                    int distSq = (c - from).LengthHorizontalSquared;
                    if (distSq >= bestDistSq)
                        continue;
                    if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly))
                        continue;
                    bestDistSq = distSq;
                    best = c;
                }
            }

            dest = best;
            return best.IsValid;
        }
    }
}
