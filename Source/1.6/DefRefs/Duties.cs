using RimWorld;
using Verse.AI;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized DutyDef references for Better Traders Guild custom duties.
    /// </summary>
    [DefOf]
    public static class Duties
    {
        /// <summary>
        /// Passive wander duty - mechs wander within 7 tiles of focus point
        /// without actively seeking enemies. Used for expensive/specialized
        /// mechs (Fabricor, Paramedic, Cleansweeper, Agrihand).
        /// </summary>
        public static DutyDef BTG_WanderInArea;

        static Duties() => DefOfHelper.EnsureInitializedInCtor(typeof(Duties));
    }
}
