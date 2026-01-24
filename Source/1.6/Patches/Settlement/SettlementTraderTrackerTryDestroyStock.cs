using BetterTradersGuild.WorldComponents;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement_TraderTracker.TryDestroyStock method
    /// Prevents stock destruction while settlement map is loaded or during defeat processing.
    /// </summary>
    /// <remarks>
    /// Blocks stock destruction in two scenarios:
    /// 1. While map is loaded (player visiting) - prevents 1-day stock expiry
    /// 2. During defeat processing - allows CheckDefeated Postfix to transfer stock to cache
    ///
    /// The second scenario is critical because during defeat:
    /// - CheckDefeated reparents the map (settlement.Map becomes null)
    /// - Then calls settlement.Destroy() which triggers TryDestroyStock
    /// - Without blocking, stock would be destroyed before our Postfix can cache it
    ///
    /// Coordination with CheckDefeated patch via settlementsBeingDefeated HashSet.
    /// </remarks>
    [HarmonyPatch(typeof(Settlement_TraderTracker), "TryDestroyStock")]
    public static class SettlementTraderTrackerTryDestroyStock
    {
        /// <summary>
        /// Prefix method - blocks stock destruction while map is loaded or during defeat.
        /// Also evicts from trader cache when destruction is allowed.
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        /// <returns>False to skip destruction, true to allow</returns>
        [HarmonyPrefix]
        public static bool Prefix(Settlement_TraderTracker __instance)
        {
            Settlement settlement = __instance.settlement;
            if (settlement != null && TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                // Block while map is loaded - player is visiting
                if (settlement.Map != null)
                {
                    return false;
                }

                // Block during defeat processing - stock needs to survive until Postfix can cache it
                // CheckDefeated Prefix adds to this set, Postfix removes after caching
                if (SettlementDefeatUtilityCheckDefeated.settlementsBeingDefeated.Contains(settlement.ID))
                {
                    return false;
                }

                // Destruction allowed - evict from trader cache
                // The stock is being destroyed, so the cached trader kind is no longer valid
                TradersGuildWorldComponent.GetComponent()?.RemoveCachedTraderKind(settlement.ID);
            }
            return true;
        }
    }
}
