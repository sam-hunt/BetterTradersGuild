using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized ThingCategoryDef references.
    /// </summary>
    [DefOf]
    public static class ThingCategories
    {
        public static ThingCategoryDef MortarShells;

        static ThingCategories() => DefOfHelper.EnsureInitializedInCtor(typeof(ThingCategories));
    }
}
