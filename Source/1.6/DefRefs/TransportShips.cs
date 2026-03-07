using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    [DefOf]
    public static class TransportShips
    {
        public static TransportShipDef Ship_PassengerShuttle;

        static TransportShips() => DefOfHelper.EnsureInitializedInCtor(typeof(TransportShips));
    }
}
