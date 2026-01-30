using System.Reflection;
using BetterTradersGuild.Helpers;
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

        // Cached reflection access to lastStockGenerationTicks field
        private static readonly FieldInfo lastStockGenerationTicksField = typeof(Settlement_TraderTracker)
            .GetField("lastStockGenerationTicks", BindingFlags.NonPublic | BindingFlags.Instance);

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
                // Stock already exists - check if it should have expired (rotation occurred while away)
                // Use unified helper to detect rotation: if effective differs from stored, rotation occurred
                int storedLastStockTicks = (int)(lastStockGenerationTicksField?.GetValue(tracker) ?? -1);
                int effectiveTicks = TradersGuildTraderRotation.GetEffectiveLastStockTicks(settlement.ID, storedLastStockTicks);
                bool rotationOccurred = (storedLastStockTicks != -1 && effectiveTicks != storedLastStockTicks);

                if (rotationOccurred)
                {
                    // Stock has expired - clear it so it will be regenerated below
                    // Clear the stock - this allows RegenerateStock to proceed
                    var stockOwner = existingStock as ThingOwner<Thing>;
                    stockOwner?.ClearAndDestroyContents();
                    stockField.SetValue(tracker, null);

                    // Reset lastStockGenerationTicks to -1 so alignment patch will align to virtual schedule
                    lastStockGenerationTicksField.SetValue(tracker, -1);
                }
                else
                {
                    // Stock still valid - leave it frozen as-is
                    return;
                }
            }

            // Stock is null - generate it now to establish the invariant
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
                if (newStock == null)
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
