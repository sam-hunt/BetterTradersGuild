using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Paramedic-mech tending, confined to the mech's MedicalBay (MedicRoomBounds).
    // Picks the single most urgent wounded same-faction (Traders Guild) defender in
    // the room - worst bleed rate first, then lowest overall health - and tends it,
    // in a bed or on the floor. Uses the leftover medicine in its own inventory
    // first (stashed there by JobDriver_BTGTendPatient at the end of each tend),
    // then the highest-potency medicine lying inside the room (any quality, ranked
    // by StatDefOf.MedicalPotency exactly as vanilla FindBestMedicine does), or
    // tends medicine-free if neither holds any. Mechs are skipped: they have no
    // tendable wounds.
    //
    // Top priority in the BTG_MechMedic duty: the medic patches every wounded defender
    // in the medbay (worst first) before the rescue node sends it out to fetch more.
    // endAfterTendedOnce re-runs the duty tree after each tend action, so the medic
    // continually re-triages to whoever is currently worst - "one at a time".
    public class JobGiver_BTGMechMedicTend : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            List<CellRect> rects = MedicRoomBounds.GetRects(pawn);
            if (rects == null)
                return null;

            Pawn patient = FindWorstPatient(pawn, rects);
            if (patient == null)
                return null;

            // Leftover medicine from the previous tend first (stashed in the mech's
            // inventory by JobDriver_BTGTendPatient's finish action): it was the best
            // stack in the room when picked up, and reusing it saves a shelf trip per
            // tend. The driver's CollectMedicineToils jumps straight to the patient
            // when the medicine is already in the doctor's inventory. Only then the
            // room search.
            Thing medicine = FindInventoryMedicine(pawn) ?? FindBestRoomMedicine(pawn, rects);

            // Mirror vanilla WorkGiver_Tend.JobOnThing exactly: TargetB is the
            // medicine; TargetC is the MEDICINE's holder and only present when the
            // medicine is nested inside a container or another pawn's inventory
            // (JobDriver_TendPatient treats a TargetC pawn as the thing to fetch the
            // medicine from). Shelf medicine is spawned loose and the medic's own
            // inventory medicine short-circuits before TargetC is ever read, so those
            // take the two-target form. No medicine tends bare-handed with the
            // single-target form - anything in TargetB is assumed to be medicine.
            Job job;
            if (medicine != null && medicine.SpawnedParentOrMe != medicine && medicine.SpawnedParentOrMe != pawn)
                job = JobMaker.MakeJob(DefRefs.Jobs.BTG_TendPatient, patient, medicine, medicine.SpawnedParentOrMe);
            else if (medicine != null)
                job = JobMaker.MakeJob(DefRefs.Jobs.BTG_TendPatient, patient, medicine);
            else
                job = JobMaker.MakeJob(DefRefs.Jobs.BTG_TendPatient, patient);

            // Re-evaluate after each individual tend so the medic always works the
            // current worst-off defender rather than finishing one before noticing
            // another has started bleeding harder.
            //
            // endAfterTendedOnce is also load-bearing for medicine: it ends the job in
            // FinalizeTend, before the driver's FindMoreMedicineToil - whose
            // HealthAIUtility.FindBestMedicine treats a playerSettings-less NPC patient
            // as MedicalCareCategory.NoMeds and so can never re-source medicine. This
            // giver supplying the medicine itself (TargetB) is what keeps NPC tends
            // medicated; don't lean on the driver's refresh path.
            job.endAfterTendedOnce = true;
            return job;
        }

        private Pawn FindWorstPatient(Pawn medic, List<CellRect> rects)
        {
            Pawn best = null;
            float bestBleed = -1f;
            float bestSeverity = -1f;

            List<Pawn> factionPawns = medic.Map.mapPawns.SpawnedPawnsInFaction(medic.Faction);
            for (int i = 0; i < factionPawns.Count; i++)
            {
                Pawn p = factionPawns[i];
                if (p == medic || p.Dead || !p.RaceProps.Humanlike)
                    continue;
                if (!MedicRoomBounds.Contains(rects, p.Position))
                    continue;
                if (!p.health.HasHediffsNeedingTend())
                    continue;

                float bleed = p.health.hediffSet.BleedRateTotal;
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

        // Highest-potency medicine already in the medic's inventory - normally the
        // leftover the tend driver stashed there. No reserve/reach checks: the mech
        // is holding it.
        private static Thing FindInventoryMedicine(Pawn medic)
        {
            ThingOwner<Thing> inv = medic.inventory?.innerContainer;
            if (inv == null)
                return null;

            Thing best = null;
            float bestPotency = -1f;
            for (int i = 0; i < inv.Count; i++)
            {
                Thing m = inv[i];
                if (!m.def.IsMedicine)
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
