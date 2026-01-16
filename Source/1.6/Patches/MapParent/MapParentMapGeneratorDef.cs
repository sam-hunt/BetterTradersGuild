using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.MapParentPatches
{
    /// <summary>
    /// Harmony postfix patch for MapParent.MapGeneratorDef property getter to use
    /// custom MapGeneratorDef for TradersGuild settlements.
    ///
    /// PURPOSE:
    /// Intercepts MapGeneratorDef selection at the highest level, allowing BTG to
    /// use a completely custom generation pipeline for TradersGuild settlements.
    /// This enables declarative GenStep configuration via XML without needing
    /// runtime reflection hacks.
    ///
    /// ARCHITECTURE:
    /// This is the idiomatic way to customize map generation for specific factions:
    /// - MapParent.MapGeneratorDef property is called when a map is being generated
    /// - We postfix to swap the result for TradersGuild settlements
    /// - The custom MapGeneratorDef (BTG_SettlementMapGenerator) includes:
    ///   - BTG_SettlementPlatform: GenStep_OrbitalPlatform with BTG_SettlementPlatform layout
    ///   - BTG_SettlementPawnsLoot: GenStep_SettlementPawnsLoot with loot disabled
    ///   - BTG_SettlementPostProcess: Custom GenStep for terrain/pipes/lighting
    ///
    /// WHY THIS APPROACH:
    /// - No reflection needed to swap layoutDef or lootMarketValue
    /// - Entire pipeline is declarative in XML
    /// - Clean separation between vanilla and BTG generation
    /// - Better mod compatibility (other mods can see/patch our GenSteps)
    /// - Explicit GenStep ordering via XML
    ///
    /// LEARNING NOTE (Property Patching):
    /// To patch a property getter in Harmony, use the property name (not "get_PropertyName").
    /// Harmony automatically resolves the getter method from the property name.
    ///
    /// IMPORTANT: Settlement OVERRIDES MapParent.MapGeneratorDef, so we must patch
    /// Settlement directly, not MapParent. Otherwise our patch never gets called.
    /// </summary>
    [HarmonyPatch(typeof(Settlement))]
    [HarmonyPatch(nameof(Settlement.MapGeneratorDef), MethodType.Getter)]
    public static class MapParentMapGeneratorDef
    {
        /// <summary>
        /// Postfix that overrides MapGeneratorDef for TradersGuild settlements.
        ///
        /// EXECUTION FLOW:
        /// 1. Check if custom layouts feature enabled in mod settings
        /// 2. Check if this is a TradersGuild settlement
        /// 3. If yes: Override result with BTG_SettlementMapGenerator
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Settlement __instance, ref MapGeneratorDef __result)
        {
            // Check if custom layouts feature enabled
            if (!BetterTradersGuildMod.Settings.useCustomLayouts)
                return;

            // Check if this is a TradersGuild settlement
            if (!TradersGuildHelper.IsTradersGuildSettlement(__instance))
                return;

            // Check custom MapGeneratorDef is available
            if (MapGenerators.BTG_SettlementMapGenerator == null)
                return;

            // Override result with custom MapGeneratorDef
            __result = MapGenerators.BTG_SettlementMapGenerator;
        }
    }
}
