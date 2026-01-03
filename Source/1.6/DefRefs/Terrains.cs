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

        // === ODYSSEY (always available - BTG hard-depends on Odyssey) ===
        public static TerrainDef OrbitalPlatform;

        static Terrains() => DefOfHelper.EnsureInitializedInCtor(typeof(Terrains));
    }
}
