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
        }

        /// <summary>
        /// Prefix method - sets flag before regeneration starts
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        [HarmonyPrefix]
        public static void Prefix(Settlement_TraderTracker __instance)
        {
            Settlement settlement = __instance.settlement;
            if (settlement != null && TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                regeneratingSettlements.Value.Add(settlement.ID);
            }
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
