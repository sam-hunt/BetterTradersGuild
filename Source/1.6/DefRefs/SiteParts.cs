using RimWorld;

namespace BetterTradersGuild.DefRefs
{
    // Centralized SitePartDef references.
    [DefOf]
    public static class SiteParts
    {
        public static SitePartDef BTG_SmugglersDen;

        static SiteParts() => DefOfHelper.EnsureInitializedInCtor(typeof(SiteParts));
    }
}
