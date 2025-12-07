using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement_TraderTracker.RegenerateStock method
    /// Aligns first-time stock generation with virtual rotation schedule
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: Vanilla RegenerateStock() sets lastStockGenerationTicks = Find.TickManager.TicksGame
    /// at the END of the method. For first-time generation, we override this with the virtual schedule
    /// value to ensure the preview trader type matches what you get when visiting.
    ///
    /// This patch uses Prefix to detect first-time generation and Postfix to restore the aligned value
    /// after vanilla overwrites it.
    /// </remarks>
    [HarmonyPatch(typeof(Settlement_TraderTracker), "RegenerateStock")]
    public static class SettlementTraderTrackerRegenerateStockAlignment
    {
        // Cache the FieldInfo for accessing private lastStockGenerationTicks
        private static FieldInfo lastStockGenerationTicksField;

        // Thread-local tracking of settlements needing alignment
        // Key: Settlement ID, Value: Virtual lastStockTicks to restore
        private static ThreadLocal<Dictionary<int, int>> pendingAlignments =
            new ThreadLocal<Dictionary<int, int>>(() => new Dictionary<int, int>());

        /// <summary>
        /// Check if a settlement has a pending alignment (first-time regeneration in progress)
        /// </summary>
        public static bool HasPendingAlignment(int settlementID, out int virtualTicks)
        {
            return pendingAlignments.Value.TryGetValue(settlementID, out virtualTicks);
        }

        /// <summary>
        /// Static constructor to initialize reflection
        /// </summary>
        static SettlementTraderTrackerRegenerateStockAlignment()
        {
            lastStockGenerationTicksField = typeof(Settlement_TraderTracker).GetField("lastStockGenerationTicks",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (lastStockGenerationTicksField == null)
            {
                Verse.Log.Error("[Better Traders Guild] Failed to find 'lastStockGenerationTicks' field for alignment patch!");
            }
        }

        /// <summary>
        /// Prefix method - detects first-time generation and calculates virtual schedule
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        [HarmonyPrefix]
        public static void Prefix(Settlement_TraderTracker __instance)
        {
            // Safety check for reflection
            if (lastStockGenerationTicksField == null)
                return;

            // Access the parent settlement
            Settlement settlement = __instance.settlement;
            if (settlement == null)
                return;

            // Only intervene for TradersGuild settlements
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            // Get current lastStockGenerationTicks
            int currentLastStockTicks = (int)lastStockGenerationTicksField.GetValue(__instance);

            // Only align on first generation (when lastStockTicks == -1)
            if (currentLastStockTicks != -1)
                return;

            // Calculate virtual lastStockTicks for this settlement
            int virtualLastStockTicks = TradersGuildTraderRotation.GetVirtualLastStockTicks(settlement.ID);

            // Store for Postfix to restore after vanilla overwrites it
            pendingAlignments.Value[settlement.ID] = virtualLastStockTicks;

            // Set it now so the TraderKind getter uses the correct value during stock generation
            lastStockGenerationTicksField.SetValue(__instance, virtualLastStockTicks);
        }

        /// <summary>
        /// Postfix method - restores aligned value after vanilla overwrites it
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

            // Only intervene for TradersGuild settlements
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            // Check if this settlement had a pending alignment
            if (!pendingAlignments.Value.TryGetValue(settlement.ID, out int virtualTicks))
                return;

            // Remove from pending
            pendingAlignments.Value.Remove(settlement.ID);

            // Restore the virtual value that vanilla just overwrote with TicksGame
            // LEARNING NOTE: Vanilla RegenerateStock() ends with:
            // lastStockGenerationTicks = Find.TickManager.TicksGame
            // We restore our aligned value here to maintain the virtual schedule
            lastStockGenerationTicksField.SetValue(__instance, virtualTicks);

            Verse.Log.Message($"[Better Traders Guild] Aligned {settlement.Label} to virtual schedule " +
                             $"(virtualTicks={virtualTicks}, prevented vanilla overwrite)");
        }
    }
}
