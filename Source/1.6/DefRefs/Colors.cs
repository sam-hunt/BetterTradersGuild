using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized ColorDef references (BTG custom colors and vanilla structure colors).
    /// </summary>
    [DefOf]
    public static class Colors
    {
        // === BTG CUSTOM ===
        public static ColorDef BTG_OrbitalSteel;

        // === VANILLA STRUCTURE COLORS ===
        public static ColorDef Structure_Marble;
        public static ColorDef Structure_Pink;
        public static ColorDef Structure_BluePastel;
        public static ColorDef Structure_GreenPastel;

        static Colors() => DefOfHelper.EnsureInitializedInCtor(typeof(Colors));
    }
}
