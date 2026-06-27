using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    /// <summary>
    /// Paramedic-mech tending, confined to the mech's MedicalBay (MedicRoomBounds).
    /// Picks the single most urgent wounded same-faction (Traders Guild) defender in
    /// the room - worst bleed rate first, then lowest overall health - and tends it,
    /// in a bed or on the floor. Uses the highest-potency medicine lying inside the
    /// room (any quality, ranked by StatDefOf.MedicalPotency exactly as vanilla
    /// FindBestMedicine does), or tends medicine-free if the room holds none. Mechs
    /// are skipped: they have no tendable wounds.
    ///
    /// Listed twice in the BTG_MechMedic duty: once with a high minBleedRate (above
    /// the rescue node, so a heavily bleeding casualty is stabilised in place BEFORE
    /// anyone is hauled to a bed) and once at minBleedRate 0 (below rescue, routine).
    /// endAfterTendedOnce re-runs the duty tree after each tend action, so the medic
    /// continually re-triages to whoever is currently worst - "one at a time".
    /// </summary>
    public class JobGiver_BTGMechMedicTend : ThinkNode_JobGiver
    {
        // Emergency gate: when > 0, only fires for patients bleeding at least this fast.
        public float minBleedRate = 0f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_BTGMechMedicTend)base.DeepCopy(resolve);
            copy.minBleedRate = minBleedRate;
            return copy;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            List<CellRect> rects = MedicRoomBounds.GetRects(pawn);
            if (rects == null)
                return null;

            Pawn patient = FindWorstPatient(pawn, rects);
            if (patient == null)
                return null;

            Thing medicine = FindBestRoomMedicine(pawn, rects);

            Job job;
            if (medicine != null)
            {
                job = JobMaker.MakeJob(JobDefOf.TendPatient, patient, medicine, patient.SpawnedParentOrMe);
                job.count = Medicine.GetMedicineCountToFullyHeal(patient);
            }
            else
            {
                job = JobMaker.MakeJob(JobDefOf.TendPatient, patient, patient.SpawnedParentOrMe);
            }

            // Re-evaluate after each individual tend so the medic always works the
            // current worst-off defender rather than finishing one before noticing
            // another has started bleeding harder.
            job.endAfterTendedOnce = true;
            return job;
        }

        private Pawn FindWorstPatient(Pawn medic, List<CellRect> rects)
        {
            Pawn best = null;
            float bestBleed = -1f;
            float bestSeverity = -1f;

            List<Pawn> defenders = medic.Map.mapPawns.SpawnedPawnsInFaction(medic.Faction);
            for (int i = 0; i < defenders.Count; i++)
            {
                Pawn p = defenders[i];
                if (p == medic || p.Dead || !p.RaceProps.Humanlike)
                    continue;
                if (!MedicRoomBounds.Contains(rects, p.Position))
                    continue;
                if (!p.health.HasHediffsNeedingTend())
                    continue;

                float bleed = p.health.hediffSet.BleedRateTotal;
                if (bleed < minBleedRate)
                    continue;
                if (!medic.CanReserveAndReach(p, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float severity = 1f - p.health.summaryHealth.SummaryHealthPercent;
                if (bleed > bestBleed || (bleed == bestBleed && severity > bestSeverity))
                {
                    bestBleed = bleed;
                    bestSeverity = severity;
                    best = p;
                }
            }
            return best;
        }

        private static Thing FindBestRoomMedicine(Pawn medic, List<CellRect> rects)
        {
            List<Thing> meds = medic.Map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine);
            Thing best = null;
            float bestPotency = -1f;
            for (int i = 0; i < meds.Count; i++)
            {
                Thing m = meds[i];
                if (!MedicRoomBounds.Contains(rects, m.Position))
                    continue;
                if (m.IsForbidden(medic))
                    continue;
                if (!medic.CanReserveAndReach(m, PathEndMode.ClosestTouch, Danger.Deadly))
                    continue;

                float potency = m.def.GetStatValueAbstract(StatDefOf.MedicalPotency);
                if (potency > bestPotency)
                {
                    bestPotency = potency;
                    best = m;
                }
            }
            return best;
        }
    }
}
