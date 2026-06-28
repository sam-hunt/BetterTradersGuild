using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.RoomContents.Nursery
{
    // Populates nursery shelves with baby food and survival meals.
    public static class NurseryShelfPopulator
    {
        // Nutrition-per-day fallback used when a pawn's food need can't be read
        // (roughly an adult human's intake). Keeps allocation sane in edge cases.
        private const float DefaultNutritionPerDay = 1.6f;

        // Populates the nursery shelf, splitting its limited slots between baby food
        // (for newborns/babies) and packaged survival meals (for everyone older) in the
        // ratio that keeps both groups fed for as long as possible.
        //
        // Each shelf cell holds up to maxItemsInCell stacks; we treat every such stack
        // as one "slot" and fill it with a full stack of a single food type. The slots
        // are divided so the two food groups would run out at roughly the same time,
        // rather than using fixed stack counts that ignore who actually lives here.
        public static void PopulateNurseryShelf(Map map, CellRect subroomRect, List<Pawn> occupants)
        {
            // Find shelves in the subroom
            List<Building_Storage> shelves = RoomShelfHelper.GetShelvesInRoom(map, subroomRect, Things.Shelf, null);

            if (shelves.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No shelves found in nursery subroom for food storage.");
                return;
            }

            // Total fillable stacks across all shelves (cells * stacks-per-cell).
            int totalSlots = shelves.Sum(s => s.AllSlotCellsList().Count * StacksPerCell(s));
            if (totalSlots <= 0)
                return;

            // Sum the occupants' daily nutrition demand, split by what they can eat.
            // Newborns/babies need baby food; everyone older eats survival meals.
            float babyDemand = 0f;
            float mealDemand = 0f;
            if (occupants != null)
            {
                foreach (Pawn pawn in occupants)
                {
                    if (pawn == null || pawn.Destroyed)
                        continue;

                    if (EatsBabyFood(pawn))
                        babyDemand += NutritionPerDay(pawn);
                    else
                        mealDemand += NutritionPerDay(pawn);
                }
            }

            // Nutrition stored by one full stack of each food type.
            float babyFoodPerSlot = StackNutrition(Things.BabyFood);
            float mealPerSlot = StackNutrition(Things.MealSurvivalPack);

            // Baby food requires Biotech; if it's unavailable, route everyone to meals.
            if (Things.BabyFood == null || babyFoodPerSlot <= 0f)
                babyDemand = 0f;

            int babySlots = AllocateBabyFoodSlots(totalSlots, babyDemand, mealDemand, babyFoodPerSlot, mealPerSlot);
            int mealSlots = totalSlots - babySlots;

            FillSlots(map, shelves, Things.BabyFood, babySlots);
            FillSlots(map, shelves, Things.MealSurvivalPack, mealSlots);
        }

        // Chooses how many of the shelf slots to devote to baby food so baby food and
        // survival meals are exhausted at roughly the same time, maximizing the moment
        // the first occupant would go hungry. Returns a slot count in [0, totalSlots].
        private static int AllocateBabyFoodSlots(
            int totalSlots, float babyDemand, float mealDemand, float babyFoodPerSlot, float mealPerSlot)
        {
            bool needBaby = babyDemand > 0f && babyFoodPerSlot > 0f;
            bool needMeals = mealDemand > 0f && mealPerSlot > 0f;

            // Only one group present (or nobody): give that group every slot.
            if (needBaby && !needMeals) return totalSlots;
            if (!needBaby) return 0;

            // Both groups present: each gets at least one slot so neither starves at t=0.
            // Search the integer splits and keep the one that maximizes the shorter of the
            // two "days fed" figures, breaking ties toward the most balanced split.
            int bestBabySlots = 1;
            float bestDaysFed = float.NegativeInfinity;
            float bestImbalance = float.PositiveInfinity;
            for (int b = 1; b <= totalSlots - 1; b++)
            {
                int m = totalSlots - b;
                float daysBaby = b * babyFoodPerSlot / babyDemand;
                float daysMeals = m * mealPerSlot / mealDemand;
                float daysFed = Mathf.Min(daysBaby, daysMeals);
                float imbalance = Mathf.Abs(daysBaby - daysMeals);

                if (daysFed > bestDaysFed || (daysFed == bestDaysFed && imbalance < bestImbalance))
                {
                    bestDaysFed = daysFed;
                    bestImbalance = imbalance;
                    bestBabySlots = b;
                }
            }
            return bestBabySlots;
        }

        // True if the pawn drinks baby food rather than eating meals (newborn or baby).
        private static bool EatsBabyFood(Pawn pawn)
        {
            DevelopmentalStage stage = pawn.DevelopmentalStage;
            return stage == DevelopmentalStage.Newborn || stage == DevelopmentalStage.Baby;
        }

        // The pawn's sustained nutrition consumption per day, measured at the "fed"
        // hunger rate so a pawn's random spawn-time hunger doesn't skew the estimate.
        private static float NutritionPerDay(Pawn pawn)
        {
            try
            {
                Need_Food food = pawn?.needs?.food;
                if (food != null)
                {
                    float perTick = food.FoodFallPerTickAssumingCategory(HungerCategory.Fed, true);
                    if (perTick > 0f)
                        return perTick * GenDate.TicksPerDay;
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Better Traders Guild] Failed to read nutrition rate for {pawn?.LabelShortCap}: {ex.Message}");
            }
            return DefaultNutritionPerDay;
        }

        // Total nutrition held by one full stack of the given food def.
        private static float StackNutrition(ThingDef def)
        {
            if (def == null)
                return 0f;

            float perUnit = def.GetStatValueAbstract(StatDefOf.Nutrition);
            if (perUnit <= 0f)
                perUnit = def.ingestible?.CachedNutrition ?? 0f;

            return perUnit * def.stackLimit;
        }

        // Stacks a single shelf cell can hold (maxItemsInCell, e.g. 3 for vanilla shelves).
        private static int StacksPerCell(Building_Storage shelf)
        {
            return Mathf.Max(1, shelf.def.building?.maxItemsInCell ?? 1);
        }

        // Spawns up to slotCount full stacks of the given food across the shelves,
        // stopping early if the shelves run out of room.
        private static void FillSlots(Map map, List<Building_Storage> shelves, ThingDef def, int slotCount)
        {
            if (def == null || slotCount <= 0)
                return;

            int placed = 0;
            foreach (Building_Storage shelf in shelves)
            {
                while (placed < slotCount)
                {
                    Thing spawned = RoomShelfHelper.AddItemsToShelf(map, shelf, def, def.stackLimit, setForbidden: true);
                    if (spawned == null)
                        break; // shelf is full; try the next one

                    placed++;
                }

                if (placed >= slotCount)
                    return;
            }
        }
    }
}
