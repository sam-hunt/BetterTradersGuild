using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.PlanetTilePatches
{
    /// <summary>
    /// Harmony patch: Allow caravans to reach friendly Traders Guild settlements in space
    /// Modifies the LayerDef check for caravan formation
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.PlanetTile), nameof(RimWorld.Planet.PlanetTile.LayerDef), MethodType.Getter)]
    public static class PlanetTileLayerDef
    {
        // Store modified LayerDef to reuse
        private static PlanetLayerDef modifiedSpaceDef = null;

        [HarmonyPostfix]
        public static void Postfix(RimWorld.Planet.PlanetTile __instance, ref PlanetLayerDef __result)
        {
            // Only care about space layers that don't allow caravans
            if (__result == null || __result.canFormCaravans || !__result.isSpace)
                return;

            // Check if this is a friendly Traders Guild tile
            if (!TileHelper.IsFriendlyTradersGuildTile(__instance))
                return;

            // Create modified def once
            if (modifiedSpaceDef == null)
            {
                modifiedSpaceDef = new PlanetLayerDef();
                modifiedSpaceDef.defName = "Space_BTG";
                modifiedSpaceDef.label = __result.label;
                modifiedSpaceDef.canFormCaravans = true;  // Key change!
                modifiedSpaceDef.isSpace = true;
                modifiedSpaceDef.alwaysRaycastable = true;
                modifiedSpaceDef.obstructsExpandingIcons = true;
            }

            __result = modifiedSpaceDef;
        }
    }
}
