using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    // Centralized LayoutDef references (BTG custom settlement layouts).
    [DefOf]
    public static class Layouts
    {
        // Main TradersGuild orbital settlement layout.
        // Named to match vanilla OrbitalSettlementPlatform convention.
        public static LayoutDef BTG_SettlementPlatform;

        // Cargo vault layout - single-room secure storage pocket map.
        // Used by GenStep_CargoVaultPlatform for the BTG_CargoVaultHatch pocket map.
        public static LayoutDef BTG_OrbitalCargoVault;

        static Layouts() => DefOfHelper.EnsureInitializedInCtor(typeof(Layouts));
    }
}
