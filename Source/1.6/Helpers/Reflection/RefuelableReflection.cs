using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.Reflection
{
    /// <summary>
    /// Single owner for reflection against RimWorld's private <see cref="CompRefuelable"/>.fuel
    /// field. BTG sets fuel directly — bypassing <c>Refuel(float)</c>'s difficulty multiplier —
    /// when pre-fuelling generated pod launchers and the arrival shuttle.
    ///
    /// <para>Replaces scattered <c>Traverse.Create(comp).Field("fuel")</c> calls, which silently
    /// returned a default on a missing field; this owner instead surfaces drift at startup.</para>
    /// </summary>
    public static class RefuelableReflection
    {
        /// <summary>Private <c>float fuel</c> field.</summary>
        public static readonly FieldInfo FuelField = AccessTools.Field(typeof(CompRefuelable), "fuel");

        /// <summary>
        /// Sets a refuelable's fuel level directly. Returns false (no-op) if the component is
        /// null or the field could not be resolved.
        /// </summary>
        public static bool TrySetFuel(CompRefuelable fuelComp, float fuel)
        {
            if (fuelComp == null || FuelField == null)
                return false;

            FuelField.SetValue(fuelComp, fuel);
            return true;
        }

        /// <summary>
        /// Logs a targeted error if the field failed to resolve. Called once at startup
        /// from <see cref="ReflectionVerification.VerifyAll"/>.
        /// </summary>
        public static void VerifyReflection()
        {
            if (FuelField == null)
                Log.Error("[Better Traders Guild] CompRefuelable.fuel field not found via reflection; "
                    + "generated pod launchers and arrival shuttles will spawn unfuelled. RimWorld API may have changed.");
        }
    }
}
