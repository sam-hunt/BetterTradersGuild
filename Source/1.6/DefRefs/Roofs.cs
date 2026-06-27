using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    // Centralized RoofDef references.
    [DefOf]
    public static class Roofs
    {
        public static RoofDef RoofConstructed;

        static Roofs() => DefOfHelper.EnsureInitializedInCtor(typeof(Roofs));
    }
}
