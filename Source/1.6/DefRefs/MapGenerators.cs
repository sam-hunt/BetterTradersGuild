using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized MapGeneratorDef references for BTG custom map generation.
    ///
    /// These are used by Harmony patches to swap the MapGeneratorDef for
    /// TradersGuild settlements, enabling custom generation pipelines.
    /// </summary>
    [DefOf]
    public static class MapGenerators
    {
        /// <summary>
        /// Custom MapGeneratorDef for TradersGuild orbital settlements.
        ///
        /// Includes:
        /// - BTG_SettlementPlatform: GenStep_OrbitalPlatform with BTG_Settlement layout
        /// - BTG_SettlementPawnsLoot: GenStep_SettlementPawnsLoot with loot disabled
        /// - BTG_SettlementPostProcess: Custom GenStep for terrain/pipes/lighting
        /// </summary>
        public static MapGeneratorDef BTG_SettlementMapGenerator;

        static MapGenerators() => DefOfHelper.EnsureInitializedInCtor(typeof(MapGenerators));
    }
}
