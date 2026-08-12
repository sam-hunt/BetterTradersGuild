using RimWorld;
using Verse;

namespace BetterTradersGuild.AI
{
    // NPC patients must never be gated out of their own faction's medical beds by the
    // PLAYER's medical-defaults screen. Every bed-lying path (RestUtility.TuckIntoBed,
    // LayDown's FailOnBedNoLongerUsable) runs RestUtility.CanUseBedNow, whose medical-bed
    // branch calls HealthAIUtility.ShouldEverReceiveMedicalCareFromPlayer - false when
    // the pawn has playerSettings with medCare == NoCare. NPC pawns normally pass because
    // playerSettings is null, but a pawn can acquire one: vanilla creates it when the
    // player ever hosts or captures the pawn, and JobDriver_BTGRescue used to create one
    // before tuck-in (copied from vanilla TakeToBed), seeded by ResetMedicalCare from the
    // player's default care for the pawn's faction relation. Players who set that
    // relation to no-care got defenders silently dumped beside the bed instead of tucked
    // in, then tended on the floor forever - the setting is scribed, so saves from before
    // the fix still carry it on previously rescued defenders.
    public static class NpcMedicalCare
    {
        // Clears the one medCare value that blocks bed rest. NoMeds keeps the player's
        // spend-nothing-on-them intent while passing
        // ShouldEverReceiveMedicalCareFromPlayer; the medic sources its own medicine
        // (JobGiver_BTGMechMedicTend supplies TargetB itself), so tend quality is
        // unaffected either way.
        public static void EnsureBedRestAllowed(Pawn pawn)
        {
            if (pawn.playerSettings != null && pawn.playerSettings.medCare == MedicalCareCategory.NoCare)
                pawn.playerSettings.medCare = MedicalCareCategory.NoMeds;
        }
    }
}
