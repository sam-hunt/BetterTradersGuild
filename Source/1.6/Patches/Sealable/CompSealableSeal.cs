using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.MapGeneration;
using BetterTradersGuild.RoomContents.CargoVault;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.SealablePatches
{
    /// <summary>
    /// Harmony patch: CompSealable.Seal method
    /// Returns all eligible items in the cargo vault to the settlement's trade inventory.
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: When a portal is sealed, this runs BEFORE the pocket map is destroyed.
    /// We use this window to collect all items and return them to the settlement's stock.
    ///
    /// Exclusions:
    /// - Unminified buildings (can't be traded anyway)
    /// - Pawns belonging to TradersGuild faction (settlement defenders, not cargo)
    /// </remarks>
    [HarmonyPatch(typeof(CompSealable), nameof(CompSealable.Seal))]
    public static class CompSealableSeal
    {
        /// <summary>
        /// Prefix method - collects and returns cargo before portal is sealed.
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(CompSealable __instance)
        {
            // Check if this is our BTG_CargoVaultHatch
            if (__instance.parent?.def != Things.BTG_CargoVaultHatch)
                return;

            // Get the MapPortal component
            if (!(__instance.parent is MapPortal portal))
                return;

            // Get the pocket map
            Map pocketMap = portal.PocketMap;
            if (pocketMap == null)
                return;

            // Navigate to parent settlement
            Settlement settlement = CargoVaultHelper.GetParentSettlement(pocketMap);

            // Get stock (may be null if settlement is gone)
            ThingOwner<Thing> stock = settlement != null
                ? CargoVaultHelper.GetStock(settlement)
                : null;

            // Collect all eligible items and pawns from the pocket map
            List<Thing> itemsToReturn = CollectEligibleItems(pocketMap);
            List<Pawn> pawnsToReturn = CollectEligiblePawns(pocketMap);

            // Return items to stock (or destroy if no stock)
            ReturnItemsToStock(itemsToReturn, stock);
            ReturnPawnsToStock(pawnsToReturn, stock);
        }

        /// <summary>
        /// Collects all haulable items from the pocket map.
        /// Excludes unminified buildings.
        /// </summary>
        private static List<Thing> CollectEligibleItems(Map map)
        {
            var items = new List<Thing>();

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver))
            {
                // Skip pawns (handled separately)
                if (thing is Pawn)
                    continue;

                // Skip unminified buildings (they stay in the vault)
                if (thing is Building && thing.def.Minifiable != true)
                    continue;

                items.Add(thing);
            }

            return items;
        }

        /// <summary>
        /// Collects all pawns from the pocket map that should be returned.
        /// Excludes pawns belonging to the TradersGuild faction.
        /// </summary>
        private static List<Pawn> CollectEligiblePawns(Map map)
        {
            var pawns = new List<Pawn>();

            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                // Skip TradersGuild faction pawns (defenders, not cargo)
                if (pawn.Faction?.def == Factions.TradersGuild)
                    continue;

                // Skip player faction pawns (they should leave normally)
                if (pawn.Faction == Faction.OfPlayer)
                    continue;

                pawns.Add(pawn);
            }

            return pawns;
        }

        /// <summary>
        /// Returns items to the settlement's trade stock.
        /// If stock is null, items are destroyed (lost, but this is a safety fallback).
        /// </summary>
        private static void ReturnItemsToStock(List<Thing> items, ThingOwner<Thing> stock)
        {
            foreach (Thing item in items)
            {
                // Despawn from map
                if (item.Spawned)
                {
                    item.DeSpawn(DestroyMode.Vanish);
                }

                if (stock != null)
                {
                    // Return to stock
                    stock.TryAdd(item, canMergeWithExistingStacks: true);
                }
                else
                {
                    // Safety fallback: destroy the item
                    // Items are replaceable; this only happens if settlement is gone
                    item.Destroy(DestroyMode.Vanish);
                }
            }
        }

        /// <summary>
        /// Returns pawns to the settlement's trade stock.
        /// If stock is null, pawns are passed to world (never destroyed).
        /// </summary>
        private static void ReturnPawnsToStock(List<Pawn> pawns, ThingOwner<Thing> stock)
        {
            foreach (Pawn pawn in pawns)
            {
                // Despawn from map
                if (pawn.Spawned)
                {
                    pawn.DeSpawn(DestroyMode.Vanish);
                }

                if (stock != null)
                {
                    // Return to stock
                    stock.TryAdd(pawn, canMergeWithExistingStacks: false);
                }
                else
                {
                    // Safety fallback: pass to world (never lose pawns)
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                }
            }
        }
    }
}
