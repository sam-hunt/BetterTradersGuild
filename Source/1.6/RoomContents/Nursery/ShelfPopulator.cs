using System.Collections.Generic;
using RimWorld;
using Verse;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents.Nursery
{
    /// <summary>
    /// Populates nursery shelves with baby food and survival meals.
    /// </summary>
    public static class ShelfPopulator
    {
        /// <summary>
        /// Populates the nursery shelf with baby food and packaged survival meals.
        /// Uses RoomShelfHelper to find and fill shelves in the subroom.
        ///
        /// Contents:
        /// - 30-50 baby food (for infants)
        /// - 12-20 packaged survival meals in two stacks (max stack size is 10)
        /// </summary>
        public static void PopulateNurseryShelf(Map map, CellRect subroomRect)
        {
            // Find shelves in the subroom
            List<Building_Storage> shelves = RoomShelfHelper.GetShelvesInRoom(map, subroomRect, "Shelf", null);

            if (shelves.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No shelves found in nursery subroom for food storage.");
                return;
            }

            int itemsAdded = 0;

            // Add baby food (30-50 units)
            int babyFoodCount = Rand.RangeInclusive(30, 50);
            Thing babyFood = RoomShelfHelper.AddItemsToShelf(map, shelves[0], "BabyFood", babyFoodCount, setForbidden: true);
            if (babyFood != null)
            {
                itemsAdded++;
            }

            // Add packaged survival meals in two stacks (max stack size is 10)
            // Stack 1: Full stack of 10
            Thing meals1 = RoomShelfHelper.AddItemsToShelf(map, shelves[0], "MealSurvivalPack", 10, setForbidden: true);
            if (meals1 != null)
            {
                itemsAdded++;
            }
            // Stack 2: Partial stack of 2-10
            int partialMealCount = Rand.RangeInclusive(2, 10);
            Thing meals2 = RoomShelfHelper.AddItemsToShelf(map, shelves[0], "MealSurvivalPack", partialMealCount, setForbidden: true);
            if (meals2 != null)
            {
                itemsAdded++;
            }
        }
    }
}
