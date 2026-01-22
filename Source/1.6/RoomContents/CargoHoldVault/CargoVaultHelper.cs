using System.Reflection;
using BetterTradersGuild.MapComponents;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    /// <summary>
    /// Helper class for cargo vault operations.
    /// Provides navigation from pocket maps to parent settlements and stock access.
    /// </summary>
    public static class CargoVaultHelper
    {
        /// <summary>
        /// Cached reflection access to Settlement_TraderTracker.stock private field
        /// </summary>
        private static readonly FieldInfo stockField = typeof(Settlement_TraderTracker)
            .GetField("stock", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Navigates from a pocket map to its parent settlement.
        /// The pocket map's Parent is a PocketMapParent, which has a sourceMap
        /// that belongs to the actual Settlement.
        /// </summary>
        /// <param name="pocketMap">The pocket map (cargo vault map)</param>
        /// <returns>The parent Settlement, or null if not found</returns>
        public static Settlement GetParentSettlement(Map pocketMap)
        {
            if (pocketMap?.Parent is PocketMapParent pocketParent)
            {
                Map sourceMap = pocketParent.sourceMap;
                return sourceMap?.Parent as Settlement;
            }
            return null;
        }

        /// <summary>
        /// Gets the source map (settlement map) from a pocket map.
        /// </summary>
        /// <param name="pocketMap">The pocket map (cargo vault map)</param>
        /// <returns>The settlement map, or null if not found</returns>
        public static Map GetSettlementMap(Map pocketMap)
        {
            if (pocketMap?.Parent is PocketMapParent pocketParent)
            {
                return pocketParent.sourceMap;
            }
            return null;
        }

        /// <summary>
        /// Gets the trade stock from a settlement's trader tracker using reflection.
        /// </summary>
        /// <param name="tracker">The settlement's trader tracker</param>
        /// <returns>The stock ThingOwner, or null if reflection fails</returns>
        /// <remarks>
        /// LEARNING NOTE: Settlement_TraderTracker.stock is private, requiring reflection.
        /// This pattern is also used in SettlementTraderTrackerRegenerateStock.cs.
        /// </remarks>
        public static ThingOwner<Thing> GetStock(Settlement_TraderTracker tracker)
        {
            if (tracker == null || stockField == null)
            {
                return null;
            }
            return stockField.GetValue(tracker) as ThingOwner<Thing>;
        }

        /// <summary>
        /// Gets the trade stock from a settlement (pure getter - no side effects).
        /// </summary>
        /// <param name="settlement">The settlement</param>
        /// <returns>The stock ThingOwner, or null if not available</returns>
        /// <remarks>
        /// This is a pure getter that never triggers regeneration.
        /// Stock generation is handled by SettlementMapGenerated patch when the map loads.
        /// This ensures stock is frozen for the entire duration of the settlement visit.
        /// </remarks>
        public static ThingOwner<Thing> GetStock(Settlement settlement)
        {
            if (settlement?.trader == null)
                return null;

            var tracker = settlement.trader;
            var stock = GetStock(tracker);
            Log.Message($"[BTG DEBUG] GetStock(Settlement): settlement.ID={settlement.ID}, tracker={tracker.GetHashCode()}, stock={(stock == null ? "null" : $"{stock.Count} items")}");
            return stock;
        }

        /// <summary>
        /// Gets the trade stock for a pocket map, with fallback to cached stock.
        /// Handles the case where the parent settlement has been defeated.
        /// </summary>
        /// <param name="pocketMap">The pocket map (cargo vault)</param>
        /// <returns>The stock ThingOwner, or null if not available</returns>
        /// <remarks>
        /// Access pattern:
        /// 1. Try normal path first (settlement still exists)
        /// 2. Fallback: get cached stock from settlement map (settlement was defeated)
        ///
        /// The settlement map survives defeat (just gets a new DestroyedSettlement parent),
        /// so the SettlementStockCache MapComponent persists with the preserved inventory.
        /// </remarks>
        public static ThingOwner<Thing> GetStock(Map pocketMap)
        {
            Settlement settlement = GetParentSettlement(pocketMap);
            Log.Message($"[BTG DEBUG] GetStock(pocketMap): settlement={settlement?.Label ?? "null"}, destroyed={settlement?.Destroyed ?? true}");

            // If settlement is alive (not destroyed) and has a trader, use trader stock
            // Return trader stock even if empty - that's where returned items should go
            if (settlement?.trader != null && !settlement.Destroyed)
            {
                var stock = GetStock(settlement);
                Log.Message($"[BTG DEBUG] GetStock(pocketMap): using settlement stock ({stock?.Count ?? -1} items)");
                return stock;
            }

            // Settlement destroyed or no trader - fall back to cache
            Map settlementMap = GetSettlementMap(pocketMap);
            Log.Message($"[BTG DEBUG] GetStock(pocketMap): settlement destroyed/null, trying cache on {settlementMap?.ToString() ?? "null"}");
            if (settlementMap != null)
            {
                var cache = settlementMap.GetComponent<SettlementStockCache>();
                if (cache?.preservedStock != null)
                {
                    Log.Message($"[BTG DEBUG] GetStock(pocketMap): using cache ({cache.preservedStock.Count} items)");
                    return cache.preservedStock;
                }
            }

            Log.Warning("[BTG CargoVault] No stock available (settlement destroyed and no cache)");
            return null;
        }

        /// <summary>
        /// Gets the original settlement ID for deterministic seeding.
        /// Handles the case where the parent settlement has been defeated.
        /// </summary>
        /// <param name="pocketMap">The pocket map (cargo vault)</param>
        /// <returns>The settlement ID, or map.Tile as fallback</returns>
        public static int GetSettlementId(Map pocketMap)
        {
            // Try normal path first (settlement still exists)
            Settlement settlement = GetParentSettlement(pocketMap);
            if (settlement != null)
                return settlement.ID;

            // Fallback: get ID from cache (settlement was defeated)
            Map settlementMap = GetSettlementMap(pocketMap);
            if (settlementMap != null)
            {
                var cache = settlementMap.GetComponent<SettlementStockCache>();
                if (cache != null && cache.originalSettlementId != 0)
                    return cache.originalSettlementId;
            }

            // Last resort: use map tile
            return pocketMap?.Tile ?? 0;
        }
    }
}
