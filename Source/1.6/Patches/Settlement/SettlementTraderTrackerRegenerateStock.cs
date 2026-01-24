using BetterTradersGuild.WorldComponents;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement_TraderTracker.RegenerateStock method
    /// Sets a flag during regeneration to help TraderKind getter detect mid-regeneration state
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: This patch sets a thread-local flag that the TraderKind getter uses
    /// to detect when it's being called from within RegenerateStock(). This is more reliable
    /// than checking if stock is empty, which can have false positives.
    /// </remarks>
    [HarmonyPatch(typeof(Settlement_TraderTracker), "RegenerateStock")]
    public static class SettlementTraderTrackerRegenerateStock
    {
        // Cache the FieldInfo for accessing private lastStockGenerationTicks
        private static FieldInfo lastStockGenerationTicksField;

        // Cache the FieldInfo for accessing private stock field
        private static FieldInfo stockField;

        // Thread-local set of settlement IDs currently regenerating stock
        // Using ThreadLocal to avoid issues with concurrent regeneration
        private static ThreadLocal<HashSet<int>> regeneratingSettlements =
            new ThreadLocal<HashSet<int>>(() => new HashSet<int>());

        /// <summary>
        /// Check if a settlement is currently regenerating stock
        /// </summary>
        public static bool IsRegeneratingStock(int settlementID)
        {
            return regeneratingSettlements.Value.Contains(settlementID);
        }

        /// <summary>
        /// Static constructor to initialize reflection
        /// </summary>
        static SettlementTraderTrackerRegenerateStock()
        {
            lastStockGenerationTicksField = typeof(Settlement_TraderTracker).GetField("lastStockGenerationTicks",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (lastStockGenerationTicksField == null)
            {
                Log.Error("[Better Traders Guild] Failed to find 'lastStockGenerationTicks' field via reflection!");
            }

            stockField = typeof(Settlement_TraderTracker).GetField("stock",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (stockField == null)
            {
                Log.Error("[Better Traders Guild] Failed to find 'stock' field via reflection!");
            }
        }

        /// <summary>
        /// Prefix method - sets flag before regeneration starts.
        /// Also blocks rotation/regeneration while settlement map is loaded (player visiting),
        /// but allows INITIAL stock generation if stock has never been created (null).
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        /// <returns>False to skip original method (when blocking rotation), true otherwise</returns>
        /// <remarks>
        /// IMPORTANT: Only allow regeneration if stock is NULL (never created).
        /// An empty stock (Count == 0) means items were legitimately removed (trading, cargo vault)
        /// and should NOT trigger regeneration - we want the stock to remain frozen while visiting.
        /// </remarks>
        [HarmonyPrefix]
        public static bool Prefix(Settlement_TraderTracker __instance)
        {
            Settlement settlement = __instance.settlement;
            if (settlement != null && TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                // Block rotation/regeneration while map is loaded - player is visiting
                // ONLY allow initial generation if stock has never been created (null)
                if (settlement.Map != null)
                {
                    object existingStock = stockField?.GetValue(__instance);
                    if (existingStock != null)
                    {
                        // Stock exists (even if empty) - block to keep it frozen
                        return false; // Skip original RegenerateStock
                    }
                    // Stock is null - allow initial generation (called by SettlementMapGenerated)
                }

                regeneratingSettlements.Value.Add(settlement.ID);
            }
            return true; // Allow original method to run
        }

        /// <summary>
        /// Postfix method - caches trader kind and clears flag after regeneration completes
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        [HarmonyPostfix]
        public static void Postfix(Settlement_TraderTracker __instance)
        {
            // Safety check for reflection
            if (lastStockGenerationTicksField == null)
                return;

            // Access the parent settlement
            Settlement settlement = __instance.settlement;
            if (settlement == null)
                return;

            // Only process TradersGuild settlements
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            // CRITICAL: Cache the trader kind BEFORE clearing the regeneration flag!
            // The TraderKind getter checks IsRegeneratingStock() to know whether to use
            // HasPendingAlignment(). If we clear the flag first, the getter won't check
            // the pending alignment and will use the wrong lastStockTicks value (TicksGame
            // instead of the aligned virtual ticks), resulting in a different trader type.
            var worldComponent = TradersGuildWorldComponent.GetComponent();
            if (worldComponent != null)
            {
                TraderKindDef traderKind = __instance.TraderKind;
                if (traderKind != null)
                {
                    worldComponent.CacheTraderKind(settlement.ID, traderKind);
                }
            }

            // Clear the regeneration flag AFTER caching
            regeneratingSettlements.Value.Remove(settlement.ID);
        }
    }
}
