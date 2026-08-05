using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.Reflection
{
    // Single owner for reflection against RimWorld's private CompRefuelable.fuel
    // field. BTG sets fuel directly — bypassing Refuel(float)'s difficulty multiplier —
    // when pre-fuelling generated pod launchers and the arrival shuttle.
    //
    // Replaces scattered Traverse.Create(comp).Field("fuel") calls, which silently
    // returned a default on a missing field; this owner instead surfaces drift at startup.
    public static class RefuelableReflection
    {
        // Private float fuel field.
        public static readonly FieldInfo FuelField = AccessTools.Field(typeof(CompRefuelable), "fuel");

        // Sets a refuelable's fuel level directly. Returns false (no-op) if the component is
        // null or the field could not be resolved.
        public static bool TrySetFuel(CompRefuelable fuelComp, float fuel)
        {
            if (fuelComp == null || FuelField == null)
                return false;

            FuelField.SetValue(fuelComp, fuel);
            return true;
        }

        // Logs a targeted error if the field failed to resolve. Called once at startup
        // from ReflectionVerification.VerifyAll.
        public static void VerifyReflection()
        {
            if (FuelField == null)
                Log.Error("[Better Traders Guild] CompRefuelable.fuel field not found via reflection; "
                    + "generated pod launchers and arrival shuttles will spawn unfuelled. RimWorld API may have changed.");
        }
    }
}
