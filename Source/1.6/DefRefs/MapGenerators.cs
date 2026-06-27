using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    // Centralized MapGeneratorDef references for BTG custom map generation.
    //
    // These are used by Harmony patches to swap the MapGeneratorDef for
    // TradersGuild settlements, enabling custom generation pipelines.
    [DefOf]
    public static class MapGenerators
    {
        // Custom MapGeneratorDef for TradersGuild orbital settlements.
        //
        // Includes:
        // - BTG_SettlementPlatform: GenStep_OrbitalPlatform with BTG_SettlementPlatform layout
        // - BTG_SettlementPawnsLoot: GenStep_BTGSettlementPawns (loot disabled, bounded defender lord)
        // - BTG_SettlementPostProcess: Custom GenStep for terrain/pipes/lighting
        public static MapGeneratorDef BTG_SettlementMapGenerator;

        // Custom MapGeneratorDef for the cargo vault pocket map.
        //
        // Inherits from SpaceMapGenerator to get space rendering properties.
        // Used by patches to identify BTG pocket maps for defensive world
        // rendering fixes (camera position and tile inheritance).
        public static MapGeneratorDef BTG_CargoVaultMapGenerator;

        static MapGenerators() => DefOfHelper.EnsureInitializedInCtor(typeof(MapGenerators));
    }
}
