using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Paramedic-mech patient feeding, confined to the mech's MedicalBay
    // (MedicRoomBounds). Downed pawns cannot start jobs, so unlike the walking
    // wounded - who get up and hit their duty's forage node when hungry - a downed
    // defender parked in the medbay would starve without this. Player-controlled
    // paramedics feed patients through the Doctor work type (DoctorFeedHumanlikes is
    // canBeDoneByMechs), so this also restores player/NPC parity.
    //
    // Picks the hungriest downed same-faction humanlike inside the medbay rects (bed
    // or floor, matching the tend node's scope; babies are the caretakers' job,
    // mirroring vanilla WorkGiver_FeedPatient's baby exclusion) and feeds it via
    // BTG_FeedPatient. Vanilla FeedPatient's driver is NOT safe here: its global
    // FailOn(!FoodUtility.ShouldBeFedBySomeone) requires a player-faction/hosted (or
    // colony-prisoner) patient in a bed, so for a TG defender it ends the job on tick
    // one and this giver re-issues it forever (job loop). JobDriver_BTGFeedPatient is
    // that driver with the fail condition made faction-neutral; it keeps vanilla's
    // native fetch of food from another pawn's inventory (CheckItemCarriedByOtherPawn
    // fills TargetC and detours to the holder), which makes source 1 below a single
    // walk: the holder IS the patient.
    //
    // Food resolution mirrors JobGiver_BTGForageInStructure's escalation, scored for
    // the PATIENT but reached/reserved by the medic:
    //   1. The patient's own carried rations - defenders usually spawn with food in
    //      inventory (kindDef invNutrition), and a downed pawn can't eat it themself.
    //   2. The best spawned food item inside the structure footprint
    //      (StructureBoundsCache rect union - so never food a player drops outside
    //      the walls as bait).
    //   3. Once the patient is UrgentlyHungry: a nutrient-paste dispenser / VNPE tap
    //      inside the structure. Real food is preferred via the ordering; pallet
    //      cracking and valve opening remain the hungry DEFENDERS' escalation
    //      (JobGiver_BTGForageInStructure), which also readies the taps this step
    //      draws from.
    public class JobGiver_BTGMechMedicFeed : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            List<CellRect> rects = MedicRoomBounds.GetRects(pawn);
            if (rects == null)
                return null;

            Pawn patient = FindHungriestDownedPatient(pawn, rects);
            if (patient == null)
                return null;

            Thing food = FindFoodFor(pawn, patient);
            if (food == null)
                return null;

            // Mirror vanilla WorkGiver_FeedPatient.JobOnThing: nutrition/count are the
            // PATIENT's (GetFinalIngestibleDef and GetNutrition both handle a dispenser
            // as the food source).
            ThingDef foodDef = FoodUtility.GetFinalIngestibleDef(food);
            float nutrition = FoodUtility.GetNutrition(patient, food, foodDef);
            Job job = JobMaker.MakeJob(DefRefs.Jobs.BTG_FeedPatient);
            job.targetA = food;
            job.targetB = patient;
            job.count = FoodUtility.WillIngestStackCountOf(patient, foodDef, nutrition);
            return job;
        }

        private static Pawn FindHungriestDownedPatient(Pawn medic, List<CellRect> rects)
        {
            Pawn best = null;
            float bestLevel = float.MaxValue;

            List<Pawn> factionPawns = medic.Map.mapPawns.SpawnedPawnsInFaction(medic.Faction);
            for (int i = 0; i < factionPawns.Count; i++)
            {
                Pawn p = factionPawns[i];
                // Only the downed: walking wounded stand up and feed themselves through
                // their own duty's forage node, and racing them to the same meal would
                // just churn reservations.
                if (p == medic || p.Dead || !p.RaceProps.Humanlike || !p.Downed)
                    continue;
                if (p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Newborn())
                    continue;
                Need_Food need = p.needs?.food;
                if (need == null || (int)need.CurCategory < (int)HungerCategory.Hungry)
                    continue;
                if (!MedicRoomBounds.Contains(rects, p.Position))
                    continue;
                if (!medic.CanReserveAndReach(p, PathEndMode.Touch, Danger.Deadly))
                    continue;

                if (need.CurLevel < bestLevel)
                {
                    bestLevel = need.CurLevel;
                    best = p;
                }
            }
            return best;
        }

        private static Thing FindFoodFor(Pawn medic, Pawn patient)
        {
            // 1. The patient's own carried rations. CanReserve mirrors the driver's food
            // reservation (same guard the caretaker baby-feed giver needed); if another
            // pawn somehow holds it, fall through to the in-structure sources.
            Thing carried = FoodUtility.BestFoodInInventory(patient, patient);
            if (carried != null && medic.CanReserve(carried))
                return carried;

            // No layout bounds known: skip the map-wide searches rather than run them
            // unbounded (same fallback the forage giver uses).
            if (StructureBoundsCache.GetRoomRects(medic.Map) == null)
                return null;

            // 2. Best spawned food item inside the structure.
            Thing item = BestFoodItemInStructure(medic, patient);
            if (item != null)
                return item;

            // 3. Dispensers/taps only once the patient is urgently hungry, so real
            // food is always preferred (same threshold as the forage giver's
            // building fallbacks).
            if ((int)patient.needs.food.CurCategory < (int)HungerCategory.UrgentlyHungry)
                return null;
            return BestDispenserInStructure(medic, patient);
        }

        // Best spawned food item inside the structure, scored for the patient but
        // reached and reserved by the medic. Same exclusions as the forage giver:
        // buildings are the separate fallback below, and corpses are beneath the guild.
        private static Thing BestFoodItemInStructure(Pawn medic, Pawn patient)
        {
            Map map = medic.Map;
            List<Thing> candidates = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);

            Thing best = null;
            float bestOptimality = float.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing food = candidates[i];
                if (food is Building || food is Corpse)
                    continue;
                if (!StructureBoundsCache.Contains(map, food.Position))
                    continue;
                if (food.IsForbidden(medic) || !patient.WillEat(food, medic))
                    continue;
                if (!medic.CanReserveAndReach(food, PathEndMode.ClosestTouch, Danger.Deadly))
                    continue;

                ThingDef foodDef = FoodUtility.GetFinalIngestibleDef(food);
                float dist = (medic.Position - food.Position).LengthHorizontal;
                float optimality = FoodUtility.FoodOptimality(patient, food, foodDef, dist);
                if (optimality > bestOptimality)
                {
                    bestOptimality = optimality;
                    best = food;
                }
            }
            return best;
        }

        // Nearest dispensable nutrient-paste dispenser / VNPE tap inside the structure
        // (the tap subclasses Building_NutrientPasteDispenser, so no hard VNPE dep).
        private static Thing BestDispenserInStructure(Pawn medic, Pawn patient)
        {
            Map map = medic.Map;
            List<Thing> candidates = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);

            Building_NutrientPasteDispenser best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!(candidates[i] is Building_NutrientPasteDispenser dispenser))
                    continue;
                if (!StructureBoundsCache.Contains(map, dispenser.Position))
                    continue;
                // CanDispenseNow = powered AND has feedstock (VNPE tap: paste in the net).
                if (!dispenser.CanDispenseNow)
                    continue;
                ThingDef meal = dispenser.DispensableDef;
                if (meal == null || !patient.WillEat(meal, medic))
                    continue;
                IntVec3 cell = dispenser.InteractionCell;
                if (!cell.Standable(map) || !StructureBoundsCache.Contains(map, cell))
                    continue;
                if (!medic.CanReach(dispenser, PathEndMode.InteractionCell, Danger.Deadly))
                    continue;

                float distSq = (medic.Position - cell).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = dispenser;
                }
            }
            return best;
        }
    }
}
