using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.MechChargerPatches
{
    /// <summary>
    /// Harmony patch: Building_MechCharger.CanPawnChargeCurrently
    /// Prevents mechs from using rechargers belonging to hostile factions.
    /// </summary>
    /// <remarks>
    /// Vanilla Biotech doesn't check charger faction ownership because enemy factions
    /// don't normally have mech rechargers. BTG spawns working rechargers in Traders Guild
    /// settlements, causing player mechs to path into hostile territory to charge.
    /// The IsForbidden API also doesn't help: CaresAboutForbidden returns false for
    /// colony mechs, bypassing forbidden checks entirely.
    ///
    /// Patching CanPawnChargeCurrently (rather than GetClosestCharger) ensures hostile
    /// chargers are excluded from the search, so the algorithm can still find valid ones.
    /// </remarks>
    [HarmonyPatch(typeof(Building_MechCharger), nameof(Building_MechCharger.CanPawnChargeCurrently))]
    public static class BuildingMechChargerCanPawnChargeCurrently
    {
        [HarmonyPostfix]
        public static void Postfix(Building_MechCharger __instance, Pawn pawn, ref bool __result)
        {
            if (__result && __instance.Faction != null && __instance.Faction.HostileTo(pawn.Faction))
                __result = false;
        }
    }
}
