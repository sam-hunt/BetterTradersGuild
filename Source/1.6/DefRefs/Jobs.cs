using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    // Centralized JobDef references.
    [DefOf]
    public static class Jobs
    {
        public static JobDef LayDownResting;

        // Job for relocking a cargo vault hatch.
        public static JobDef BTG_Relock;

        // Job for a defender to open an in-structure container (survival-meal
        // pallet) when foraging, without a player Open designation.
        public static JobDef BTG_OpenContainer;

        // Job for a starving defender to use an in-structure comms console and call
        // in a cargo-pod food resupply drop (last-resort hunger escalation).
        public static JobDef BTG_CallResupply;

        static Jobs() => DefOfHelper.EnsureInitializedInCtor(typeof(Jobs));
    }
}
