using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.MessHall
{
    /// <summary>
    /// Custom RoomContentsWorker for MessHall.
    ///
    /// Post-processes the mess hall to connect VFE Spacer interactive tables
    /// to the power grid via hidden conduits.
    ///
    /// LEARNING NOTE: This worker calls base.FillRoom() FIRST because the XML prefabs
    /// spawn the tables, and we need those to exist before we can connect them to power.
    /// The interactive table (Table_interactive_2x2c) is only present when VFE Spacer
    /// is installed (via patch), so this gracefully does nothing when VFE Spacer is absent.
    /// </summary>
    public class RoomContents_MessHall : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs (tables, chairs, etc.),
        /// then connects VFE Spacer interactive tables to power if present.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            CellRect roomRect = room.rects.First();

            // 2. Connect VFE Spacer interactive tables to power (does nothing if VFE Spacer not installed)
            RoomEdgeConnector.ConnectBuildingsToConduitNetwork(map, roomRect, Things.Table_interactive_2x2c);

            // 3. Spawn decorative daylilies in corner plant pots
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Daylily, growth: 1.0f);

            // 4. Fill shelves with random meals
            FillShelvesWithMeals(map, roomRect);
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
            {
                Log.Warning("[Better Traders Guild] No meal defs found for MessHall shelves");
                return;
            }

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
