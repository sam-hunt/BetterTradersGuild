using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.PlanetTilePatches
{
    /// <summary>
    /// Harmony patch: Allow caravans to reach friendly Traders Guild settlements in space
    /// Modifies the LayerDef check for caravan formation
    /// </summary>
    [HarmonyPatch(typeof(PlanetTile), nameof(PlanetTile.LayerDef), MethodType.Getter)]
    public static class PlanetTileLayerDef
    {
        private static PlanetLayerDef modifiedSpaceDef = null;
        private static readonly MethodInfo MemberwiseCloneMethod =
            AccessTools.Method(typeof(object), "MemberwiseClone");

        [HarmonyPostfix]
        public static void Postfix(PlanetTile __instance, ref PlanetLayerDef __result)
        {
            // Only care about space layers that don't allow caravans
            if (__result == null || __result.canFormCaravans || !__result.isSpace)
                return;

            // Check if this is a friendly Traders Guild tile
            if (!TileHelper.IsFriendlyTradersGuildTile(__instance))
                return;

            // Clone the orbit LayerDef once, preserving all vanilla properties
            // (e.g. rangeDistanceFactor=20 prevents proximity goodwill penalties,
            // onlyAllowWhitelist* filters inappropriate incidents/arrivals/quests)
            // and only overriding canFormCaravans.
            if (modifiedSpaceDef == null)
            {
                modifiedSpaceDef = (PlanetLayerDef)MemberwiseCloneMethod.Invoke(__result, null);
                modifiedSpaceDef.defName = "Space_BTG";
                modifiedSpaceDef.canFormCaravans = true;    // This is the key change for trade visits
                modifiedSpaceDef.raidPointsFactor = 1.0f;   // Default for space is 0.85
            }

            __result = modifiedSpaceDef;
        }
    }
}
