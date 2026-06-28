using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Break-out-to-eat fallback for bounded defenders that spawned trapped. Settlement
    // pawns are scattered across the structure at map-gen, and some land in a side room
    // (bedroom, etc.) sealed by a locked AncientBlastDoor. Such a pawn can reach none of
    // the food the nodes above rely on - floor items, meal pallets, paste taps, or a comms
    // console - so without this it would either starve in place or pointlessly batter the
    // door (vanilla's desperate response to being walled in).
    //
    // Sits BELOW the forager (JobGiver_BTGForageInStructure) AND the resupply caller
    // (JobGiver_BTGCallResupply) in the duty think tree, and is gated to Starving. The
    // ordering is the trigger: a pawn only breaks out once every reachable in-structure
    // food source and the resupply path have come up empty - i.e. it really is sealed off.
    // It issues the vanilla Hack job against the nearest reachable, still-locked
    // AncientBlastDoor; once the door unlocks the pawn re-runs this tree and the
    // forage/resupply nodes can finally path to food.
    //
    // Containment: a defender must only ever break INWARD (deeper into the structure),
    // never out toward the perimeter. Two filters enforce that on the candidate doors:
    //   * The door must touch the structure footprint - rejects any stray blast door
    //     spawned away from the structure.
    //   * The door must NOT be a perimeter / airlock door. BTG seals each entrance with an
    //     airlock (CorridorAirlockDefenceSpawner / AirlockDefences.xml): a VacBarrier on the
    //     old corridor-edge door line, plus a fresh AncientBlastDoor two cells inboard of it.
    //     That inboard blast door lands INSIDE the corridor footprint, so the footprint test
    //     alone cannot tell it from an interior subroom door - and a starving defender
    //     roaming the corridor (e.g. with resupply on cooldown) would otherwise hack it and
    //     head straight for the vacuum seal. We reject it by its tell: a VacBarrier within a
    //     couple of cells, a marker that only ever appears at the perimeter (airlocks and the
    //     shuttle bay). Interior subroom doors have none, so they stay hackable and the
    //     trapped-pawn escape still works.
    // All capability and state gating (not already hacked, not locked out, pawn can
    // manipulate and meets the intellectual prerequisite, door is reachable) is delegated to
    // the vanilla CompHackable.CanHackNow(pawn), so we accept exactly the hacks the game
    // itself would and an incapable pawn simply gets no job (and falls through to
    // rest/wander). Mechs never reach this node (no food need).
    public class JobGiver_BTGHackDoorForFood : ThinkNode_JobGiver
    {
        public HungerCategory minCategory = HungerCategory.Starving;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_BTGHackDoorForFood)base.DeepCopy(resolve);
            copy.minCategory = minCategory;
            return copy;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Need_Food need = pawn.needs?.food;
            if (need == null || (int)need.CurCategory < (int)minCategory)
                return null;

            ThingDef doorDef = Things.AncientBlastDoor;
            if (doorDef == null)
                return null;

            Map map = pawn.Map;
            List<Thing> doors = map.listerThings.ThingsOfDef(doorDef);

            Building_HackableDoor best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < doors.Count; i++)
            {
                if (!(doors[i] is Building_HackableDoor door))
                    continue;
                if (!TouchesStructure(map, door.Position))
                    continue;
                // Never break OUT through the perimeter: skip airlock doors, flagged by the
                // adjacent vacuum seal. Only escape deeper into the structure is allowed.
                if (LeadsToPerimeter(map, door.Position))
                    continue;
                // CanHackNow(pawn) bundles every gate vanilla's own WorkGiver_Hack uses:
                // not already hacked, not in post-hack lockout, pawn capable of hacking
                // (manipulation + intellectual not disabled), door reachable, and the
                // intellectual-skill prerequisite. Mirror it, then add reservation like the
                // WorkGiver does so two trapped pawns don't both claim the same door.
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

        // Lenient containment check: the door cell itself, or one of its cardinal neighbours,
        // lies inside the structure footprint. Doors sit on the wall shared between two rooms,
        // and that wall cell can fall just outside a room rect, so checking neighbours avoids
        // wrongly excluding a legitimate interior door (which would defeat the whole point)
        // while still rejecting any stray blast door spawned away from the structure.
        private static bool TouchesStructure(Map map, IntVec3 cell)
        {
            if (StructureBoundsCache.Contains(map, cell))
                return true;
            for (int i = 0; i < 4; i++)
            {
                if (StructureBoundsCache.Contains(map, cell + GenAdj.CardinalDirections[i]))
                    return true;
            }
            return false;
        }

        // The airlock prefab anchors a VacBarrier on the old corridor-edge door line and the
        // replacement AncientBlastDoor two cells inboard, so a VacBarrier sits a fixed two
        // cells from every perimeter/airlock door (rotation-invariant). VacBarriers exist only
        // at the perimeter - airlock seals and the shuttle bay - never beside an interior
        // subroom door, so one within this radius cleanly marks a door that breaks OUT toward
        // vacuum. The footprint test can't catch these because the door is embedded in the
        // corridor footprint; this is the discriminator that can.
        private const int PerimeterVacBarrierRadius = 2;

        private static bool LeadsToPerimeter(Map map, IntVec3 doorCell)
        {
            ThingDef vacBarrier = Things.VacBarrier;
            if (vacBarrier == null)
                return false;

            foreach (IntVec3 cell in CellRect.CenteredOn(doorCell, PerimeterVacBarrierRadius))
            {
                if (!cell.InBounds(map))
                    continue;
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].def == vacBarrier)
                        return true;
                }
            }
            return false;
        }
    }
}
