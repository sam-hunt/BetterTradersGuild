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
        /// Cached reflection access to Settlement_TraderTracker.RegenerateStock() protected method
        /// </summary>
        private static readonly MethodInfo regenerateStockMethod = typeof(Settlement_TraderTracker)
            .GetMethod("RegenerateStock", BindingFlags.NonPublic | BindingFlags.Instance);

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
        /// Gets the trade stock from a settlement.
        /// Convenience method that handles null checks.
        /// Will trigger stock generation if not already generated.
        /// </summary>
        /// <param name="settlement">The settlement</param>
        /// <returns>The stock ThingOwner, or null if not available</returns>
        public static ThingOwner<Thing> GetStock(Settlement settlement)
        {
            if (settlement?.trader == null)
                return null;

            // Check if stock needs to be generated
            ThingOwner<Thing> existingStock = GetStock(settlement.trader);
            if (existingStock == null)
            {
                TraderKindDef traderKind = settlement.trader.TraderKind;
                if (traderKind == null || regenerateStockMethod == null)
                {
                    Log.Warning("[BTG CargoVault] GetStock: Cannot generate stock - TraderKind or RegenerateStock method unavailable");
                    return null;
                }

                try
                {
                    regenerateStockMethod.Invoke(settlement.trader, null);
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[BTG CargoVault] GetStock: Exception during RegenerateStock: {ex.Message}");
                    return null;
                }
            }

            return GetStock(settlement.trader);
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
            // Try normal path first (settlement still exists)
            Settlement settlement = GetParentSettlement(pocketMap);
            if (settlement?.trader != null)
            {
                var stock = GetStock(settlement);
                if (stock != null)
                    return stock;
            }

            // Fallback: get cached stock from settlement map (settlement was defeated)
            // Note: Don't check Count > 0 here - the cache may be empty after cargo spawning
            // but is still the valid destination for returned items when vault is locked.
            Map settlementMap = GetSettlementMap(pocketMap);
            if (settlementMap != null)
            {
                var cache = settlementMap.GetComponent<SettlementStockCache>();
                if (cache?.preservedStock != null)
                {
                    Log.Message($"[BTG CargoVault] Using preserved stock ({cache.preservedStock.Count} items) from defeated settlement");
                    return cache.preservedStock;
                }
            }

            Log.Warning("[BTG CargoVault] No stock available (settlement defeated and no cache)");
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
