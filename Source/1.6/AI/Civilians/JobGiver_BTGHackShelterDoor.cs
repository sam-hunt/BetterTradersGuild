using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Escape-phase first step: open the way out of the locked crib subroom by hacking its
    // AncientBlastDoor. Only the subroom's own door is targeted (filtered to the duty radius
    // of the subroom-centre focus), so this never opens the wider settlement's perimeter.
    //
    // All hack eligibility - not already hacked, not in lockout, pawn capable (manipulation +
    // intellectual prerequisite), door reachable - is delegated to vanilla
    // CompHackable.CanHackNow, exactly like JobGiver_BTGHackDoorForFood. So a child who can't
    // hack simply gets no job here and the caretaker does it; once the door is open this
    // returns null and the walker falls through to carrying/boarding.
    public class JobGiver_BTGHackShelterDoor : ThinkNode_JobGiver
    {
        private const float SubroomRadius = 8f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            ThingDef doorDef = Things.AncientBlastDoor;
            if (doorDef == null)
                return null;

            PawnDuty duty = pawn.mindState?.duty;
            IntVec3 focus = (duty != null && duty.focus.IsValid) ? duty.focus.Cell : pawn.Position;

            Map map = pawn.Map;
            List<Thing> doors = map.listerThings.ThingsOfDef(doorDef);

            Building_HackableDoor best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < doors.Count; i++)
            {
                if (!(doors[i] is Building_HackableDoor door))
                    continue;
                if ((door.Position - focus).LengthHorizontal > SubroomRadius)
                    continue;
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

            return best != null ? JobMaker.MakeJob(JobDefOf.Hack, best) : null;
        }
    }
}
