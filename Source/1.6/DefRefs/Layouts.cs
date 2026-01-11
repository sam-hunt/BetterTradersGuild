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
        /// </summary>
        public static LayoutDef BTG_Settlement;

        /// <summary>
        /// Cargo hold vault layout - single-room secure storage pocket map.
        /// Used by GenStep_CargoHoldVault for the BTG_CargoHoldHatch pocket map.
        /// </summary>
        public static LayoutDef BTG_CargoHoldVaultLayout;

        static Layouts() => DefOfHelper.EnsureInitializedInCtor(typeof(Layouts));
    }
}
