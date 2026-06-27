using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    // Centralized ThingCategoryDef references.
    [DefOf]
    public static class ThingCategories
    {
        public static ThingCategoryDef MortarShells;

        static ThingCategories() => DefOfHelper.EnsureInitializedInCtor(typeof(ThingCategories));
    }
}
