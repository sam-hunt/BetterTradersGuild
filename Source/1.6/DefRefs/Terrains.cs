using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized TerrainDef references.
    /// </summary>
    [DefOf]
    public static class Terrains
    {
        // === VANILLA ===
        public static TerrainDef AncientTile;
        public static TerrainDef MetalTile;

        // Carpets - Pastel colors (for nursery and decorative flooring)
        public static TerrainDef CarpetPink;
        public static TerrainDef CarpetBluePastel;
        public static TerrainDef CarpetGreenPastel;

        // Carpets - Neutral/muted colors (for crew quarters subrooms)
        public static TerrainDef CarpetGranite;
        public static TerrainDef CarpetMarble;
        public static TerrainDef CarpetSlate;
        public static TerrainDef CarpetSandstone;
        public static TerrainDef CarpetBlack;
        public static TerrainDef CarpetBlueSubtle;
        public static TerrainDef CarpetPurpleSubtle;
        public static TerrainDef CarpetGreyDark;
        public static TerrainDef CarpetGreenFaded;

        // === ODYSSEY (always available - BTG hard-depends on Odyssey) ===
        public static TerrainDef OrbitalPlatform;

        static Terrains() => DefOfHelper.EnsureInitializedInCtor(typeof(Terrains));
    }
}
