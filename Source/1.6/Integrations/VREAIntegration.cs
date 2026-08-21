using System;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Integrations
{
    // Optional integration with Vanilla Races Expanded - Androids (VREA).
    // BTG spawns VREA_AndroidStand buildings as crew-quarters waste fillers, owned by the
    // settlement faction; this class resolves the hook needed to keep visiting colony
    // androids from auto-charging on them.
    //
    // Why faction alone isn't enough: VREA's automatic charge search
    // (JobGiver_FreeMemorySpace.FindStandFor) gates each stand on
    // Building_AndroidStand.CannotUseNowReason plus CanReserveAndReach, none of which
    // check the stand's faction. The bed faction gate lives only in
    // RestUtility.IsValidBedFor, which that path never consults (CannotUseNowReason calls
    // the faction-blind RestUtility.CanUseBedNow instead). Only the float menu checks
    // faction, so a reachable NPC-owned stand is auto-used and even auto-assigned.
    //
    // CannotUseNowReason is the single choke point shared by both FindStandFor passes
    // (including the second pass's auto-assign) and the float-menu reason text, which is
    // why the patch (BuildingAndroidStandCannotUseNowReason) targets it rather than
    // FindStandFor itself.
    //
    // Self-reports drift at startup (Pattern B): silent when VREA isn't installed; one
    // Log.Warning when the type IS present but the member failed to resolve.
    public static class VREAIntegration
    {
        private const string StandTypeName = "VREAndroids.Building_AndroidStand";
        private const string CannotUseMethodName = "CannotUseNowReason";

        // The VREA android stand type, or null when VREA isn't loaded.
        public static readonly Type StandType;

        // Building_AndroidStand.CannotUseNowReason(Pawn).
        public static readonly MethodInfo CannotUseNowReasonMethod;

        // True only when VREA is loaded AND the reflected member resolved.
        public static bool Available => StandType != null && CannotUseNowReasonMethod != null;

        static VREAIntegration()
        {
            try
            {
                StandType = GenTypes.GetTypeInAnyAssembly(StandTypeName);
                if (StandType == null)
                    return; // VREA not installed — stay silent.

                CannotUseNowReasonMethod = StandType.GetMethod(CannotUseMethodName,
                    BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Pawn) }, null);
            }
            catch (Exception ex)
            {
                Log.Warning("[Better Traders Guild] VREA (Androids) reflection failed (visiting colony "
                    + "androids will auto-charge on settlement android stands): " + ex);
                return;
            }

            if (StandType != null && !Available)
            {
                Log.Warning("[Better Traders Guild] VREA (Androids) active but Building_AndroidStand."
                    + CannotUseMethodName + "(Pawn) could not be resolved; visiting colony androids "
                    + "will auto-charge on settlement android stands. VREA API may have changed.");
            }
        }
    }
}
