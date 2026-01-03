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
        public static LayoutDef BTG_Settlement;

        static Layouts() => DefOfHelper.EnsureInitializedInCtor(typeof(Layouts));
    }
}
