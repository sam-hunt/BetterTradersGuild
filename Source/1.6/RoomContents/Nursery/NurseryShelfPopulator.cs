using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Nursery
{
    /// <summary>
    /// Populates nursery shelves with baby food and survival meals.
    /// </summary>
    public static class NurseryShelfPopulator
    {
        /// <summary>
        /// Populates the nursery shelf with baby food and packaged survival meals.
        /// Uses RoomShelfHelper to find and fill shelves in the subroom.
        ///
        /// Contents:
        /// - 40-80 baby food (for infants)
        /// - 16-20 packaged survival meals in two stacks (max stack size is 10)
        /// </summary>
        public static void PopulateNurseryShelf(Map map, CellRect subroomRect)
        {
            // Find shelves in the subroom
            List<Building_Storage> shelves = RoomShelfHelper.GetShelvesInRoom(map, subroomRect, Things.Shelf, null);

            if (shelves.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No shelves found in nursery subroom for food storage.");
                return;
            }

            // Add baby food (30-50 units) - requires Biotech DLC
            if (Things.BabyFood != null)
            {
                int babyFoodCount = Rand.RangeInclusive(40, 80);
                RoomShelfHelper.AddItemsToShelf(map, shelves[0], Things.BabyFood, babyFoodCount, setForbidden: true);
            }

            // Add packaged survival meals in two stacks (max stack size is 10)
            // Stack 1: Full stack of 10
            RoomShelfHelper.AddItemsToShelf(map, shelves[0], Things.MealSurvivalPack, 10, setForbidden: true);
            // Stack 2: Partial stack of 6-10
            int partialMealCount = Rand.RangeInclusive(6, 10);
            RoomShelfHelper.AddItemsToShelf(map, shelves[0], Things.MealSurvivalPack, partialMealCount, setForbidden: true);
        }
    }
}
