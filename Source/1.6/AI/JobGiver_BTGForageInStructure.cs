using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    /// <summary>
    /// Hunger response for bounded defenders. Everything it targets lies inside the
    /// structure footprint (StructureBoundsCache rect union), so a player can never
    /// bait a hungry guard out by dropping food past the walls. It replaces vanilla
    /// JobGiver_GetFood, whose TryFindBestFoodSourceFor fuses inventory eating with an
    /// UNBOUNDED map search and exposes no location filter, but it reuses vanilla's
    /// scoring/edibility/job helpers (BestFoodInInventory, WillEat, FoodOptimality,
    /// GetFinalIngestibleDef, GetNutrition, WillIngestStackCountOf) rather than
    /// reimplementing food-preference logic.
    ///
    /// Resolution order (each step only reached when the previous found nothing):
    ///   1. Carried rations            - eaten in place, no pathing, no exploit surface.
    ///   2. Spawned food items         - the best reachable meal/ingredient on the floor.
    /// Then, only once UrgentlyHungry and able to manipulate (real food is preferred
    /// via the ordering above), two "last resort" in-structure sources:
    ///   3. Survival-meal pallets      - opened via BTG_OpenContainer; ejected meals are
    ///                                   then eaten by step 2 on a later think tick.
    ///   4. Nutrient-paste dispensers  - vanilla dispensers AND VNPE taps (the tap
    ///                                   subclasses Building_NutrientPasteDispenser, so
    ///                                   detecting the base type needs no hard VNPE dep;
    ///                                   VNPE's Harmony prefix makes the vanilla Ingest
    ///                                   job draw from the paste pipe net).
    ///
    /// Steps 3-4 both require manipulation (opening crates and operating dispensers both
    /// do in vanilla) and are gated to UrgentlyHungry so a merely-Hungry defender holds
    /// its post rather than tearing open pallets. Mechs never reach any of this (no food
    /// need). Raw crops from the hydroponics are deliberately NOT foraged - wealthy guild
    /// traders escalate hunger by radioing in a packaged-meal resupply drop (the comms-
    /// console path), not by eating unprepared produce; see the research doc.
    /// </summary>
    public class JobGiver_BTGForageInStructure : ThinkNode_JobGiver
    {
        public HungerCategory minCategory = HungerCategory.Hungry;
        public float maxLevelPercentage = 1f;

        // Building fallbacks (pallets, dispensers) only kick in at this hunger level, so
        // real food (inventory + floor items) is always preferred.
        public HungerCategory fallbackMinCategory = HungerCategory.UrgentlyHungry;

        // Openable in-structure containers a defender will crack for food, by defName.
        // Resolved null-safe, so an absent def (or DLC) is simply skipped.
        public List<string> mealContainerDefNames = new List<string> { "Pallet_SurvivalMeals" };

        private List<ThingDef> resolvedContainerDefs;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_BTGForageInStructure)base.DeepCopy(resolve);
            copy.minCategory = minCategory;
            copy.maxLevelPercentage = maxLevelPercentage;
            copy.fallbackMinCategory = fallbackMinCategory;
            copy.mealContainerDefNames = mealContainerDefNames;
            return copy;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Need_Food need = pawn.needs?.food;
            if (need == null || (int)need.CurCategory < (int)minCategory || need.CurLevelPercentage > maxLevelPercentage)
                return null;

            // 1. Eat carried rations first: in place, no pathing, no exploit surface.
            Thing inventoryFood = FoodUtility.BestFoodInInventory(pawn, pawn);
            if (inventoryFood != null)
                return IngestJob(pawn, inventoryFood);

            // No layout bounds known: don't run an unbounded map search. Carried food
            // was already tried above, so just stay put.
            if (StructureBoundsCache.GetRoomRects(pawn.Map) == null)
                return null;

            // 2. Otherwise the best reachable food item that sits inside the structure.
            Thing structureFood = BestFoodInStructure(pawn);
            if (structureFood != null)
                return IngestJob(pawn, structureFood);

            // 3-4. Last-resort in-structure building food. Only once urgently hungry,
            // and only for pawns that can manipulate (opening / dispensing both need it).
            // Mechs never get here (no food need).
            if ((int)need.CurCategory < (int)fallbackMinCategory
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            // 3. Crack open a survival-meal pallet; its meals are eaten on a later tick.
            Job openJob = TryOpenMealContainer(pawn);
            if (openJob != null)
                return openJob;

            // 4. Dispense a meal from a nutrient-paste dispenser / VNPE tap.
            return TryDispenseFromTap(pawn);
        }

        private static Job IngestJob(Pawn pawn, Thing food)
        {
            ThingDef foodDef = FoodUtility.GetFinalIngestibleDef(food);
            float nutrition = FoodUtility.GetNutrition(pawn, food, foodDef);
            Job job = JobMaker.MakeJob(JobDefOf.Ingest, food);
            job.count = FoodUtility.WillIngestStackCountOf(pawn, foodDef, nutrition);
            return job;
        }

        private static Danger MaxDanger(Pawn pawn)
        {
            return pawn.needs.food.CurCategory == HungerCategory.Starving ? Danger.Deadly : Danger.Some;
        }

        private Thing BestFoodInStructure(Pawn pawn)
        {
            Map map = pawn.Map;
            Danger maxDanger = MaxDanger(pawn);
            List<Thing> candidates = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);

            Thing best = null;
            float bestOptimality = float.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing food = candidates[i];

                // Building food sources (nutrient-paste dispensers/hoppers) are handled
                // separately as a lower-priority fallback (TryDispenseFromTap).
                if (food is Building)
                    continue;
                if (!StructureBoundsCache.Contains(map, food.Position))
                    continue;
                if (food.IsForbidden(pawn) || !pawn.WillEat(food, pawn))
                    continue;
                if (!pawn.CanReserveAndReach(food, PathEndMode.ClosestTouch, maxDanger))
                    continue;

                ThingDef foodDef = FoodUtility.GetFinalIngestibleDef(food);
                float dist = (pawn.Position - food.Position).LengthHorizontal;
                float optimality = FoodUtility.FoodOptimality(pawn, food, foodDef, dist);
                if (optimality > bestOptimality)
                {
                    bestOptimality = optimality;
                    best = food;
                }
            }
            return best;
        }

        // 3. Nearest reachable, openable, non-empty meal pallet inside the structure.
        private Job TryOpenMealContainer(Pawn pawn)
        {
            Map map = pawn.Map;
            Danger maxDanger = MaxDanger(pawn);

            Thing best = null;
            float bestDistSq = float.MaxValue;
            foreach (ThingDef containerDef in MealContainerDefs())
            {
                List<Thing> things = map.listerThings.ThingsOfDef(containerDef);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    // CanOpen implies the casket still has contents and isn't locked.
                    if (!(thing is IOpenable openable) || !openable.CanOpen)
                        continue;
                    if (!StructureBoundsCache.Contains(map, thing.Position))
                        continue;
                    if (thing.IsForbidden(pawn))
                        continue;
                    if (!pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, maxDanger))
                        continue;

                    float distSq = (pawn.Position - thing.Position).LengthHorizontalSquared;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = thing;
                    }
                }
            }

            return best != null ? JobMaker.MakeJob(Jobs.BTG_OpenContainer, best) : null;
        }

        // 4. Nearest dispensable nutrient-paste dispenser / VNPE tap inside the
        // structure. Detected via the vanilla base type, so VNPE taps (a subclass)
        // are covered with no hard dependency.
        private Job TryDispenseFromTap(Pawn pawn)
        {
            Map map = pawn.Map;
            Danger maxDanger = MaxDanger(pawn);
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
                if (meal == null || !pawn.WillEat(meal, pawn))
                    continue;
                IntVec3 cell = dispenser.InteractionCell;
                if (!cell.Standable(map) || !StructureBoundsCache.Contains(map, cell))
                    continue;
                if (!pawn.CanReach(dispenser, PathEndMode.InteractionCell, maxDanger))
                    continue;

                float distSq = (pawn.Position - cell).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = dispenser;
                }
            }

            return best != null ? IngestJob(pawn, best) : null;
        }

        private List<ThingDef> MealContainerDefs()
        {
            if (resolvedContainerDefs == null)
            {
                resolvedContainerDefs = new List<ThingDef>();
                if (mealContainerDefNames != null)
                {
                    for (int i = 0; i < mealContainerDefNames.Count; i++)
                    {
                        ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(mealContainerDefNames[i]);
                        if (def != null)
                            resolvedContainerDefs.Add(def);
                    }
                }
            }
            return resolvedContainerDefs;
        }
    }
}
