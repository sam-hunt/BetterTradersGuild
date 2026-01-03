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

        static Jobs() => DefOfHelper.EnsureInitializedInCtor(typeof(Jobs));
    }
}
