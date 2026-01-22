using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement_TraderTracker.TraderKind property getter
    /// Returns a weighted, deterministic orbital trader type for TradersGuild settlements
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: Uses Rand.PushState/PopState for deterministic weighted selection
    /// The seed is based on settlement ID + lastStockGenerationTicks, ensuring:
    /// - Deterministic (same settlement + same stock gen = same trader)
    /// - Weighted by CalculatedCommonality (respects rarity)
    /// - Rotation (changes when stock regenerates)
    /// - Save/load stable (uses persisted field)
    ///
    /// KEY FIX: The RegenerateStock patch sets a flag during execution that we check
    /// to detect mid-regeneration. When detected, we use Find.TickManager.TicksGame to
    /// match what RegenerateStock will set lastStockGenerationTicks to at the end,
    /// ensuring consistent trader type selection during and after stock generation.
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
            // Only intervene if TraderKind is null (not already set by vanilla)
            if (__result != null)
                return;

            // Safety check for reflection
            if (lastStockGenerationTicksField == null)
                return;

            // Access the parent settlement
            Settlement settlement = __instance.settlement;
            if (settlement == null)
                return;

            // Check if this is a TradersGuild settlement
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            // Get the faction's orbital trader types
            Faction faction = settlement.Faction;
            if (faction?.def?.orbitalTraderKinds == null || faction.def.orbitalTraderKinds.Count == 0)
            {
                Log.Warning($"[Better Traders Guild] TradersGuild faction has no orbitalTraderKinds defined! Cannot assign trader to {settlement.Label}");
                return;
            }

            // Get the current lastStockGenerationTicks
            int lastStockTicks = (int)lastStockGenerationTicksField.GetValue(__instance);

            // CRITICAL FIX: Detect if we're inside RegenerateStock()
            // During RegenerateStock(), the sequence is:
            // 1. Old stock destroyed
            // 2. NEW empty ThingOwner created (stock is NOT null, just empty with Count=0)
            // 3. TraderKind getter called (HERE!) with OLD lastStockGenerationTicks
            // 4. Stock generated
            // 5. lastStockGenerationTicks updated to Find.TickManager.TicksGame
            //
            // We must use the FUTURE value (step 5) during step 3 to avoid desync!
            //
            // Determine the tick value to use for trader selection
            // LEARNING NOTE: This logic handles multiple scenarios:
            // 1. Unvisited settlements (lastStockTicks == -1) → Use virtual schedule
            // 2. First-time regeneration → Use pending alignment value (set by alignment patch)
            // 3. Subsequent regenerations → Use TicksGame to match vanilla's end-of-method update
            int settlementID = settlement.ID;

            if (lastStockTicks == -1)
            {
                // Stock never initialized - use the virtual schedule
                // LEARNING NOTE: For uninitialized settlements, we use the helper to calculate
                // a stable, settlement-specific rotation schedule. This ensures:
                // 1. No flickering (stable across frames)
                // 2. Each settlement has its own schedule (desynchronized via settlement ID offset)
                // 3. Matches what they'll get when visited (virtual schedule alignment)
                lastStockTicks = Helpers.TradersGuildTraderRotation.GetVirtualLastStockTicks(settlementID);
            }
            else if (SettlementTraderTrackerRegenerateStock.IsRegeneratingStock(settlementID))
            {
                // We're inside RegenerateStock
                // CRITICAL: Check if the alignment patch has a pending alignment for this settlement
                // If yes → first-time generation, use the aligned value
                // If no → subsequent regeneration, use TicksGame

                if (SettlementTraderTrackerRegenerateStockAlignment.HasPendingAlignment(settlementID, out int alignedTicks))
                {
                    // First-time regeneration with alignment - use the aligned virtual value
                    // This ensures the stock is generated with the same seed as the preview
                    lastStockTicks = alignedTicks;
                }
                else
                {
                    // Subsequent regeneration - use TicksGame to match what vanilla sets at the end
                    // This allows trader rotation while avoiding desync
                    lastStockTicks = Find.TickManager.TicksGame;
                }
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
                // Weighted selection respects TraderKindDef.CalculatedCommonality (commonality * population curve)
                traderKind = faction.def.orbitalTraderKinds.RandomElementByWeight(t => t.CalculatedCommonality);
            }
            finally
            {
                Rand.PopState();
            }

            // Store in cache for subsequent accesses
            traderCache[settlementID] = new CachedTraderInfo
            {
                traderKind = traderKind,
                lastStockTicks = lastStockTicks
            };

            __result = traderKind;
        }
    }
}
