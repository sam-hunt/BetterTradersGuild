using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement_TraderTracker.RegenerateStockEveryDays property getter
    /// Returns custom rotation interval for TradersGuild settlements
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: RegenerateStockEveryDays is a protected virtual property
    /// that returns the number of days between stock regenerations. Vanilla returns 30.
    ///
    /// By patching this for TradersGuild settlements, we ensure both visited and unvisited
    /// settlements use the same rotation interval (player-configurable, default 15 days).
    /// </remarks>
    [HarmonyPatch(typeof(Settlement_TraderTracker), "RegenerateStockEveryDays", MethodType.Getter)]
    public static class SettlementTraderTrackerRegenerateStockEveryDays
    {
        /// <summary>
        /// Postfix method - modifies the regeneration interval for TradersGuild settlements
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        /// <param name="__result">The regeneration interval in days (can be modified)</param>
        [HarmonyPostfix]
        public static void Postfix(Settlement_TraderTracker __instance, ref int __result)
        {
            // Only modify interval for TradersGuild settlements
            Settlement settlement = __instance.settlement;
            if (settlement == null)
                return;

            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            // Return custom interval from mod settings
            // LEARNING NOTE: Convert ticks back to days (divide by 60000)
            __result = TradersGuildTraderRotation.GetRotationIntervalTicks() / 60000;
        }
    }
}
