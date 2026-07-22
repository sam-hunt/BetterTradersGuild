using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Escape-phase first step: open the way out of the locked crib subroom by hacking its
    // AncientBlastDoor. AncientBlastDoor is reused as the locked-door def by every other
    // subroom prefab (CrewBedSubroom*, CommandersBedroom, ServerRacks_Subroom) and by the
    // settlement's own perimeter airlock (AirlockDefences.xml), so distance from the
    // subroom-centre focus alone isn't a safe filter - a compact layout can put an unrelated
    // door, including the perimeter, within radius. Candidates are filtered to the duty radius
    // AND to doors that actually border the focus cell's Room (see BordersRoom), so this never
    // opens the wider settlement's perimeter.
    //
    // All hack eligibility - not already hacked, not in lockout, pawn capable (manipulation +
    // intellectual prerequisite), door reachable - is delegated to vanilla
    // CompHackable.CanHackNow, exactly like JobGiver_BTGHackDoorForFood. So a child who can't
    // hack simply gets no job here and the caretaker does it; once the door is open this
    // returns null and the walker falls through to carrying/boarding.
    public class JobGiver_BTGHackShelterDoor : ThinkNode_JobGiver
    {
        private const float SubroomRadius = 8f;

        // The largest known subroom prefab is 6x6 = 36 footprint cells (CommandersBedroom,
        // ServerRacks_Subroom, CribSubroom); this leaves comfortable slack above that without
        // approaching corridor/perimeter scale, so it's used below to detect a room that's been
        // fused into something bigger than any real subroom (e.g. by a breach).
        private const int MaxPlausibleSubroomCells = 64;

        protected override Job TryGiveJob(Pawn pawn)
        {
            ThingDef doorDef = Things.AncientBlastDoor;
            if (doorDef == null)
                return null;

            PawnDuty duty = pawn.mindState?.duty;
            IntVec3 focus = (duty != null && duty.focus.IsValid) ? duty.focus.Cell : pawn.Position;

            Map map = pawn.Map;
            // Doors sit in their own portal region/"doorway" room, distinct from either room
            // they connect, so GetRoom(focus) here resolves to the crib subroom's actual
            // interior room (focus is the subroom centre, never a door cell).
            Room focusRoom = focus.GetRoom(map);
            // A merged room bigger than any real subroom means a breach has fused the crib
            // subroom into the corridor or further, so membership can no longer distinguish it
            // from the perimeter - trusting radius alone in that case is exactly the failure
            // mode this fix closes. Rather than risk it, no candidate qualifies; the breach
            // itself already gives the civilian a physical way out, so this doesn't strand
            // anyone. A null focusRoom (no region resolved for the focus cell) can't be tested
            // for membership at all, so that case instead falls back to the pre-existing
            // radius-only filter below rather than stranding the civilian.
            bool roomTooBigToTrust = focusRoom != null && focusRoom.CellCount > MaxPlausibleSubroomCells;

            List<Thing> doors = map.listerThings.ThingsOfDef(doorDef);

            Building_HackableDoor best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < doors.Count; i++)
            {
                if (!(doors[i] is Building_HackableDoor door))
                    continue;
                if ((door.Position - focus).LengthHorizontal > SubroomRadius)
                    continue;
                if (roomTooBigToTrust)
                    continue;
                if (focusRoom != null && !BordersRoom(door.Position, focusRoom, map))
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

            return best != null ? JobMaker.MakeJob(DefRefs.Jobs.BTG_Hack, best) : null;
        }

        // A door's own cell resolves to its portal region's "doorway" room, never the interior
        // room on either side, so membership can't be tested on the door cell itself - it's
        // tested via the door's cardinal neighbours instead: a door in the crib subroom's own
        // wall has the subroom interior as one of its neighbours, while unrelated doors and the
        // perimeter airlock never do.
        private static bool BordersRoom(IntVec3 doorCell, Room room, Map map)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 neighbor = doorCell + GenAdj.CardinalDirections[i];
                if (neighbor.InBounds(map) && room.ContainsCell(neighbor))
                    return true;
            }
            return false;
        }
    }
}
