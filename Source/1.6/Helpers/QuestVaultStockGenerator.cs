using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Generates trade stock from a TraderKindDef without requiring a Settlement_TraderTracker.
    ///
    /// Used by GenStep_GenerateQuestVaultStock to populate the cargo vault on quest sites.
    /// Replicates the stock generation logic from Settlement_TraderTracker.RegenerateStock()
    /// by directly iterating the TraderKindDef's stockGenerators.
    /// </summary>
    public static class QuestVaultStockGenerator
    {
        /// <summary>
        /// Generates a complete set of trade stock for a given TraderKindDef.
        /// </summary>
        /// <param name="traderKind">The trader type whose stock generators to use</param>
        /// <param name="forTile">World tile for biome/region context</param>
        /// <param name="faction">Faction context for ideology filtering (can be null)</param>
        /// <param name="holder">IThingHolder to own the generated ThingOwner</param>
        /// <returns>ThingOwner containing all generated stock items</returns>
        public static ThingOwner<Thing> GenerateStock(
            TraderKindDef traderKind,
            PlanetTile forTile,
            Faction faction,
            IThingHolder holder)
        {
            var stock = new ThingOwner<Thing>(holder);

            if (traderKind?.stockGenerators == null)
            {
                Log.Warning("[BTG] QuestVaultStockGenerator: TraderKindDef or stockGenerators is null");
                return stock;
            }

            foreach (StockGenerator generator in traderKind.stockGenerators)
            {
                IEnumerable<Thing> generated = generator.GenerateThings(forTile, faction);
                foreach (Thing thing in generated)
                {
                    // Register pawns as world pawns before adding to stock.
                    // Settlement_TraderTracker.TraderTrackerTick() validates that all pawns
                    // in stock are world pawns, removing any that aren't.
                    // Same pattern as CargoReturnHelper.ReturnPawnsToStock.
                    if (thing is Pawn pawn)
                    {
                        if (!pawn.Dead)
                        {
                            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                        }
                    }

                    stock.TryAdd(thing, canMergeWithExistingStacks: false);
                }
            }

            return stock;
        }
    }
}
