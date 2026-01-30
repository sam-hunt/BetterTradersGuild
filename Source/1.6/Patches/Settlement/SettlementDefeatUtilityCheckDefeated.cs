using System.Collections.Generic;
using BetterTradersGuild.MapComponents;
using BetterTradersGuild.RoomContents.CargoVault;
using BetterTradersGuild.WorldComponents;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: SettlementDefeatUtility.CheckDefeated method
    /// Preserves trade inventory in a MapComponent after settlement defeat is confirmed.
    /// </summary>
    /// <remarks>
    /// ARCHITECTURE:
    /// When a settlement is defeated, CheckDefeated():
    /// 1. Creates a DestroyedSettlement
    /// 2. Reassigns map.info.parent = destroyedSettlement (map reparented, settlement.Map becomes null)
    /// 3. Destroys the original Settlement via settlement.Destroy()
    /// 4. Settlement.PostRemove() calls trader.TryDestroyStock() - blocked by our TryDestroyStock patch
    ///
    /// Our approach uses Prefix + Postfix with coordination:
    /// - Prefix: Capture map reference, add settlement ID to settlementsBeingDefeated set
    /// - TryDestroyStock patch: Block if settlement ID is in settlementsBeingDefeated
    /// - Postfix: After CheckDefeated completes, transfer stock if defeated, remove from set
    ///
    /// This coordination ensures TryDestroyStock is blocked during defeat processing
    /// even after settlement.Map becomes null (due to map reparenting).
    ///
    /// Priority.Last ensures we run after other mods' Postfixes.
    /// </remarks>
    [HarmonyPatch(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated))]
    public static class SettlementDefeatUtilityCheckDefeated
    {
        /// <summary>
        /// Settlement IDs currently being processed by CheckDefeated.
        /// Used by TryDestroyStock patch to block destruction during defeat processing.
        /// </summary>
        public static readonly HashSet<int> settlementsBeingDefeated = new HashSet<int>();

        /// <summary>
        /// State passed from Prefix to Postfix.
        /// Captures references that become unavailable after defeat processing.
        /// </summary>
        public class DefeatState
        {
            public Map Map;
            public int SettlementId;
            public bool IsTradersGuild;
        }

        /// <summary>
        /// Prefix: Capture map reference and mark settlement as being processed.
        /// After defeat, settlement.Map is null, so we need to store it here.
        /// The settlementsBeingDefeated set tells TryDestroyStock to block destruction.
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(Settlement factionBase, out DefeatState __state)
        {
            __state = null;

            // Only process TradersGuild settlements with loaded maps
            if (factionBase?.Map == null)
                return;

            if (!TradersGuildHelper.IsTradersGuildSettlement(factionBase))
                return;

            // Mark settlement as being processed - TryDestroyStock will check this
            settlementsBeingDefeated.Add(factionBase.ID);

            // Capture state before defeat processing potentially reparents the map
            __state = new DefeatState
            {
                Map = factionBase.Map,
                SettlementId = factionBase.ID,
                IsTradersGuild = true
            };
        }

        /// <summary>
        /// Postfix: After CheckDefeated completes, transfer stock to cache if defeat confirmed.
        /// Always removes settlement from settlementsBeingDefeated set to prevent leaks.
        /// Runs with Priority.Last to ensure we execute after all other mods' patches.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Settlement factionBase, DefeatState __state)
        {
            // Always clean up the set, even if we're not processing this settlement
            if (__state != null)
            {
                settlementsBeingDefeated.Remove(__state.SettlementId);
            }

            // No state means not a TradersGuild settlement or no map was loaded
            if (__state == null || !__state.IsTradersGuild)
                return;

            // CRITICAL: Only transfer if defeat actually happened
            // This is the key safety check - we KNOW defeat occurred, not just predicted
            if (!factionBase.Destroyed)
                return;

            // Settlement is destroyed - evict from trader cache
            // TryDestroyStock was blocked during defeat processing, so we handle eviction here
            TradersGuildWorldComponent.GetComponent()?.RemoveCachedTraderKind(__state.SettlementId);

            Map map = __state.Map;
            if (map == null)
                return;

            // Get or add the cache component
            var cache = map.GetComponent<SettlementStockCache>();
            if (cache == null)
            {
                cache = new SettlementStockCache(map);
                map.components.Add(cache);
            }

            // If we already have preserved stock, don't overwrite
            if (cache.preservedStock.Count > 0)
                return;

            // Get stock from the (now destroyed) settlement's trader
            // This still works because the Settlement object exists in memory,
            // and our TryDestroyStock patch blocked stock destruction
            ThingOwner<Thing> stock = CargoVaultHelper.GetStock(factionBase);
            if (stock == null || stock.Count == 0)
                return;

            // Store original settlement ID for deterministic seeding
            cache.originalSettlementId = __state.SettlementId;

            // Transfer all items from trader stock to our cache
            cache.preservedStock.TryAddRangeOrTransfer(stock, canMergeWithExistingStacks: true);
        }
    }
}
