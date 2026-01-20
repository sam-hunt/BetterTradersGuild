using BetterTradersGuild.MapComponents;
using BetterTradersGuild.RoomContents.CargoVault;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: SettlementDefeatUtility.CheckDefeated method
    /// Preserves trade inventory in a MapComponent before settlement defeat.
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: When a settlement is defeated:
    /// 1. CheckDefeated() creates a DestroyedSettlement
    /// 2. Reassigns map.info.parent = destroyedSettlement
    /// 3. Destroys the original Settlement via settlement.Destroy()
    /// 4. Settlement.PostRemove() calls trader.TryDestroyStock() - stock is destroyed
    ///
    /// The cargo vault pocket map cannot access trade inventory after defeat because:
    /// - pocketMap.Parent.sourceMap.Parent is now DestroyedSettlement (not Settlement)
    /// - DestroyedSettlement has no trader tracker or stock
    ///
    /// Solution: This prefix captures the trade inventory into a MapComponent before
    /// defeat processing. The settlement map survives (just gets reparented), so the
    /// MapComponent persists and cargo vault generation can access the cached stock.
    /// </remarks>
    [HarmonyPatch(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated))]
    public static class SettlementDefeatUtilityCheckDefeated
    {
        /// <summary>
        /// Prefix method - captures trade inventory before defeat processing.
        /// </summary>
        /// <param name="factionBase">The settlement being checked for defeat</param>
        [HarmonyPrefix]
        public static void Prefix(Settlement factionBase)
        {
            // Only process TradersGuild settlements
            if (!TradersGuildHelper.IsTradersGuildSettlement(factionBase))
                return;

            // Settlement must have a map loaded
            Map map = factionBase.Map;
            if (map == null)
                return;

            // Get or add the cache component
            var cache = map.GetComponent<SettlementStockCache>();
            if (cache == null)
            {
                cache = new SettlementStockCache(map);
                map.components.Add(cache);
            }

            // If we already have preserved stock, don't overwrite
            // (CheckDefeated can be called multiple times)
            if (cache.preservedStock.Count > 0)
                return;

            // Get stock - uses Settlement overload which regenerates if needed.
            // This handles the case where player defeats settlement without ever
            // trading or entering the cargo vault (stock is lazily generated).
            ThingOwner<Thing> stock = CargoVaultHelper.GetStock(factionBase);
            if (stock == null || stock.Count == 0)
                return;

            // Store original settlement ID for deterministic seeding
            cache.originalSettlementId = factionBase.ID;

            // Transfer all items from trader stock to our cache
            // This empties the original stock, so TryDestroyStock becomes a no-op
            cache.preservedStock.TryAddRangeOrTransfer(stock, canMergeWithExistingStacks: true);

            Log.Message($"[BTG] Preserved {cache.preservedStock.Count} items from {factionBase.Label} before defeat");
        }
    }
}
