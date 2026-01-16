using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.MessHall
{
    public class RoomContents_MessHall : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            foreach (CellRect roomRect in room.rects)
            {
                RoomEdgeConnector.ConnectBuildingsToConduitNetwork(map, roomRect, Things.Table_interactive_2x2c);
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Daylily, growth: 1.0f);
                FillShelvesWithMeals(map, roomRect);
            }

        }

        /// <summary>
        /// Fills all shelves in the room with random meal types.
        /// Each cell gets a randomly selected meal type for visual variety.
        /// Only fills one slot per cell with a partial stack (6-8 meals) for balance.
        /// </summary>
        private void FillShelvesWithMeals(Map map, CellRect roomRect)
        {
            // Get all shelves in the room (any size)
            var shelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, requiredWidth: null);

            if (shelves.Count == 0)
                return;

            // Get all meal defs from the FoodMeals category
            var mealDefs = GetAllMealDefs();
            if (mealDefs.Count == 0)
                return;

            foreach (var shelf in shelves)
            {
                var slots = RoomShelfHelper.GetShelfSlotCells(shelf);
                foreach (var slot in slots)
                {
                    // Pick a random meal type for each cell (visual variety)
                    ThingDef mealDef = mealDefs.RandomElement();

                    // Spawn 6-8 meals per stack (balanced loot quantity)
                    int stackCount = Rand.RangeInclusive(6, 8);

                    // Only fill one slot per cell (shelves have 3 slots)
                    // AddItemsToShelf handles empty slot prioritization
                    RoomShelfHelper.AddItemsToShelf(map, shelf, mealDef, stackCount, setForbidden: true);
                }
            }
        }

        /// <summary>
        /// Gets all meal ThingDefs from the FoodMeals category.
        /// Falls back to checking thingCategories if FoodMeals category not found.
        /// </summary>
        private List<ThingDef> GetAllMealDefs()
        {
            // Try to get the FoodMeals category first
            var foodMealsCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("FoodMeals");
            if (foodMealsCategory != null && foodMealsCategory.childThingDefs != null)
            {
                return foodMealsCategory.childThingDefs.ToList();
            }

            // Fallback: find all ThingDefs that are in a meal-related category
            return DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.thingCategories != null &&
                              def.thingCategories.Any(cat =>
                                  cat.defName.Contains("Meals") ||
                                  cat.defName == "FoodMeals"))
                .ToList();
        }
    }
}
