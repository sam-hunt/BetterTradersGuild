using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    // Handles selection of cargo items from trade inventory.
    // Transfers ALL items from stock to the cargo vault.
    public static class CargoSelector
    {
        // Selects ALL cargo from stock.
        // All items are REMOVED from the source stock and returned.
        // stock: The trade inventory to select from
        // Returns: List of Things removed from stock
        public static List<Thing> SelectCargo(ThingOwner<Thing> stock)
        {
            var selected = new List<Thing>();

            if (stock == null || stock.Count == 0)
            {
                return selected;
            }

            // Take ALL items from stock (simple transfer)
            // Iterate over copy since we're modifying the collection
            foreach (Thing item in stock.ToList())
            {
                Thing taken = item.SplitOff(item.stackCount);
                selected.Add(taken);
            }

            return selected;
        }

        // Categorizes selected cargo into items and pawns for different spawn handling.
        // Pawns spawn on floor; items try shelves first, then floor.
        // Filters out corrupt MinifiedThings (null InnerThing) to prevent render crashes.
        // cargo: All selected cargo
        // items: Output: Non-pawn items
        // pawns: Output: Pawns (slaves from pirate merchants)
        public static void CategorizeItems(
            List<Thing> cargo,
            out List<Thing> items,
            out List<Pawn> pawns)
        {
            items = new List<Thing>();
            pawns = new List<Pawn>();

            foreach (Thing thing in cargo)
            {
                if (thing is Pawn pawn)
                {
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
                    items.Add(thing);
                }
                else
                {
                    items.Add(thing);
                }
            }
        }
    }
}
