using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized FactionDef references.
    /// </summary>
    [DefOf]
    public static class Factions
    {
        // === ODYSSEY (always available - BTG hard-depends on Odyssey) ===
        public static FactionDef TradersGuild;
        public static FactionDef Salvagers;

        static Factions() => DefOfHelper.EnsureInitializedInCtor(typeof(Factions));
    }
}
