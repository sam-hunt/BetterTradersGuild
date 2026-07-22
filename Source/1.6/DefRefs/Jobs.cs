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

        // Paramedic-mech rescue: carry a downed defender to an in-medbay medical bed.
        // Mirrors vanilla Rescue but uses a driver without the guest-of-player logic
        // (vanilla CheckMakeTakeeGuest hardcodes Faction.OfPlayer, which red-errors or
        // corrupts HostFaction when the rescuer is an NPC). See JobDriver_BTGRescue.
        public static JobDef BTG_Rescue;

        // Paramedic-mech patient feeding: hand-feed a downed defender in the medbay.
        // Mirrors vanilla FeedPatient but uses a driver whose fail condition doesn't
        // require a player-faction/player-hosted patient (vanilla's ShouldBeFedBySomeone
        // gate kills the job on tick one for NPC patients, job-looping the medic).
        // See JobDriver_BTGFeedPatient.
        public static JobDef BTG_FeedPatient;

        // Escape job: a sheltering-civilian walker carries one infant/baby and loads it
        // into a launchable (shuttle / transport pod). TargetA = baby, TargetB = launchable.
        // See JobDriver_BTGCarryBabyToLaunchable.
        public static JobDef BTG_CarryBabyToLaunchable;

        // Escape job: a sheltering-civilian walker boards a launchable themselves, entering
        // its transporter container. TargetA = launchable. See JobDriver_BTGBoardLaunchable.
        public static JobDef BTG_BoardLaunchable;

        static Jobs() => DefOfHelper.EnsureInitializedInCtor(typeof(Jobs));
    }
}
