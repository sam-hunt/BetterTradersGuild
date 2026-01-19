using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    /// <summary>
    /// Handles selection of cargo items from trade inventory.
    /// Transfers ALL items from stock to the cargo vault.
    /// </summary>
    public static class CargoSelector
    {
        /// <summary>
        /// Selects ALL cargo from stock.
        /// All items are REMOVED from the source stock and returned.
        /// </summary>
        /// <param name="stock">The trade inventory to select from</param>
        /// <returns>List of Things removed from stock</returns>
        public static List<Thing> SelectCargo(ThingOwner<Thing> stock)
        {
            var selected = new List<Thing>();

            if (stock == null || stock.Count == 0)
            {
                Log.Message($"[BTG DEBUG] SelectCargo: Stock is {(stock == null ? "null" : "empty")}, returning empty list");
                return selected;
            }

            // Log initial stock state, especially pawns
            int initialPawnCount = stock.OfType<Pawn>().Count();
            Log.Message($"[BTG DEBUG] SelectCargo: Starting with {stock.Count} items in stock ({initialPawnCount} pawns)");
            foreach (Pawn p in stock.OfType<Pawn>())
            {
                Log.Message($"[BTG DEBUG] SelectCargo: Found pawn in stock: '{p.LabelShort}' (ThingID: {p.ThingID}, Faction: {p.Faction?.Name ?? "null"})");
                Log.Message($"[BTG DEBUG]   - IsWorldPawn: {Find.WorldPawns.Contains(p)}, Destroyed: {p.Destroyed}, Discarded: {p.Discarded}");
            }

            // Take ALL items from stock (simple transfer)
            // Iterate over copy since we're modifying the collection
            foreach (Thing item in stock.ToList())
            {
                bool isPawn = item is Pawn;
                bool isMinified = item is MinifiedThing;

                if (isPawn)
                {
                    Pawn pawn = item as Pawn;
                    Log.Message($"[BTG DEBUG] SelectCargo: Processing PAWN '{pawn.LabelShort}' (ThingID: {pawn.ThingID})");
                    Log.Message($"[BTG DEBUG]   - Before SplitOff: stackCount={item.stackCount}, holdingOwner={item.holdingOwner?.GetType().Name ?? "null"}");
                }

                // Check MinifiedThing BEFORE SplitOff to see if corruption exists in stock
                if (isMinified)
                {
                    MinifiedThing minifiedBefore = item as MinifiedThing;
                    string innerDefBefore = minifiedBefore.InnerThing?.def?.defName ?? "NULL";
                    string innerSource = minifiedBefore.InnerThing?.def?.modContentPack?.Name ?? "Unknown";
                    Log.Message($"[BTG DEBUG] SelectCargo: Processing MinifiedThing '{item.ThingID}' - InnerThing BEFORE SplitOff: {innerDefBefore} (from: {innerSource})");
                }

                Thing taken = item.SplitOff(item.stackCount);

                if (isPawn)
                {
                    Pawn takenPawn = taken as Pawn;
                    Log.Message($"[BTG DEBUG]   - After SplitOff: taken={taken?.LabelShort ?? "null"}, same object={ReferenceEquals(item, taken)}");
                    Log.Message($"[BTG DEBUG]   - Taken pawn state: Destroyed={takenPawn?.Destroyed}, Discarded={takenPawn?.Discarded}, holdingOwner={takenPawn?.holdingOwner?.GetType().Name ?? "null"}");
                }

                // Check MinifiedThing AFTER SplitOff to see if SplitOff causes corruption
                if (isMinified)
                {
                    MinifiedThing minifiedAfter = taken as MinifiedThing;
                    string innerDefAfter = minifiedAfter?.InnerThing?.def?.defName ?? "NULL";
                    string innerSourceAfter = minifiedAfter?.InnerThing?.def?.modContentPack?.Name ?? "Unknown";
                    bool sameObject = ReferenceEquals(item, taken);
                    Log.Message($"[BTG DEBUG]   - After SplitOff: InnerThing={innerDefAfter} (from: {innerSourceAfter}), sameObject={sameObject}");
                }

                selected.Add(taken);
            }

            // Log final state
            int finalPawnCount = selected.OfType<Pawn>().Count();
            Log.Message($"[BTG DEBUG] SelectCargo: Transferred {selected.Count} items from stock ({finalPawnCount} pawns)");
            Log.Message($"[BTG DEBUG] SelectCargo: Stock now has {stock.Count} items remaining");

            return selected;
        }

        /// <summary>
        /// Categorizes selected cargo into items and pawns for different spawn handling.
        /// Pawns spawn on floor; items try shelves first, then floor.
        /// Filters out corrupt MinifiedThings (null InnerThing) to prevent render crashes.
        /// </summary>
        /// <param name="cargo">All selected cargo</param>
        /// <param name="items">Output: Non-pawn items</param>
        /// <param name="pawns">Output: Pawns (slaves from pirate merchants)</param>
        public static void CategorizeItems(
            List<Thing> cargo,
            out List<Thing> items,
            out List<Pawn> pawns)
        {
            items = new List<Thing>();
            pawns = new List<Pawn>();

            Log.Message($"[BTG DEBUG] CategorizeItems: Processing {cargo.Count} cargo items");

            foreach (Thing thing in cargo)
            {
                if (thing is Pawn pawn)
                {
                    Log.Message($"[BTG DEBUG] CategorizeItems: Found PAWN '{pawn.LabelShort}' (ThingID: {pawn.ThingID})");
                    Log.Message($"[BTG DEBUG]   - Faction: {pawn.Faction?.Name ?? "null"}, IsWorldPawn: {Find.WorldPawns.Contains(pawn)}");
                    Log.Message($"[BTG DEBUG]   - Destroyed: {pawn.Destroyed}, Discarded: {pawn.Discarded}, Spawned: {pawn.Spawned}");
                    pawns.Add(pawn);
                }
                else if (thing is MinifiedThing minified)
                {
                    // Validate MinifiedThing has valid InnerThing - corrupt ones crash during rendering
                    if (minified.InnerThing == null)
                    {
                        // Log detailed diagnostic info to help track down the source
                        string defSource = minified.def?.modContentPack?.Name ?? "Unknown";
                        string stuffDef = minified.Stuff?.defName ?? "null";
                        string stuffSource = minified.Stuff?.modContentPack?.Name ?? "Unknown";
                        Log.Error($"[Better Traders Guild] MinifiedThing with null InnerThing - item skipped to prevent crash.\n" +
                            $"  ThingID: {minified.ThingID}\n" +
                            $"  Def: {minified.def?.defName ?? "null"} (from: {defSource})\n" +
                            $"  Stuff: {stuffDef} (from: {stuffSource})\n" +
                            $"  Label: {minified.Label ?? "null"}\n" +
                            $"  stackCount: {minified.stackCount}, stackLimit: {minified.def?.stackLimit ?? -1}\n" +
                            $"  Spawned: {minified.Spawned}, Destroyed: {minified.Destroyed}\n" +
                            $"  holdingOwner: {minified.holdingOwner?.GetType().Name ?? "null"}\n" +
                            $"  Please report this at: https://github.com/sam-hunt/BetterTradersGuild/issues\n" +
                            $"  Include your mod list and Player.log file.");
                        // Destroy to prevent memory leak
                        minified.Destroy(DestroyMode.Vanish);
                        continue;
                    }
                    Log.Message($"[BTG DEBUG] CategorizeItems: MinifiedThing '{minified.Label}' contains {minified.InnerThing.def?.defName ?? "null"} (from: {minified.InnerThing.def?.modContentPack?.Name ?? "Unknown"})");
                    items.Add(thing);
                }
                else
                {
                    items.Add(thing);
                }
            }

            Log.Message($"[BTG DEBUG] CategorizeItems: Result - {items.Count} items, {pawns.Count} pawns");
        }
    }
}
