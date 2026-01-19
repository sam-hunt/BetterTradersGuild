using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized JobDef references.
    /// </summary>
    [DefOf]
    public static class Jobs
    {
        public static JobDef LayDownResting;

        /// <summary>
        /// Job for relocking a cargo vault hatch.
        /// </summary>
        public static JobDef BTG_Relock;

        static Jobs() => DefOfHelper.EnsureInitializedInCtor(typeof(Jobs));
    }
}
