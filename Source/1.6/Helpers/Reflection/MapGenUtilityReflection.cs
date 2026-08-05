using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.Reflection
{
    // Single owner for reflection against RimWorld's private MapGenUtility.DefaultPawnsPoints
    // field: the flat points range (1150-1600 as of 1.6) that MapGenUtility.GeneratePawns
    // rolls when no explicit points are passed - i.e. the vanilla strength of every
    // settlement garrison.
    //
    // GenStep_BTGSettlementPawns needs the same base roll when the defender settings scale
    // it, so we read the real field rather than hardcoding a copy that would silently drift
    // if vanilla ever rebalances the range.
    public static class MapGenUtilityReflection
    {
        private static readonly FieldInfo DefaultPawnsPointsField =
            AccessTools.Field(typeof(MapGenUtility), "DefaultPawnsPoints");

        // Vanilla's flat garrison points range, or the last-known 1.6 value if the field
        // failed to resolve (callers keep working; VerifyReflection reports the drift).
        public static FloatRange DefaultPawnsPoints =>
            DefaultPawnsPointsField != null
                ? (FloatRange)DefaultPawnsPointsField.GetValue(null)
                : new FloatRange(1150f, 1600f);

        // Logs a targeted error if the field failed to resolve. Called once at startup
        // from ReflectionVerification.VerifyAll.
        public static void VerifyReflection()
        {
            if (DefaultPawnsPointsField == null)
                Log.Error("[Better Traders Guild] MapGenUtility.DefaultPawnsPoints field not found via reflection; "
                    + "defender scaling settings will assume the last-known vanilla base roll (1150-1600). RimWorld API may have changed.");
        }
    }
}
