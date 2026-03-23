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
        /// - BTG_SettlementPlatform: GenStep_OrbitalPlatform with BTG_SettlementPlatform layout
        /// - BTG_SettlementPawnsLoot: GenStep_SettlementPawnsLoot with loot disabled
        /// - BTG_SettlementPostProcess: Custom GenStep for terrain/pipes/lighting
        /// </summary>
        public static MapGeneratorDef BTG_SettlementMapGenerator;

        /// <summary>
        /// Custom MapGeneratorDef for the cargo vault pocket map.
        ///
        /// Inherits from SpaceMapGenerator to get space rendering properties.
        /// Used by patches to identify BTG pocket maps for defensive world
        /// rendering fixes (camera position and tile inheritance).
        /// </summary>
        public static MapGeneratorDef BTG_CargoVaultMapGenerator;

        /// <summary>
        /// Custom MapGeneratorDef for smuggler's den quest sites.
        ///
        /// Forked from BTG_SettlementMapGenerator with:
        /// - BTG_SmugglersDenPlatform layout (no nursery/classroom, required armory)
        /// - BTG_GenerateQuestVaultStock step for vault stock pre-population
        /// </summary>
        public static MapGeneratorDef BTG_SmugglersDenMapGenerator;

        static MapGenerators() => DefOfHelper.EnsureInitializedInCtor(typeof(MapGenerators));
    }
}
