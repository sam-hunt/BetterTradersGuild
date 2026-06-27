using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Rest response for bounded defenders. Placed LAST in the duty think tree, just
    // above idle wander, so it is only reached when nothing more important wants the
    // pawn: no critical-withdraw, no in-structure combat target, no wounds to tend, no
    // hunger. That ordering is itself the "no immediate threat" gate - a defender only
    // lies down once combat acquires nothing inside the structure.
    //
    // Waking is entirely vanilla and needs no help from us:
    //   - JobDriver_LayDown's toil calls CheckForJobOverride() every 211 ticks while
    //     sleeping (LookForOtherJobs == true). That re-runs the whole duty tree top-down,
    //     and combat sits ABOVE this node, so a hostile that has entered the structure
    //     perimeter pulls the defender back into the fight within ~3.5s.
    //   - Pawn_JobTracker.Notify_DamageTaken calls CheckForJobOverride() immediately, so a
    //     defender that is shot while asleep wakes the same tick.
    //   - RestUtility.CanFallAsleep refuses sleep for 400 ticks after any disturbance, so
    //     a defender woken by a brief skirmish does not instantly flop back down.
    //
    // Why subclass JobGiver_GetRest rather than use it directly: the duty tree is a
    // ThinkNode_Priority, whose TryIssueJobPackage ignores GetPriority() and only uses the
    // first non-null TryGiveJob in order. Vanilla GetRest puts its tiredness gate ("rest
    // only when the rest need is low") entirely in GetPriority - bypassed here - so its
    // raw TryGiveJob would hand back a LayDown job even when fully rested. We re-apply the
    // gate in the TryGiveJob path, then reuse base.TryGiveJob (and through it
    // RestUtility.FindBedFor) for the actual bed/spot selection, and finally constrain the
    // chosen spot to the structure footprint.
    //
    // Mechs fall out immediately (no rest need). Combat-leashing, containment, and the
    // "tiredness is an accepted debuff" stance match the duty's other nodes.
    public class JobGiver_BTGRestInStructure : JobGiver_GetRest
    {
        // Lowest rest category that makes a defender lie down. Defaults to Tired (rest
        // below ~28%), matching vanilla's GetPriority threshold for an "Anything" timetable.
        public RestCategory minRestCategory = RestCategory.Tired;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_BTGRestInStructure)base.DeepCopy(resolve);
            copy.minRestCategory = minRestCategory;
            return copy;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            // Re-apply the gates vanilla keeps in GetPriority (bypassed by ThinkNode_Priority):
            // a real rest need, tired enough, not just woken (canSleepTick), and able to sleep
            // at all. Mechs have no rest need and stop here.
            Need_Rest rest = pawn.needs?.rest;
            if (rest == null || (int)rest.CurCategory < (int)minRestCategory)
                return null;
            if (Find.TickManager.TicksGame < pawn.mindState.canSleepTick)
                return null;
            if (!RestUtility.CanFallAsleep(pawn))
                return null;

            // Vanilla bed/ground-spot selection (reuses RestUtility.FindBedFor and the
            // lord-toil AllowRestingInBed gate, both of which our toil leaves at default).
            Job job = base.TryGiveJob(pawn);
            if (job == null)
                return null;

            // Containment: never path to a bed or ground spot outside the structure
            // footprint. FindBedFor runs an unbounded whole-map search and the ground
            // fallback picks a cell near the pawn; in practice every settlement bed is
            // inside the rect union, but a result outside it would be a straying vector.
            // If the chosen spot is outside, hold post instead - tiredness is an accepted
            // debuff, the same stance as the duty's other nodes. (Permissive when no
            // layout is known, matching the forager.)
            if (StructureBoundsCache.GetRoomRects(pawn.Map) != null)
            {
                LocalTargetInfo target = job.targetA;
                IntVec3 spot = target.HasThing ? target.Thing.Position : target.Cell;
                if (!StructureBoundsCache.Contains(pawn.Map, spot))
                    return null;
            }
            return job;
        }
    }
}
