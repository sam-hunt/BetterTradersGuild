using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    /// <summary>
    /// Hunger response for bounded defenders. Eats from the pawn's own inventory
    /// first (in place), then searches for the best food source on the map that
    /// lies inside the structure footprint (StructureBoundsCache rect union).
    ///
    /// Replaces vanilla JobGiver_GetFood for defenders. Vanilla's
    /// TryFindBestFoodSourceFor fuses inventory eating with an UNBOUNDED map search
    /// in a single call and exposes no location filter, so it would send a hungry
    /// guard outside the structure to grab food the player dropped just past the
    /// walls (an easy bait). That search can't live anywhere in the think tree —
    /// above us it grabs the outside bait, below us it grabs it whenever the
    /// interior is empty. So we own the loop here but reuse vanilla's
    /// scoring/edibility/job helpers (BestFoodInInventory, WillEat, FoodOptimality,
    /// GetNutrition, WillIngestStackCountOf) rather than reimplementing food
    /// preference logic.
    ///
    /// First pass intentionally omits dispensers/hoppers, plant harvesting, and any
    /// hunger escalation (resupply drop vs. all-in assault is a deliberately open
    /// design choice). See TODOs.
    /// </summary>
    public class JobGiver_BTGForageInStructure : ThinkNode_JobGiver
    {
        public HungerCategory minCategory = HungerCategory.Hungry;
        public float maxLevelPercentage = 1f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_BTGForageInStructure)base.DeepCopy(resolve);
            copy.minCategory = minCategory;
            copy.maxLevelPercentage = maxLevelPercentage;
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

            // 2. Otherwise the best reachable food that sits inside the structure.
            Thing structureFood = BestFoodInStructure(pawn);
            if (structureFood != null)
                return IngestJob(pawn, structureFood);

            return null;
        }

        private static Job IngestJob(Pawn pawn, Thing food)
        {
            ThingDef foodDef = FoodUtility.GetFinalIngestibleDef(food);
            float nutrition = FoodUtility.GetNutrition(pawn, food, foodDef);
            Job job = JobMaker.MakeJob(JobDefOf.Ingest, food);
            job.count = FoodUtility.WillIngestStackCountOf(pawn, foodDef, nutrition);
            return job;
        }

        private Thing BestFoodInStructure(Pawn pawn)
        {
            Map map = pawn.Map;
            // No layout bounds known: don't run an unbounded map search. Carried
            // food was already tried above, so just stay put.
            if (StructureBoundsCache.GetRoomRects(map) == null)
                return null;

            Danger maxDanger = pawn.needs.food.CurCategory == HungerCategory.Starving ? Danger.Deadly : Danger.Some;
            List<Thing> candidates = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);

            Thing best = null;
            float bestOptimality = float.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing food = candidates[i];

                // Skip building food sources (nutrient paste dispensers/hoppers);
                // deferred to the hunger-escalation pass.
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
    }
}
