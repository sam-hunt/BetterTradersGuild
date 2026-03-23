using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized SitePartDef references.
    /// </summary>
    [DefOf]
    public static class SiteParts
    {
        public static SitePartDef BTG_SmugglersDen;

        static SiteParts() => DefOfHelper.EnsureInitializedInCtor(typeof(SiteParts));
    }
}
