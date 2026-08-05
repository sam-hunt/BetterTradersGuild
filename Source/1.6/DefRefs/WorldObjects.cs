using RimWorld;

namespace BetterTradersGuild.DefRefs
{
    // Centralized WorldObjectDef references.
    [DefOf]
    public static class WorldObjects
    {
        public static WorldObjectDef BTG_SmugglersDenSite;

        static WorldObjects() => DefOfHelper.EnsureInitializedInCtor(typeof(WorldObjects));
    }
}
