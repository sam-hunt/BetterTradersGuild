using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized MessageTypeDef references.
    /// </summary>
    [DefOf]
    public static class MessageTypes
    {
        public static MessageTypeDef RejectInput;
        public static MessageTypeDef NegativeEvent;

        static MessageTypes() => DefOfHelper.EnsureInitializedInCtor(typeof(MessageTypes));
    }
}
