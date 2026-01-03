using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized RoofDef references.
    /// </summary>
    [DefOf]
    public static class Roofs
    {
        public static RoofDef RoofConstructed;

        static Roofs() => DefOfHelper.EnsureInitializedInCtor(typeof(Roofs));
    }
}
