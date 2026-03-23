using BetterTradersGuild.Comps;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.MapComponents;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// GenStep that pre-populates the cargo vault stock for quest sites.
    ///
    /// Reads the chosen TraderKindDef from the site's WorldObjectComp_QuestVault
    /// and generates stock using QuestVaultStockGenerator. The stock is stored in
    /// SettlementStockCache.preservedStock on the site's map, which is the same
    /// MapComponent used by defeated TradersGuild settlements.
    ///
    /// This allows the existing CargoVaultHelper.GetStock() fallback path to work
    /// without modification: it tries Settlement first (returns null for Sites),
    /// then falls through to SettlementStockCache.
    ///
    /// Order 698: runs after structure generation (200) but before pawns (700).
    /// </summary>
    public class GenStep_GenerateQuestVaultStock : GenStep
    {
        public override int SeedPart => 926174038;

        public override void Generate(Map map, GenStepParams parms)
        {
            // Only runs on quest sites with WorldObjectComp_QuestVault
            var questComp = map.Parent?.GetComponent<WorldObjectComp_QuestVault>();
            if (questComp == null)
            {
                Log.Message($"[BTG] GenStep_GenerateQuestVaultStock: No WorldObjectComp_QuestVault on {map.Parent?.GetType().Name ?? "null"}");
                return;
            }
            if (!questComp.HasCargoVault)
            {
                Log.Message($"[BTG] GenStep_GenerateQuestVaultStock: HasCargoVault=false (chosenTraderKindDefName='{questComp.chosenTraderKindDefName}')");
                return;
            }

            TraderKindDef traderKind = questComp.ChosenTraderKind;
            if (traderKind == null)
            {
                Log.Warning($"[BTG] GenStep_GenerateQuestVaultStock: Could not resolve TraderKindDef '{questComp.chosenTraderKindDefName}'");
                return;
            }

            Log.Message($"[BTG] GenStep_GenerateQuestVaultStock: Generating stock for trader '{traderKind.defName}' ({traderKind.label})");

            // Get or verify the SettlementStockCache MapComponent
            var cache = map.GetComponent<SettlementStockCache>();
            if (cache == null)
            {
                Log.Error("[BTG] GenStep_GenerateQuestVaultStock: SettlementStockCache MapComponent not found on map");
                return;
            }

            // Skip if stock already populated (revisit scenario)
            if (cache.preservedStock != null && cache.preservedStock.Count > 0)
            {
                Log.Message($"[BTG] GenStep_GenerateQuestVaultStock: Stock already populated ({cache.preservedStock.Count} items), skipping");
                return;
            }

            // Generate stock from the chosen trader type
            PlanetTile tile = map.Parent?.Tile ?? default;
            Faction faction = map.Parent?.Faction;

            ThingOwner<Thing> generatedStock = QuestVaultStockGenerator.GenerateStock(
                traderKind, tile, faction, cache);

            Log.Message($"[BTG] GenStep_GenerateQuestVaultStock: Generated {generatedStock.Count} items");

            // Transfer to the cache
            cache.preservedStock.TryAddRangeOrTransfer(generatedStock, canMergeWithExistingStacks: false);

            Log.Message($"[BTG] GenStep_GenerateQuestVaultStock: Cache now has {cache.preservedStock.Count} items");

            // Set the settlement ID for deterministic seeding in cargo vault generation
            cache.originalSettlementId = map.Parent?.ID ?? map.Tile;
        }
    }
}
