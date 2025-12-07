using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Harmony prefix patch for GenStep_SettlementPawnsLoot.Generate().
    ///
    /// PURPOSE:
    /// Disables random loot scatter for TradersGuild settlements while preserving
    /// hostile pawn spawns. TradersGuild settlements use hand-crafted loot placement
    /// via prefabs, RoomContentsWorkers, and deconstructible structures instead of
    /// generic "abandoned colony" loot scatter.
    ///
    /// DESIGN RATIONALE:
    /// GenStep_SettlementPawnsLoot spawns both pawns AND loot. By setting the
    /// lootMarketValue field to zero range (0~0), we skip only the loot generation
    /// section while preserving the pawn spawning logic. Vanilla's IL code checks
    /// lootMarketValue.IsZeros and returns early if true, skipping MapGenUtility.GenerateLoot().
    ///
    /// ARCHITECTURE:
    /// Uses reflection to modify the public lootMarketValue field before vanilla
    /// reads it. This is safer than overriding MapGeneratorDef because:
    /// - No save/load compatibility issues (same generatorDef name)
    /// - Better mod compatibility (other mods see vanilla GenSteps)
    /// - Auto-updates with vanilla (no manual GenStep syncing)
    ///
    /// LEARNING NOTE:
    /// This pattern is surgical and maintainable. We modify a single field to achieve
    /// desired behavior without skipping vanilla execution or duplicating XML definitions.
    /// The vanilla code is designed to handle zero loot values (it's not a hack).
    /// </summary>
    [HarmonyPatch(typeof(GenStep_SettlementPawnsLoot))]
    [HarmonyPatch("Generate")]
    public static class GenStepSettlementPawnsLootGenerate
    {
        // Cached reflection reference to lootMarketValue field (lazy-loaded)
        private static FieldInfo lootMarketValueField = null;

        /// <summary>
        /// Prefix that overrides lootMarketValue to zero for TradersGuild settlements.
        /// Runs before vanilla Generate() executes, allowing vanilla to skip loot generation.
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(GenStep_SettlementPawnsLoot __instance, Map map)
        {
            // Only affect TradersGuild settlements (faction check)
            Settlement settlement = map?.Parent as Settlement;
            if (settlement == null || !TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                return; // Not TradersGuild, allow normal loot generation
            }

            // Lazy-load reflection (only once per game session)
            if (lootMarketValueField == null)
            {
                lootMarketValueField = typeof(GenStep_SettlementPawnsLoot).GetField(
                    "lootMarketValue",
                    BindingFlags.Public | BindingFlags.Instance
                );

                if (lootMarketValueField == null)
                {
                    Log.Error("[Better Traders Guild] Failed to find lootMarketValue field in GenStep_SettlementPawnsLoot. " +
                              "Random loot will spawn in TradersGuild settlements. This may indicate a vanilla API change.");
                    return;
                }
            }

            // Set loot market value to zero range (vanilla checks IsZeros and skips loot)
            // IMPORTANT: Must use nullable FloatRange? (not FloatRange) to match field type
            FloatRange? zeroRange = new FloatRange(0f, 0f);
            lootMarketValueField.SetValue(__instance, zeroRange);
        }
    }
}
