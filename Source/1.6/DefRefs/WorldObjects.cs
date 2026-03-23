using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized WorldObjectDef references.
    /// </summary>
    [DefOf]
    public static class WorldObjects
    {
        public static WorldObjectDef BTG_SmugglersDenSite;

        static WorldObjects() => DefOfHelper.EnsureInitializedInCtor(typeof(WorldObjects));
    }
}
