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

        /// <summary>
        /// Bounded defender duty - combat-engaged but constrained to the
        /// settlement structure footprint. Target acquisition is filtered
        /// by JobGiver_BTGDefendStructure so defenders never path outside the
        /// structure to pursue intruders. Includes self-tend and forage
        /// fallbacks for human defenders.
        /// </summary>
        public static DutyDef BTG_DefendStructure;

        static Duties() => DefOfHelper.EnsureInitializedInCtor(typeof(Duties));
    }
}
