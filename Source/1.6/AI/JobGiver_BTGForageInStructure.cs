using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Hunger response for bounded defenders. Everything it targets lies inside the
    // structure footprint (StructureBoundsCache rect union), so a player can never
    // bait a hungry guard out by dropping food past the walls. It replaces vanilla
    // JobGiver_GetFood, whose TryFindBestFoodSourceFor fuses inventory eating with an
    // UNBOUNDED map search and exposes no location filter, but it reuses vanilla's
    // scoring/edibility/job helpers (BestFoodInInventory, WillEat, FoodOptimality,
    // GetFinalIngestibleDef, GetNutrition, WillIngestStackCountOf) rather than
    // reimplementing food-preference logic.
    //
    // Resolution order (each step only reached when the previous found nothing):
    //   1. Carried rations            - eaten in place, no pathing, no exploit surface.
    //   2. Spawned food items         - the best reachable meal/ingredient on the floor.
    // Then, only once UrgentlyHungry and able to manipulate (real food is preferred
    // via the ordering above), three "last resort" in-structure sources:
    //   3. Nutrient-paste valves      - BTG mapgen (PipeValveHandler) locks the settlement
    //                                   down by closing every VNPE paste valve, isolating
    //                                   the vats from the net so a tap (step 5) has nothing
    //                                   to dispense - and so the player can't siphon the
    //                                   net from outside. A defender re-opens a closed
    //                                   valve (BTG_OpenPasteValve flicks it on); the vat
    //                                   refills the net and step 5 can dispense on a later
    //                                   think tick. Checked FIRST of the three so the
    //                                   renewable paste supply is brought online proactively
    //                                   - while the one-shot pallets still exist - rather
    //                                   than only after they run dry. That closes the
    //                                   handover gap where, the moment pallets empty, no
    //                                   tap works yet and a starving defender would fall
    //                                   through to the resupply call (a per-map, cooldowned
    //                                   resource). Opening a valve only READIES the supply;
    //                                   eating still prefers a pallet (step 4) over the
    //                                   paste this enables (step 5), so a proud guild guard
    //                                   flips the emergency valve but still eats its good
    //                                   rations before drinking sludge.
    //   4. Survival-meal pallets      - opened via BTG_OpenContainer; ejected meals are
    //                                   then eaten by step 2 on a later think tick.
    //   5. Nutrient-paste dispensers  - vanilla dispensers AND VNPE taps (the tap
    //                                   subclasses Building_NutrientPasteDispenser, so
    //                                   detecting the base type needs no hard VNPE dep;
    //                                   VNPE's Harmony prefix makes the vanilla Ingest
    //                                   job draw from the paste pipe net).
    //
    // Steps 3-5 all require manipulation (flicking valves, opening crates, and operating
    // dispensers all do in vanilla) and are gated to UrgentlyHungry so a merely-Hungry
    // defender holds its post rather than tearing open pallets. Mechs never reach any of
    // this (no food need). Raw crops from the hydroponics are deliberately NOT foraged -
    // wealthy guild traders escalate hunger by radioing in a packaged-meal resupply drop
    // (the comms-console path), not by eating unprepared produce; see the research doc.
    public class JobGiver_BTGForageInStructure : ThinkNode_JobGiver
    {
        public HungerCategory minCategory = HungerCategory.Hungry;
        public float maxLevelPercentage = 1f;

        // Building fallbacks (valves, pallets, dispensers) only kick in at this hunger
        // level, so real food (inventory + floor items) is always preferred.
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

            // 3-5. Last-resort in-structure building food. Only once urgently hungry,
            // and only for pawns that can manipulate (flicking / opening / dispensing all
            // need it). Mechs never get here (no food need).
            if ((int)need.CurCategory < (int)fallbackMinCategory
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            // 3. Proactively re-open a locked-down paste valve so a vat refills the net.
            // Done before eating (steps 4-5) so the renewable tap supply comes online
            // while pallets still exist, not only once they run dry - this is what lets
            // step 5 dispense on a later think tick.
            Job valveJob = TryOpenPasteValve(pawn);
            if (valveJob != null)
                return valveJob;

            // 4. Crack open a survival-meal pallet; its meals are eaten on a later tick.
            // Preferred over the paste tap (step 5): a real meal beats nutrient sludge.
            Job openJob = TryOpenMealContainer(pawn);
            if (openJob != null)
                return openJob;

            // 5. Dispense a meal from a nutrient-paste dispenser / VNPE tap.
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
                // Corpses carry nutrition and ride along in FoodSourceNotPlantOrTree, and
                // vanilla WillEat does not reject them - but eating the dead is beneath the
                // guild, and would also pre-empt the valve/pallet/tap/resupply escalation below.
                // TODO: Unless they're cannibal trait or precept?
                if (food is Corpse)
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

        // 4. Nearest reachable, openable, non-empty meal pallet inside the structure.
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

        // 5. Nearest dispensable nutrient-paste dispenser / VNPE tap inside the
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

        // 3. Nearest reachable, still-closed VNPE nutrient-paste valve inside the
        // structure. Flicking it on (BTG_OpenPasteValve) reconnects a vat to the paste
        // net so step 5 can dispense later. Def is null when VNPE isn't installed - then
        // there are no valves and this is simply skipped.
        private Job TryOpenPasteValve(Pawn pawn)
        {
            ThingDef valveDef = Things.VNPE_NutrientPasteValve;
            if (valveDef == null)
                return null;

            Map map = pawn.Map;
            Danger maxDanger = MaxDanger(pawn);

            Thing best = null;
            float bestDistSq = float.MaxValue;
            List<Thing> valves = map.listerThings.ThingsOfDef(valveDef);
            for (int i = 0; i < valves.Count; i++)
            {
                Thing valve = valves[i];
                CompFlickable flickable = valve.TryGetComp<CompFlickable>();
                // Only closed valves are worth a trip; an open one already feeds the net.
                if (flickable == null || flickable.SwitchIsOn)
                    continue;
                if (!StructureBoundsCache.Contains(map, valve.Position))
                    continue;
                if (!pawn.CanReserveAndReach(valve, PathEndMode.Touch, maxDanger))
                    continue;

                float distSq = (pawn.Position - valve.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = valve;
                }
            }

            return best != null ? JobMaker.MakeJob(Jobs.BTG_OpenPasteValve, best) : null;
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
