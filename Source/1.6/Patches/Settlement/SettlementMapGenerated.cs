using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Ensures TradersGuild settlement stock is generated when the settlement map loads.
    ///
    /// This establishes the invariant: once settlement.Map is non-null, stock is guaranteed to exist.
    /// Combined with patches that block RegenerateStock and TryDestroyStock while the map is active,
    /// this ensures stock remains frozen for the duration of the visit.
    /// </summary>
    /// <remarks>
    /// ARCHITECTURE:
    /// - On map load: Ensure stock exists (generate if null)
    /// - While map active: Stock is frozen (other patches block changes)
    /// - On defeat: Stock transfers to cache (CheckDefeated patch)
    /// - GetStock: Pure getter, never regenerates
    ///
    /// This patch hooks into Map.FinalizeInit which is called after map generation completes
    /// and the map is fully initialized. At this point settlement.Map is set.
    /// </remarks>
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class SettlementMapGenerated
    {
        // Cached reflection access to RegenerateStock method
        private static readonly MethodInfo regenerateStockMethod = typeof(Settlement_TraderTracker)
            .GetMethod("RegenerateStock", BindingFlags.NonPublic | BindingFlags.Instance);

        // Cached reflection access to stock field
        private static readonly FieldInfo stockField = typeof(Settlement_TraderTracker)
            .GetField("stock", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Postfix that ensures TradersGuild settlement stock exists after map initialization.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Map __instance)
        {
            // Check if this map belongs to a TradersGuild settlement
            if (__instance?.Parent is not Settlement settlement)
                return;

            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            // Check if stock already exists
            if (settlement.trader == null)
                return;

            var tracker = settlement.trader;
            object existingStock = stockField?.GetValue(tracker);
            if (existingStock != null)
            {
                // Stock already exists - leave it frozen as-is
                var stockOwner = existingStock as ThingOwner<Thing>;
                Log.Message($"[BTG] Settlement map loaded: settlement.ID={settlement.ID}, tracker={tracker.GetHashCode()}, stock has {stockOwner?.Count ?? -1} items");
                return;
            }

            // Stock is null - generate it now to establish the invariant
            Log.Message($"[BTG] Settlement map loaded: Generating initial stock for {settlement.Label}");

            if (regenerateStockMethod == null)
            {
                Log.Error("[BTG] Failed to find RegenerateStock method via reflection");
                return;
            }

            try
            {
                // This will be allowed through our RegenerateStock patch because stock is null
                regenerateStockMethod.Invoke(settlement.trader, null);

                // Verify it worked
                object newStock = stockField?.GetValue(settlement.trader);
                if (newStock != null)
                {
                    var stockOwner = newStock as ThingOwner<Thing>;
                    Log.Message($"[BTG] Stock generated successfully: {stockOwner?.Count ?? 0} items");
                }
                else
                {
                    Log.Warning("[BTG] Stock generation appeared to fail - stock is still null");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[BTG] Exception during stock generation: {ex.Message}");
            }
        }
    }
}
