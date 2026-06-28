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

        // Job for a starving defender to re-open a nutrient-paste valve that BTG mapgen
        // (PipeValveHandler) closed to lock the settlement down, reconnecting a vat to
        // the paste net so a tap can dispense. Flicks CompFlickable on without a player
        // Flick designation.
        public static JobDef BTG_OpenPasteValve;

        // Cleansweeper-mech filth cleaning. Mirrors vanilla Clean but uses a driver
        // without the player Home-area gate (which a TG settlement never satisfies); the
        // work area is defined by the giver instead. See JobDriver_BTGCleanFilth.
        public static JobDef BTG_Clean;

        static Jobs() => DefOfHelper.EnsureInitializedInCtor(typeof(Jobs));
    }
}
