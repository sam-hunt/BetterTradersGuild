using BetterTradersGuild.Comps;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.MapComponents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that pre-populates the cargo vault stock for quest sites.
    //
    // Reads the chosen TraderKindDef from the site's WorldObjectComp_QuestVault
    // and generates stock using QuestVaultStockGenerator. The stock is stored in
    // SettlementStockCache.preservedStock on the site's map, which is the same
    // MapComponent used by defeated TradersGuild settlements.
    //
    // This allows the existing CargoVaultHelper.GetStock() fallback path to work
    // without modification: it tries Settlement first (returns null for Sites),
    // then falls through to SettlementStockCache.
    //
    // Order 698: runs after structure generation (200) but before pawns (700).
    public class GenStep_GenerateQuestVaultStock : GenStep
    {
        public override int SeedPart => 926174038;

        public override void Generate(Map map, GenStepParams parms)
        {
            // Only runs on quest sites with WorldObjectComp_QuestVault;
            // sealed vault (goodwill reward chosen) gets no stock.
            var questComp = map.Parent?.GetComponent<WorldObjectComp_QuestVault>();
            if (questComp?.HasCargoVault != true)
                return;

            TraderKindDef traderKind = questComp.ChosenTraderKind;
            if (traderKind == null)
            {
                Log.Warning($"[Better Traders Guild] GenStep_GenerateQuestVaultStock: Could not resolve TraderKindDef '{questComp.chosenTraderKindDefName}'");
                return;
            }

            var cache = map.GetComponent<SettlementStockCache>();
            if (cache == null)
            {
                Log.Error("[Better Traders Guild] GenStep_GenerateQuestVaultStock: SettlementStockCache MapComponent not found on map");
                return;
            }

            // Skip if stock already populated (belt-and-braces; the site is normally
            // destroyed with its map, so a second generation shouldn't occur)
            if (cache.preservedStock?.Count > 0)
                return;

            // Generate as the Traders Guild, not the site's Salvagers faction: the reward
            // choice filtered slave-ship traders against TG ideology, and per quest lore the
            // Guild contact stocked the vault. Fall back to the site faction if TG is absent.
            Faction makingFaction = Find.FactionManager.FirstFactionOfDef(Factions.TradersGuild)
                ?? map.Parent?.Faction;

            ThingOwner<Thing> generatedStock = QuestVaultStockGenerator.GenerateStock(
                traderKind, map.Tile, makingFaction, cache);

            cache.preservedStock.TryAddRangeOrTransfer(generatedStock, canMergeWithExistingStacks: false);

            // Set the settlement ID for deterministic seeding in cargo vault generation
            cache.originalSettlementId = map.Parent?.ID ?? map.Tile;
        }
    }
}
