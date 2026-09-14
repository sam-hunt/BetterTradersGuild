using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    // Handles selection of cargo items from trade inventory.
    // Moves items out of stock into the vault, honouring the two spawn caps
    // (max stacks, max wealth) from mod settings. Whatever the caps exclude is simply
    // left in the stock ThingOwner, so it stays caravan-tradeable and is never
    // duplicated: relock returns spawned things to the same owner, and the next hack
    // reselects from the merged remainder.
    public static class CargoSelector
    {
        // Selects cargo from stock within the given caps and REMOVES it from stock.
        // stock: The trade inventory to select from
        // settlementID: seeds the within-type pick so the same stock yields the same
        //   subset on every hack (relock and re-hack shows the same vault)
        // stackCap / wealthCap: CargoLimitAllocator.Unlimited (negative) for no cap
        // Returns: List of Things removed from stock
        //
        // Pawns (slaves, animals) always spawn: they are the interesting part of a
        // slaver's stock and there are never enough of them to matter for the crash
        // the caps guard against. They still count toward both caps, so the item
        // budgets are whatever remains after the pawns are reserved.
        public static List<Thing> SelectCargo(ThingOwner<Thing> stock, int settlementID, int stackCap, float wealthCap)
        {
            var selected = new List<Thing>();

            if (stock == null || stock.Count == 0)
            {
                return selected;
            }

            bool limited = CargoLimitAllocator.IsLimited(stackCap) || CargoLimitAllocator.IsLimited(wealthCap);
            List<Thing> toTake = limited
                ? ChooseWithinCaps(stock, settlementID, stackCap, wealthCap)
                : stock.ToList();

            // SplitOff of the full stack detaches the Thing from its holder (a move,
            // never a copy), which is what keeps the stock/vault split duplication-free.
            foreach (Thing item in toTake)
            {
                Thing taken = item.SplitOff(item.stackCount);
                selected.Add(taken);
            }

            return selected;
        }

        // Picks which stock entries spawn: every pawn, then a per-ThingDef quota of
        // items from CargoLimitAllocator so each type keeps its share of the inventory.
        private static List<Thing> ChooseWithinCaps(ThingOwner<Thing> stock, int settlementID, int stackCap, float wealthCap)
        {
            var chosen = new List<Thing>();
            int pawnStacks = 0;
            float pawnWealth = 0f;
            var groups = new Dictionary<ThingDef, List<Thing>>();
            var groupOrder = new List<ThingDef>();

            foreach (Thing thing in stock)
            {
                if (thing is Pawn)
                {
                    chosen.Add(thing);
                    pawnStacks++;
                    pawnWealth += thing.MarketValue;
                    continue;
                }

                if (!groups.TryGetValue(thing.def, out List<Thing> group))
                {
                    group = new List<Thing>();
                    groups[thing.def] = group;
                    groupOrder.Add(thing.def);
                }
                group.Add(thing);
            }

            if (groupOrder.Count == 0)
                return chosen;

            // Stable group order (defName) so the allocator's index tie-breaks do not
            // depend on stock enumeration order.
            groupOrder.Sort((a, b) => string.CompareOrdinal(a.defName, b.defName));

            var groupStacks = new int[groupOrder.Count];
            var groupWealth = new float[groupOrder.Count];
            int totalStacks = pawnStacks;
            float totalWealth = pawnWealth;
            for (int g = 0; g < groupOrder.Count; g++)
            {
                foreach (Thing thing in groups[groupOrder[g]])
                {
                    groupStacks[g] += StacksOf(thing);
                    groupWealth[g] += WealthOf(thing);
                }
                totalStacks += groupStacks[g];
                totalWealth += groupWealth[g];
            }

            int[] quota = CargoLimitAllocator.Allot(groupStacks, groupWealth,
                CargoLimitAllocator.Remaining(stackCap, pawnStacks),
                CargoLimitAllocator.Remaining(wealthCap, pawnWealth));

            int chosenStacks = pawnStacks;
            float chosenWealth = pawnWealth;
            for (int g = 0; g < groupOrder.Count; g++)
            {
                int remaining = quota[g];
                if (remaining <= 0)
                    continue;

                // Deterministic pseudo-random order keyed on the thing's persistent ID
                // and the settlement, so relock + re-hack of untouched stock picks the
                // same entries regardless of how the ThingOwner got reordered.
                List<Thing> group = groups[groupOrder[g]];
                group.Sort((a, b) => PickKey(a, settlementID).CompareTo(PickKey(b, settlementID)));

                foreach (Thing thing in group)
                {
                    int stacks = StacksOf(thing);
                    if (stacks > remaining)
                        continue;
                    chosen.Add(thing);
                    remaining -= stacks;
                    chosenStacks += stacks;
                    chosenWealth += WealthOf(thing);
                    if (remaining <= 0)
                        break;
                }
            }

            if (Prefs.DevMode && chosen.Count < stock.Count)
            {
                Log.Message($"[Better Traders Guild] Cargo vault caps trimmed the spawn to {chosenStacks}/{totalStacks} stacks, " +
                    $"{chosenWealth:F0}/{totalWealth:F0} silver; the rest stays in the trade inventory.");
            }

            return chosen;
        }

        // Number of floor/shelf stacks a stock entry becomes once spawned (CargoSpawner
        // splits anything over stackLimit). Stock entries are normally already at or
        // under stackLimit, so this is usually 1.
        private static int StacksOf(Thing thing)
        {
            int limit = Mathf.Max(1, thing.def.stackLimit);
            return Mathf.Max(1, Mathf.CeilToInt(thing.stackCount / (float)limit));
        }

        private static float WealthOf(Thing thing)
        {
            return thing.MarketValue * thing.stackCount;
        }

        private static int PickKey(Thing thing, int settlementID)
        {
            unchecked
            {
                int hash = Gen.HashCombineInt(thing.thingIDNumber, settlementID);
                hash ^= hash >> 16;
                hash *= (int)0x85ebca6b;
                hash ^= hash >> 13;
                hash *= (int)0xc2b2ae35;
                hash ^= hash >> 16;
                return hash;
            }
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
