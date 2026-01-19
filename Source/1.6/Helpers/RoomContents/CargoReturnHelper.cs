using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.RoomContents.CargoVault;
using RimWorld;
using RimWorld.Planet;
using Verse;
using PawnKinds = BetterTradersGuild.DefRefs.PawnKinds;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Helper class for returning cargo vault items to settlement trade inventory.
    /// Extracted from CompSealableSeal patch for reuse by CompRelockable and CargoVaultHatch.DeSpawn.
    /// </summary>
    public static class CargoReturnHelper
    {
        /// <summary>
        /// Returns all eligible items and pawns from a pocket map to the parent settlement's trade stock.
        /// </summary>
        /// <param name="pocketMap">The cargo vault pocket map</param>
        public static void ReturnItemsToStock(Map pocketMap)
        {
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

            Log.Message($"[BTG] CargoReturnHelper: Returned {itemsToReturn.Count} items and {pawnsToReturn.Count} pawns to stock");
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
        /// Collects all pawns from the pocket map that should be captured.
        /// Only excludes wasp drones (vault defense units that remain with the vault).
        /// All other pawns (TradersGuild staff, player colonists, animals, slaves, other factions)
        /// are captured and transferred to the settlement's trade inventory.
        /// </summary>
        private static List<Pawn> CollectEligiblePawns(Map map)
        {
            var pawns = new List<Pawn>();

            Log.Message($"[BTG DEBUG] CollectEligiblePawns: Scanning {map.mapPawns.AllPawnsSpawnedCount} pawns on map");

            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                string kindDefName = pawn.kindDef?.defName ?? "null";
                bool isWaspDrone = PawnKinds.Drone_Wasp != null && pawn.kindDef == PawnKinds.Drone_Wasp;

                Log.Message($"[BTG DEBUG] CollectEligiblePawns: Checking pawn '{pawn.LabelShort}' - Kind: {kindDefName}, IsWaspDrone: {isWaspDrone}");

                // Skip wasp drones (vault defense units - they remain part of the vault infrastructure)
                // All other pawns are captured and transferred to the settlement's trade inventory
                if (isWaspDrone)
                {
                    Log.Message($"[BTG DEBUG] CollectEligiblePawns: SKIPPING '{pawn.LabelShort}' - Wasp drone (vault defense)");
                    continue;
                }

                Log.Message($"[BTG DEBUG] CollectEligiblePawns: COLLECTING '{pawn.LabelShort}' for return to stock (captured)");
                pawns.Add(pawn);
            }

            Log.Message($"[BTG DEBUG] CollectEligiblePawns: Collected {pawns.Count} pawns total");
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
        /// <remarks>
        /// IMPORTANT: Pawns must be registered as world pawns BEFORE being added to stock.
        /// Settlement_TraderTracker.TraderTrackerTick() validates that all pawns in stock
        /// are world pawns (via WorldPawnsUtility.IsWorldPawn), removing any that aren't.
        /// Without this registration, pawns would be removed on the next tick with the error:
        /// "Faction base has non-world-pawns in its stock. Removing..."
        /// </remarks>
        private static void ReturnPawnsToStock(List<Pawn> pawns, ThingOwner<Thing> stock)
        {
            Log.Message($"[BTG DEBUG] ReturnPawnsToStock: Processing {pawns.Count} pawns, stock is {(stock != null ? "valid" : "NULL")}");

            foreach (Pawn pawn in pawns)
            {
                Log.Message($"[BTG DEBUG] ReturnPawnsToStock: Processing pawn '{pawn.LabelShort}' (ThingID: {pawn.ThingID})");
                Log.Message($"[BTG DEBUG]   - Spawned: {pawn.Spawned}, Destroyed: {pawn.Destroyed}, Discarded: {pawn.Discarded}");
                Log.Message($"[BTG DEBUG]   - IsWorldPawn before: {Find.WorldPawns.Contains(pawn)}");

                // Despawn from map
                if (pawn.Spawned)
                {
                    Log.Message($"[BTG DEBUG]   - Despawning from map...");
                    pawn.DeSpawn(DestroyMode.Vanish);
                    Log.Message($"[BTG DEBUG]   - After despawn - Spawned: {pawn.Spawned}, Destroyed: {pawn.Destroyed}");
                }

                if (stock != null)
                {
                    // Register as world pawn BEFORE adding to stock.
                    // KeepForever ensures the pawn persists (not garbage collected)
                    // since it's actively in trade inventory and could be purchased.
                    Log.Message($"[BTG DEBUG]   - Registering as world pawn (KeepForever)...");
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                    Log.Message($"[BTG DEBUG]   - IsWorldPawn after PassToWorld: {Find.WorldPawns.Contains(pawn)}");

                    // Return to stock
                    Log.Message($"[BTG DEBUG]   - Adding to stock (current stock count: {stock.Count})...");
                    bool addResult = stock.TryAdd(pawn, canMergeWithExistingStacks: false);
                    Log.Message($"[BTG DEBUG]   - TryAdd result: {addResult}, new stock count: {stock.Count}");

                    if (!addResult)
                    {
                        Log.Warning($"[BTG DEBUG] ReturnPawnsToStock: FAILED to add pawn '{pawn.LabelShort}' to stock!");
                        Log.Warning($"[BTG DEBUG]   - Pawn state: Destroyed={pawn.Destroyed}, Discarded={pawn.Discarded}, holdingOwner={pawn.holdingOwner?.GetType().Name ?? "null"}");
                    }
                }
                else
                {
                    // Safety fallback: pass to world (never lose pawns)
                    Log.Message($"[BTG DEBUG]   - Stock is null, passing to world with Decide mode");
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                }
            }

            // Log final stock state for pawns
            if (stock != null)
            {
                int pawnCount = stock.OfType<Pawn>().Count();
                Log.Message($"[BTG DEBUG] ReturnPawnsToStock: Complete. Stock now has {pawnCount} pawns out of {stock.Count} total items");
                foreach (Pawn p in stock.OfType<Pawn>())
                {
                    Log.Message($"[BTG DEBUG]   - Pawn in stock: '{p.LabelShort}' (ThingID: {p.ThingID})");
                }
            }
        }
    }
}
