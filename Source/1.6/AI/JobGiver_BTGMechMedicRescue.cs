using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Paramedic-mech rescue, confined to the mech's MedicalBay (MedicRoomBounds).
    // If a downed same-faction (Traders Guild) defender is lying in the room and not
    // already in a bed, and an unreserved medical bed with a free slot exists IN THE
    // SAME ROOM, the medic carries them to it (worst-bleed casualty first). When no
    // in-room bed is free this returns null and the lower-priority tend node patches
    // the casualty on the floor instead.
    //
    // Sits below the emergency-tend node (so a heavily bleeding casualty is stabilised
    // before being moved) and above the routine-tend node.
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

        private static Pawn FindDownedNeedingBed(Pawn medic, List<CellRect> rects)
        {
            Pawn best = null;
            float bestBleed = -1f;

            List<Pawn> defenders = medic.Map.mapPawns.SpawnedPawnsInFaction(medic.Faction);
            for (int i = 0; i < defenders.Count; i++)
            {
                Pawn p = defenders[i];
                if (p == medic || p.Dead || !p.RaceProps.Humanlike)
                    continue;
                if (!p.Downed || p.InBed())
                    continue;
                if (!MedicRoomBounds.Contains(rects, p.Position))
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
