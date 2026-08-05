using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Paramedic-mech rescue: gets casualties into medbay beds. If a same-faction
    // (Traders Guild) defender is downed and not already in a bed, lying anywhere
    // within the structure footprint (StructureBoundsCache) - elsewhere in the base,
    // or on the medbay floor itself (downed in place, or dropped by an interrupted
    // carry) - and an unreserved medical bed with a free slot exists in the medbay
    // (MedicRoomBounds), the medic carries them to it (worst-bleed casualty first).
    // When no in-medbay bed is free this returns null - the casualty waits where
    // they lie (the tend node above still treats anyone inside the medbay on the
    // floor) and is promoted into a bed once one frees up.
    //
    // Sits below the tend node, so the medic clears every wounded defender already in
    // the medbay before carrying anyone. The medic's duty focus stays pinned to the
    // medbay centre while it is out, so MedicRoomBounds still resolves its room (and
    // the destination bed) correctly.
    public class JobGiver_BTGMechMedicRescue : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            List<CellRect> rects = MedicRoomBounds.GetRects(pawn);
            if (rects == null)
                return null;

            Pawn patient = FindDownedNeedingBed(pawn);
            if (patient == null)
                return null;

            Building_Bed bed = FindRoomMedicalBed(pawn, rects);
            if (bed == null)
                return null;

            // BTG_Rescue, not vanilla Rescue: vanilla's TakeToBed driver hardcodes
            // Faction.OfPlayer when making the rescued pawn a guest, which red-errors
            // (hostile case) or corrupts HostFaction (friendly case) for an NPC rescuer.
            Job job = JobMaker.MakeJob(Jobs.BTG_Rescue, patient, bed);
            job.count = 1;
            return job;
        }

        private static Pawn FindDownedNeedingBed(Pawn medic)
        {
            Pawn best = null;
            float bestBleed = -1f;

            Map map = medic.Map;
            List<Pawn> factionPawns = map.mapPawns.SpawnedPawnsInFaction(medic.Faction);
            for (int i = 0; i < factionPawns.Count; i++)
            {
                Pawn p = factionPawns[i];
                if (p == medic || p.Dead || !p.RaceProps.Humanlike)
                    continue;
                // Babies and newborns can't be tucked into the medbay's adult medical beds; a
                // Rescue job targeting one fails on arrival and is re-issued every tick (job
                // spam). Leave infants to the caretaker AI (tuck-in-crib / carry-to-launchable).
                if (p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Newborn())
                    continue;
                if (!p.Downed || p.InBed())
                    continue;
                // Anywhere inside the settlement structure, including the medbay floor:
                // a casualty tended where they fell still belongs in a bed, so floor
                // patients are re-rescued (promoted) once a bed frees up.
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
