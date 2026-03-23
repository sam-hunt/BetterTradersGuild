using BetterTradersGuild.WorldComponents;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement_TraderTracker.TraderKind property getter
    /// Returns a weighted, deterministic orbital trader type for TradersGuild settlements
    /// </summary>
    /// <remarks>
    /// Uses Rand.PushState/PopState for deterministic weighted selection.
    /// The seed is based on settlement ID + effective lastStockTicks, ensuring:
    /// - Deterministic (same settlement + same rotation cycle = same trader)
    /// - Weighted by static commonality (not population-dependent CalculatedCommonality)
    /// - Rotation (changes when rotation interval expires)
    /// - Save/load stable (uses persisted field when within rotation period)
    ///
    /// KEY ARCHITECTURE: Uses TradersGuildTraderRotation.GetEffectiveLastStockTicks() to
    /// determine the correct tick value for trader selection. This unified helper ensures
    /// preview and stock generation use the same seed for the same rotation cycle.
    /// During mid-regeneration, defers to the alignment patch's pending value.
    /// </remarks>
    [HarmonyPatch(typeof(Settlement_TraderTracker), nameof(Settlement_TraderTracker.TraderKind), MethodType.Getter)]
    public static class SettlementTraderTrackerGetTraderKind
    {
        /// <summary>
        /// Cached trader information to avoid recalculating every frame
        /// </summary>
        private class CachedTraderInfo
        {
            public TraderKindDef traderKind;
            public int lastStockTicks;
        }

        // Cache the FieldInfo for accessing private lastStockGenerationTicks field
        private static FieldInfo lastStockGenerationTicksField;

        // Cache trader assignments to avoid recalculating on every property access
        // Key: Settlement ID, Value: Cached trader info
        private static Dictionary<int, CachedTraderInfo> traderCache = new Dictionary<int, CachedTraderInfo>();

        /// <summary>
        /// Clears the local trader cache. Called when rotation interval setting changes.
        /// </summary>
        public static void ClearLocalCache()
        {
            traderCache.Clear();
        }

        // Slavery and stock generator checks are in OrbitalTraderHelper (shared with quest reward system)

        /// <summary>
        /// Static constructor to initialize reflection
        /// </summary>
        static SettlementTraderTrackerGetTraderKind()
        {
            lastStockGenerationTicksField = typeof(Settlement_TraderTracker).GetField("lastStockGenerationTicks",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (lastStockGenerationTicksField == null)
            {
                Log.Error("[Better Traders Guild] Failed to find 'lastStockGenerationTicks' field via reflection!");
            }
        }

        /// <summary>
        /// Postfix method - provides weighted orbital trader types for TradersGuild settlements
        /// </summary>
        /// <param name="__instance">The Settlement_TraderTracker instance</param>
        /// <param name="__result">The TraderKindDef result (can be modified)</param>
        [HarmonyPostfix]
        public static void Postfix(Settlement_TraderTracker __instance, ref TraderKindDef __result)
        {
            // Safety check for reflection
            if (lastStockGenerationTicksField == null)
                return;

            // Access the parent settlement
            Settlement settlement = __instance.settlement;
            if (settlement == null)
                return;

            // Check if this is a TradersGuild settlement
            // IMPORTANT: Do this check BEFORE the null check, because we want to ALWAYS
            // override vanilla's result for TradersGuild settlements (not just when null).
            // Vanilla may return one of the 4 default faction traders, but we want to
            // use our extended list of ALL orbital traders.
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                // For non-TradersGuild settlements, only intervene if vanilla returned null
                if (__result != null)
                    return;
                // Otherwise fall through to provide a default (shouldn't normally happen)
                return;
            }
            // For TradersGuild settlements, always proceed to override with extended traders

            int settlementID = settlement.ID;

            // PRIORITY: Check WorldComponent cache first
            // If stock has been generated, use the exact trader that was selected during generation.
            // This avoids any recalculation divergence issues.
            var worldComponent = TradersGuildWorldComponent.GetComponent();
            if (worldComponent != null && worldComponent.TryGetCachedTraderKind(settlementID, out TraderKindDef cachedTrader))
            {
                __result = cachedTrader;
                return;
            }

            // WorldComponent cache miss (expired or never cached) - clear local cache to prevent stale data
            // The local cache uses lastStockTicks as key, which doesn't change on rotation for visited settlements
            traderCache.Remove(settlementID);

            // Fall back to deterministic calculation
            // This happens for unvisited settlements (no stock yet) or after rotation expiry

            // Get all available orbital traders (filtered for world state and ideology)
            // Uses shared helper also used by smuggler's den quest reward system
            List<TraderKindDef> allOrbitalTraders = Helpers.OrbitalTraderHelper.GetAvailableOrbitalTraders(settlement.Faction);

            if (allOrbitalTraders.Count == 0)
            {
                Log.Warning($"[Better Traders Guild] No orbital trader types found in DefDatabase! Cannot assign trader to {settlement.Label}");
                return;
            }

            // Get the current lastStockGenerationTicks from the field
            int rawLastStockTicks = (int)lastStockGenerationTicksField.GetValue(__instance);

            // Track if this is an unvisited settlement (for caching decisions)
            bool isUnvisitedSettlement = (rawLastStockTicks == -1);

            // Determine the effective lastStockTicks for trader selection
            // LEARNING NOTE: This handles the stock/dialog desync problem. During RegenerateStock():
            // 1. Old stock destroyed
            // 2. NEW empty ThingOwner created
            // 3. TraderKind getter called (HERE!) with OLD lastStockGenerationTicks
            // 4. Stock generated
            // 5. lastStockGenerationTicks updated to Find.TickManager.TicksGame
            //
            // Without intervention, step 3 and later accesses would use different seeds!
            int lastStockTicks;
            if (SettlementTraderTrackerRegenerateStock.IsRegeneratingStock(settlementID) &&
                SettlementTraderTrackerRegenerateStockAlignment.HasPendingAlignment(settlementID, out int alignedTicks))
            {
                // Mid-regeneration with alignment: use the pre-calculated aligned value
                // The alignment patch has already determined the correct virtual schedule tick
                lastStockTicks = alignedTicks;
            }
            else
            {
                // Preview or post-generation: use unified helper for all cases
                // Handles: unvisited (virtual), visited+rotated (new virtual), visited+not rotated (stored)
                lastStockTicks = Helpers.TradersGuildTraderRotation.GetEffectiveLastStockTicks(settlementID, rawLastStockTicks);
            }

            // Check cache before expensive calculation
            // The TraderKind property is accessed frequently (every frame during trade dialog)
            if (traderCache.TryGetValue(settlementID, out CachedTraderInfo cached))
            {
                if (cached.lastStockTicks == lastStockTicks)
                {
                    __result = cached.traderKind;
                    return; // Cache hit - return immediately
                }
            }

            // Cache miss - calculate new trader type with deterministic weighted selection
            int seed = Gen.HashCombineInt(settlementID, lastStockTicks);

            TraderKindDef traderKind;
            Rand.PushState(seed);
            try
            {
                // Weighted selection using STATIC commonality (not CalculatedCommonality which varies with population)
                // LEARNING NOTE: CalculatedCommonality = commonality * populationCurve, where populationCurve
                // depends on current colony population vs storyteller target. This dynamic weighting causes
                // different traders to be selected between preview and stock generation if population changed.
                // Using static commonality ensures deterministic selection given the same seed.
                traderKind = allOrbitalTraders.RandomElementByWeight(t => t.commonality);
            }
            finally
            {
                Rand.PopState();
            }

            // Store in local cache for subsequent accesses within this session
            traderCache[settlementID] = new CachedTraderInfo
            {
                traderKind = traderKind,
                lastStockTicks = lastStockTicks
            };

            // Cache to WorldComponent with expiration for ALL settlements after recalculation
            // This ensures both visited and unvisited settlements get re-cached after expiration
            if (worldComponent != null)
            {
                worldComponent.CacheTraderKind(settlementID, traderKind);
            }

            __result = traderKind;
        }
    }
}
