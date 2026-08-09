using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.AI.Civilians
{
    // Locates the crib subroom's own blast door(s) for the sheltering-civilian lord.
    // Shared by JobGiver_BTGHackShelterDoor (which door to hack) and
    // LordJob_BTGShelterCivilians (is the family still sealed in by a door it could open?).
    //
    // AncientBlastDoor is reused as the locked-door def by every other subroom prefab
    // (CrewBedSubroom*, CommandersBedroom, ServerRacks_Subroom) and by the settlement's own
    // perimeter airlock (AirlockDefences.xml), so distance from the subroom-centre focus alone
    // isn't a safe filter - a compact layout can put an unrelated door, including the
    // perimeter, within radius. Candidates are filtered to the radius AND to doors that
    // actually border the focus cell's Room (see BordersRoom), so this never selects the
    // wider settlement's perimeter.
    public static class ShelterDoorHelper
    {
        private const float SubroomRadius = 8f;

        // The largest known subroom prefab is 6x6 = 36 footprint cells (CommandersBedroom,
        // ServerRacks_Subroom, CribSubroom); this leaves comfortable slack above that without
        // approaching corridor/perimeter scale, so it's used to detect a room that's been
        // fused into something bigger than any real subroom (e.g. by a breach) - both below
        // and by LordJob_BTGShelterCivilians.ShelterCompromised.
        public const int MaxPlausibleSubroomCells = 64;

        // All blast doors belonging to the subroom around the focus cell. Empty when the focus
        // room has been breached into something too big to trust (the breach itself is the way
        // out then); when the focus cell resolves no room at all, falls back to the radius-only
        // filter rather than returning nothing.
        public static List<Building_HackableDoor> ShelterDoors(IntVec3 focus, Map map)
        {
            var result = new List<Building_HackableDoor>();
            ThingDef doorDef = Things.AncientBlastDoor;
            if (doorDef == null || map == null)
                return result;

            // Doors sit in their own portal region/"doorway" room, distinct from either room
            // they connect, so GetRoom(focus) here resolves to the crib subroom's actual
            // interior room (focus is the subroom centre, never a door cell).
            Room focusRoom = focus.GetRoom(map);
            // A merged room bigger than any real subroom means a breach has fused the crib
            // subroom into the corridor or further, so membership can no longer distinguish it
            // from the perimeter - trusting radius alone in that case is exactly the failure
            // mode this filter closes. Rather than risk it, no candidate qualifies; the breach
            // itself already gives the civilian a physical way out, so this doesn't strand
            // anyone.
            if (focusRoom?.CellCount > MaxPlausibleSubroomCells)
                return result;

            List<Thing> doors = map.listerThings.ThingsOfDef(doorDef);
            for (int i = 0; i < doors.Count; i++)
            {
                if (!(doors[i] is Building_HackableDoor door))
                    continue;
                if ((door.Position - focus).LengthHorizontal > SubroomRadius)
                    continue;
                if (focusRoom != null && !BordersRoom(door.Position, focusRoom, map))
                    continue;
                result.Add(door);
            }
            return result;
        }

        // True while a still-locked shelter door seals the subroom AND some living, un-downed
        // ADULT walker could hack it open right now (CompHackable.CanHackNow: not locked out,
        // pawn capable, door reachable, skill prerequisite met). Children never hack by design,
        // so the adult gate here mirrors JobGiver_BTGHackShelterDoor - the two must agree or
        // the lord would hold the escape open waiting for a hack no walker will perform. Used
        // by the lord's escape/stranded transition: a locked hackable door blocks CanReach even
        // for its own faction, so launchable reachability is ALWAYS false during the door-hack
        // prelude of an escape - while this returns true the escape is still opening the way
        // out, not failed.
        public static bool AnyWalkerCanOpenShelterDoor(List<Pawn> walkers, IntVec3 focus, Map map)
        {
            if (walkers == null)
                return false;

            List<Building_HackableDoor> doors = ShelterDoors(focus, map);
            for (int i = 0; i < doors.Count; i++)
            {
                Building_HackableDoor door = doors[i];
                // An already-hacked door isn't sealing anyone in (CanHackNow would refuse it
                // anyway; checked here first because it's cheap).
                if (!door.Locked || door.Hackable == null)
                    continue;

                for (int j = 0; j < walkers.Count; j++)
                {
                    Pawn pawn = walkers[j];
                    if (pawn?.Dead != false || pawn.Downed)
                        continue;
                    if (!pawn.DevelopmentalStage.Adult())
                        continue;
                    if (door.Hackable.CanHackNow(pawn).Accepted)
                        return true;
                }
            }
            return false;
        }

        // True when some shelter door exists but is no longer Locked - i.e. the seal has been
        // opened from OUTSIDE the family's own plan (player hack, scenario scripting): while
        // sheltering, no walker duty ever touches the door, so an unlocked door always means an
        // external actor. Distinct from a DESTROYED door, which merges the subroom into a
        // larger room and is caught by the room-size breach check instead (ShelterDoors then
        // returns empty, so this stays false - correctly, since the breach check already fired).
        // Used by LordJob_BTGShelterCivilians.ShelterCompromised: an unsealed shelter no longer
        // protects anyone, so the family bolts rather than waiting out starvation behind an
        // open door.
        public static bool AnyShelterDoorUnlocked(IntVec3 focus, Map map)
        {
            List<Building_HackableDoor> doors = ShelterDoors(focus, map);
            for (int i = 0; i < doors.Count; i++)
            {
                if (!doors[i].Locked)
                    return true;
            }
            return false;
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
