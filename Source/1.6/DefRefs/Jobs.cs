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

        /// <summary>
        /// Job for a defender to open an in-structure container (survival-meal
        /// pallet) when foraging, without a player Open designation.
        /// </summary>
        public static JobDef BTG_OpenContainer;

        static Jobs() => DefOfHelper.EnsureInitializedInCtor(typeof(Jobs));
    }
}
