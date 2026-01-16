using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    /// <summary>
    /// Handles weighted random selection of cargo items from trade inventory.
    /// Higher value items have proportionally higher selection probability.
    /// </summary>
    public static class CargoSelector
    {
        /// <summary>
        /// Selects cargo from stock using weighted random selection.
        /// Higher market value items have higher selection probability.
        /// Selected items are REMOVED from the source stock.
        /// </summary>
        /// <param name="stock">The trade inventory to select from</param>
        /// <param name="budgetPercent">Percentage of total stock value to select (0.0-1.0)</param>
        /// <returns>List of Things selected and removed from stock</returns>
        public static List<Thing> SelectCargo(ThingOwner<Thing> stock, float budgetPercent)
        {
            var selected = new List<Thing>();

            if (stock == null || stock.Count == 0 || budgetPercent <= 0f)
            {
                return selected;
            }

            // Calculate total stock value and budget
            float totalValue = stock.Sum(t => t.MarketValue * t.stackCount);
            float budget = totalValue * budgetPercent;
            float spent = 0f;

            // Create mutable candidate list (items with positive market value)
            var candidates = stock.Where(t => t.MarketValue > 0f).ToList();

            Log.Message($"[BTG CargoVault] SelectCargo: Total value={totalValue:F0}, Budget={budget:F0} ({budgetPercent * 100}%)");

            // Weighted random selection until budget exhausted or no candidates
            while (spent < budget && candidates.Count > 0)
            {
                // Select random item weighted by market value
                Thing item = candidates.RandomElementByWeight(t => t.MarketValue);

                // Calculate how much we can afford (at least 1)
                float itemValue = item.MarketValue;
                int maxAfford = Mathf.Max(1, Mathf.FloorToInt((budget - spent) / itemValue));
                int toTake = Mathf.Min(maxAfford, item.stackCount);

                // Split from stock (removes the items)
                Thing taken = item.SplitOff(toTake);
                selected.Add(taken);
                spent += taken.MarketValue * taken.stackCount;

                // ALWAYS remove from candidates if:
                // 1. SplitOff returned the same object (non-stackable or took entire stack)
                // 2. Original item is depleted or destroyed
                if (taken == item || item.stackCount <= 0 || item.Destroyed)
                {
                    candidates.Remove(item);
                }
            }

            Log.Message($"[BTG CargoVault] SelectCargo: Spent={spent:F0}, Selected {selected.Count} items");

            return selected;
        }

        /// <summary>
        /// Categorizes selected cargo into items and pawns for different spawn handling.
        /// Pawns spawn on floor; items try shelves first, then floor.
        /// </summary>
        /// <param name="cargo">All selected cargo</param>
        /// <param name="items">Output: Non-pawn items</param>
        /// <param name="pawns">Output: Pawns (slaves from pirate merchants)</param>
        public static void CategorizeItems(
            List<Thing> cargo,
            out List<Thing> items,
            out List<Pawn> pawns)
        {
            items = new List<Thing>();
            pawns = new List<Pawn>();

            foreach (Thing thing in cargo)
            {
                if (thing is Pawn pawn)
                {
                    pawns.Add(pawn);
                }
                else
                {
                    items.Add(thing);
                }
            }
        }
    }
}
