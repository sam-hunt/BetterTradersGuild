using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized LayoutDef references (BTG custom settlement layouts).
    /// </summary>
    [DefOf]
    public static class Layouts
    {
        /// <summary>
        /// Main TradersGuild orbital settlement layout.
        /// Named to match vanilla OrbitalSettlementPlatform convention.
        /// </summary>
        public static LayoutDef BTG_SettlementPlatform;

        /// <summary>
        /// Cargo vault layout - single-room secure storage pocket map.
        /// Used by GenStep_CargoVaultPlatform for the BTG_CargoVaultHatch pocket map.
        /// </summary>
        public static LayoutDef BTG_OrbitalCargoVault;

        static Layouts() => DefOfHelper.EnsureInitializedInCtor(typeof(Layouts));
    }
}
