using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Self-evacuation to the medbay for a bounded defender that is wounded but cannot
    // patch itself up because it has lost Manipulation (e.g. both arms shot off). The
    // vanilla self-tend node (JobGiver_SelfTend) needs Manipulation too, so such a pawn
    // is invisible to it and would otherwise just stand and bleed. Here it walks to a
    // free medical bed INSIDE a BTG medbay and lies down, where the medbay-confined
    // paramedic mech (JobGiver_BTGMechMedicTend) can reach and tend it.
    //
    // Why not vanilla JobGiver_PatientGoToBed: it resolves a bed via the unbounded,
    // whole-map RestUtility.FindBedFor. That is both unbounded (the straying-vector
    // problem JobGiver_BTGRestInStructure fixes for tiredness) and, worse here, willing
    // to pick ANY bed when the medbay's are full - a crew bunk the medbay-bound medic can
    // never reach. We instead resolve a medical bed within a medbay room directly
    // (StructureRoomLocator), and no-op when none is free - the same "wait for a bed"
    // stance the medic's own rescue node takes.
    //
    // Gates on Manipulation only - the exact complement of JobGiver_SelfTend's capability
    // check for these pawns. That node's "Doctor work disabled" guard is colonist-only
    // (skipped for non-player pawns), so for a faction defender Manipulation alone decides
    // whether it can self-tend. This node and self-tend therefore partition the wounded
    // with no overlap, and their relative order is immaterial. Sits below combat, so an
    // able-bodied defender keeps fighting and only the genuinely-can't-self-tend peel off.
    //
    // Downed pawns are deliberately excluded: those are the paramedic's rescue job
    // (JobGiver_BTGMechMedicRescue carries them in). This node is for the walking
    // wounded. Mechs fall out on the Humanlike gate.
    public class JobGiver_BTGSeekTreatmentInMedbay : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike || pawn.Downed || pawn.InAggroMentalState)
                return null;
            if (!pawn.health.HasHediffsNeedingTend())
                return null;
            // Only pawns that cannot self-tend. For these non-player pawns the AI self-tend
            // node gates purely on Manipulation (the colonist-only Doctor-disabled check is
            // skipped), so Manipulation is the real self-tend capability here.
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            List<CellRect> medbayRects = GetMedbayRects(pawn.Map);
            if (medbayRects == null)
                return null;

            // Already resting in a medbay bed: nothing to do - the medic tends in place.
            Building_Bed current = RestUtility.CurrentBed(pawn);
            if (current != null && current.Medical && MedicRoomBounds.Contains(medbayRects, current.Position))
                return null;

            Building_Bed bed = FindMedbayBed(pawn, medbayRects);
            if (bed == null)
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.LayDown, bed);
            job.checkOverrideOnExpire = true;
            return job;
        }

        // Union of every BTG medbay room's rects on the map, or null when the map has no
        // medbay (or no layout sketch). Mirrors how MedicRoomBounds resolves the medic's
        // room, but map-wide rather than anchored to one mech's duty focus.
        private static List<CellRect> GetMedbayRects(Map map)
        {
            var rects = new List<CellRect>();
            foreach (LayoutRoom room in StructureRoomLocator.RoomsOfDef(map, LayoutRooms.BTG_MedicalBay))
            {
                if (room.rects != null)
                    rects.AddRange(room.rects);
            }
            return rects.Count == 0 ? null : rects;
        }

        private static Building_Bed FindMedbayBed(Pawn pawn, List<CellRect> medbayRects)
        {
            Building_Bed best = null;
            float bestDistSq = float.MaxValue;
            List<Thing> beds = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Bed);
            for (int i = 0; i < beds.Count; i++)
            {
                if (!(beds[i] is Building_Bed bed))
                    continue;
                if (!bed.Medical || !bed.AnyUnoccupiedSleepingSlot)
                    continue;
                if (!MedicRoomBounds.Contains(medbayRects, bed.Position))
                    continue;
                if (bed.IsForbidden(pawn))
                    continue;
                if (!pawn.CanReserveAndReach(bed, PathEndMode.OnCell, Danger.Deadly))
                    continue;

                float distSq = pawn.Position.DistanceToSquared(bed.Position);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = bed;
                }
            }
            return best;
        }
    }
}
