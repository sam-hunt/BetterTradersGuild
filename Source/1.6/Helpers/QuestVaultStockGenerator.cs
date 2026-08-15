using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Helpers
{
    // Generates trade stock from a TraderKindDef without requiring a Settlement_TraderTracker.
    //
    // Used by GenStep_GenerateQuestVaultStock to populate the cargo vault on quest sites.
    // Mirrors Settlement_TraderTracker.RegenerateStock() by generating through the
    // TraderStock ThingSetMaker, which applies the tradeability filter and calls
    // PostGeneratedForTrader on each thing - a hand-rolled stockGenerators loop misses both.
    public static class QuestVaultStockGenerator
    {
        // Generates a complete set of trade stock for a given TraderKindDef.
        // makingFaction: faction context for stock generation (e.g. slave generation);
        // pass the faction whose ideology the trader selection was filtered against.
        public static ThingOwner<Thing> GenerateStock(
            TraderKindDef traderKind,
            PlanetTile forTile,
            Faction makingFaction,
            IThingHolder holder)
        {
            var stock = new ThingOwner<Thing>(holder);

            if (traderKind == null)
            {
                Log.Warning("[Better Traders Guild] QuestVaultStockGenerator: TraderKindDef is null");
                return stock;
            }

            ThingSetMakerParams parms = new ThingSetMakerParams
            {
                traderDef = traderKind,
                tile = forTile,
                makingFaction = makingFaction
            };

            foreach (Thing thing in ThingSetMakerDefOf.TraderStock.root.Generate(parms))
            {
                // Register pawns as world pawns before adding to stock.
                // Settlement_TraderTracker.TraderTrackerTick() validates that all pawns
                // in stock are world pawns, removing any that aren't.
                // Same pattern as CargoReturnHelper.ReturnPawnsToStock.
                if (thing is Pawn pawn && !pawn.Dead)
                {
                    WorldPawnRegistrar.EnsureWorldPawn(pawn, PawnDiscardDecideMode.KeepForever);
                }

                stock.TryAdd(thing, canMergeWithExistingStacks: false);
            }

            return stock;
        }
    }
}
