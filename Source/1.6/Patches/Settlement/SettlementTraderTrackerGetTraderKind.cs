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
        /// Checks if a faction's ideology approves of slavery.
        /// Returns true if no ideology system is active (classic mode) or if any ideo approves.
        /// </summary>
        private static bool FactionApprovesOfSlavery(Faction faction)
        {
            // No ideology system (classic mode) = slavery implicitly approved via Slavery_Classic precept
            if (faction?.ideos == null)
                return true;

            foreach (var ideo in faction.ideos.AllIdeos)
            {
                if (IdeoUtility.IdeoApprovesOfSlavery(ideo))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a trader kind has a StockGenerator_Slaves in its stock generators.
        /// Used to filter out slave ships when the faction doesn't approve of slavery.
        /// </summary>
        private static bool HasSlavesStockGenerator(TraderKindDef trader)
        {
            if (trader.stockGenerators == null)
                return false;

            foreach (var sg in trader.stockGenerators)
            {
                if (sg is StockGenerator_Slaves)
                    return true;
            }
            return false;
        }

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

            // Cache miss - fall back to deterministic calculation
            // This happens for unvisited settlements (no stock yet) or after game load before first access

            // Get all orbital trader types from the game (includes modded traders)
            // LEARNING NOTE: We query DefDatabase instead of faction.def.orbitalTraderKinds
            // to include orbital traders added by other mods and DLCs
            List<TraderKindDef> allOrbitalTraders = DefDatabase<TraderKindDef>.AllDefsListForReading
                .Where(t => t.orbital)
                .ToList();

            // Filter out traders with slave stock generators if faction's ideology doesn't approve
            // This prevents the slave ship from appearing when it can't actually deliver slaves
            // (StockGenerator_Slaves checks IdeoApprovesOfSlavery and generates nothing if false)
            if (!FactionApprovesOfSlavery(settlement.Faction))
            {
                allOrbitalTraders.RemoveAll(t => HasSlavesStockGenerator(t));
            }

            if (allOrbitalTraders.Count == 0)
            {
                Log.Warning($"[Better Traders Guild] No orbital trader types found in DefDatabase! Cannot assign trader to {settlement.Label}");
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
