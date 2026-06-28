using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Paramedic-mech rescue: fetches casualties from the rest of the settlement into
    // the medbay. If a same-faction (Traders Guild) defender is downed and not already
    // in a bed, lying OUTSIDE the medbay but still within the structure footprint
    // (StructureBoundsCache), and an unreserved medical bed with a free slot exists in
    // the medbay (MedicRoomBounds), the medic carries them to it (worst-bleed casualty
    // first). When no in-medbay bed is free this returns null - the casualty waits.
    //
    // Sits below the tend node, so the medic clears every wounded defender already in
    // the medbay before walking out to retrieve another. The medic's duty focus stays
    // pinned to the medbay centre while it is out, so MedicRoomBounds still resolves
    // its room (and the destination bed) correctly.
    public class JobGiver_BTGMechMedicRescue : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            List<CellRect> rects = MedicRoomBounds.GetRects(pawn);
            if (rects == null)
                return null;

            Pawn patient = FindDownedNeedingBed(pawn, rects);
            if (patient == null)
                return null;

            Building_Bed bed = FindRoomMedicalBed(pawn, rects);
            if (bed == null)
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.Rescue, patient, bed);
            job.count = 1;
            return job;
        }

        private static Pawn FindDownedNeedingBed(Pawn medic, List<CellRect> medbayRects)
        {
            Pawn best = null;
            float bestBleed = -1f;

            Map map = medic.Map;
            List<Pawn> defenders = map.mapPawns.SpawnedPawnsInFaction(medic.Faction);
            for (int i = 0; i < defenders.Count; i++)
            {
                Pawn p = defenders[i];
                if (p == medic || p.Dead || !p.RaceProps.Humanlike)
                    continue;
                if (!p.Downed || p.InBed())
                    continue;
                // Outside the medbay but still inside the settlement structure: the tend
                // node already handles anyone downed within the medbay, so this node only
                // fetches casualties lying elsewhere in the base.
                if (MedicRoomBounds.Contains(medbayRects, p.Position))
                    continue;
                if (!StructureBoundsCache.Contains(map, p.Position))
                    continue;
                if (!medic.CanReserveAndReach(p, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float bleed = p.health.hediffSet.BleedRateTotal;
                if (bleed > bestBleed)
                {
                    bestBleed = bleed;
                    best = p;
                }
            }
            return best;
        }

        private static Building_Bed FindRoomMedicalBed(Pawn medic, List<CellRect> rects)
        {
            List<Thing> beds = medic.Map.listerThings.ThingsInGroup(ThingRequestGroup.Bed);
            for (int i = 0; i < beds.Count; i++)
            {
                if (!(beds[i] is Building_Bed bed))
                    continue;
                if (!bed.Medical || !bed.AnyUnoccupiedSleepingSlot)
                    continue;
                if (!MedicRoomBounds.Contains(rects, bed.Position))
                    continue;
                if (bed.IsForbidden(medic))
                    continue;
                if (!medic.CanReserveAndReach(bed, PathEndMode.OnCell, Danger.Deadly))
                    continue;

                return bed;
            }
            return null;
        }
    }
}
