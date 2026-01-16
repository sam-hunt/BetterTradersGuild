using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement_TraderTracker.TryDestroyStock method
    /// Prevents stock auto-expiry (1-day timeout) while settlement map is loaded.
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: Vanilla calls TryDestroyStock() from TraderTrackerTick() when
    /// stock is more than 60000 ticks (1 day) old. Without this patch, if a player
    /// stays on a settlement map for more than a day (tending wounds, looting), the
    /// trade stock would expire and the cargo would spawn nothing.
    ///
    /// This works in tandem with the RegenerateStock rotation lock patch to ensure
    /// trade inventory is stable while the player is visiting.
    /// </remarks>
    [HarmonyPatch(typeof(Settlement_TraderTracker), "TryDestroyStock")]
    public static class SettlementTraderTrackerTryDestroyStock
    {
        /// <summary>
        /// Prefix method - blocks stock destruction while settlement map is loaded.
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        /// <returns>False to skip destruction (when map is loaded), true otherwise</returns>
        [HarmonyPrefix]
        public static bool Prefix(Settlement_TraderTracker __instance)
        {
            Settlement settlement = __instance.settlement;
            if (settlement != null && TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                // Block stock destruction while map is loaded - player is visiting
                if (settlement.Map != null)
                {
                    return false; // Skip original TryDestroyStock
                }
            }
            return true; // Allow original method to run
        }
    }
}
