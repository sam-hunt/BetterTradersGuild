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
        /// Also blocks rotation while settlement map is loaded (player visiting),
        /// but allows initial stock generation if stock doesn't exist yet.
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        /// <returns>False to skip original method (when blocking rotation), true otherwise</returns>
        [HarmonyPrefix]
        public static bool Prefix(Settlement_TraderTracker __instance)
        {
            Settlement settlement = __instance.settlement;
            if (settlement != null && TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                // Block rotation while map is loaded - player is visiting
                // BUT allow initial stock generation if stock doesn't exist yet
                if (settlement.Map != null)
                {
                    // Check if stock already exists - if not, allow generation
                    object existingStock = stockField?.GetValue(__instance);
                    if (existingStock != null)
                    {
                        // Stock exists - block rotation to prevent trader changing mid-visit
                        return false; // Skip original RegenerateStock
                    }
                    // Stock is null - allow initial generation to proceed
                }

                regeneratingSettlements.Value.Add(settlement.ID);
            }
            return true; // Allow original method to run
        }

        /// <summary>
        /// Postfix method - clears flag and logs after regeneration completes
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

            // Clear the regeneration flag
            regeneratingSettlements.Value.Remove(settlement.ID);

            // Log the new trader type after stock regenerates
            TraderKindDef newTraderKind = __instance.TraderKind;
            string traderLabel = newTraderKind?.label ?? "none";
            int lastStockTicks = (int)lastStockGenerationTicksField.GetValue(__instance);

            Log.Message($"[Better Traders Guild] {settlement.Label} stock regenerated → trader: {traderLabel} " +
                       $"(ticks={lastStockTicks}, commonality={newTraderKind?.CalculatedCommonality:F2})");
        }
    }
}
