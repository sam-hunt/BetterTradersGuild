using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Open the way out of the locked crib subroom by hacking its AncientBlastDoor. First step
    // of the escape duty, and the sealed-in safety net of the stranded duties (stranding can
    // fire while the door is still locked, e.g. when no launchable exists at all - without
    // this the stranded wider-structure forage can't path out and the family starves boxed in).
    //
    // Candidate discovery (radius + focus-room membership, so this never opens an unrelated
    // subroom or the perimeter airlock) lives in ShelterDoorHelper, shared with the lord's
    // escape/stranded transition. All hack eligibility - not already hacked, not in lockout,
    // pawn capable (manipulation + intellectual prerequisite), door reachable - is delegated
    // to vanilla CompHackable.CanHackNow, exactly like JobGiver_BTGHackDoorForFood. So a child
    // who can't hack simply gets no job here and the caretaker does it; once the door is open
    // this returns null and the walker falls through to the rest of its duty.
    public class JobGiver_BTGHackShelterDoor : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState?.duty;
            IntVec3 focus = (duty != null && duty.focus.IsValid) ? duty.focus.Cell : pawn.Position;

            List<Building_HackableDoor> doors = ShelterDoorHelper.ShelterDoors(focus, pawn.Map);

            Building_HackableDoor best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < doors.Count; i++)
            {
                Building_HackableDoor door = doors[i];
                CompHackable hackable = door.Hackable;
                if (hackable == null || !hackable.CanHackNow(pawn).Accepted)
                    continue;
                if (!pawn.CanReserve(door))
                    continue;

                float distSq = (pawn.Position - door.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = door;
                }
            }

            return best != null ? JobMaker.MakeJob(DefRefs.Jobs.BTG_Hack, best) : null;
        }
    }
}
