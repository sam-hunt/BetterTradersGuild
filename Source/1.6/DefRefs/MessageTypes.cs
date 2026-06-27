using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    // Centralized MessageTypeDef references.
    [DefOf]
    public static class MessageTypes
    {
        public static MessageTypeDef RejectInput;
        public static MessageTypeDef NegativeEvent;

        static MessageTypes() => DefOfHelper.EnsureInitializedInCtor(typeof(MessageTypes));
    }
}
