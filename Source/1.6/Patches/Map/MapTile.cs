using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.MapPatches
{
    /// <summary>
    /// Harmony patch: Fix Map.Tile for BTG cargo vault pocket maps during world rendering.
    ///
    /// PROBLEM:
    /// The world background renderer (WorldCameraDriver.ApplyMapPositionToGameObject) uses
    /// Map.Tile.Layer.BackgroundWorldCameraOffset to determine camera distance/zoom.
    /// For pocket maps, Map.Tile returns PlanetTile.Invalid (tileId -1, layerId 0), which
    /// resolves to the Surface layer instead of the Space layer, causing incorrect zoom.
    ///
    /// SOLUTION:
    /// For BTG cargo vault pocket maps, return the source map's tile instead.
    /// This ensures the pocket map uses the same PlanetLayer as its parent settlement,
    /// providing the correct BackgroundWorldCameraOffset for space map rendering.
    ///
    /// DEFENSIVE DESIGN:
    /// This patch ONLY affects pocket maps using our BTG_CargoVault MapGeneratorDef.
    /// Other pocket maps (vanilla underground, anomaly, other mods) are unaffected.
    ///
    /// WHY Map.Tile AND NOT MapInfo.Tile:
    /// Map.Tile calls MapInfo.Tile internally. Patching Map.Tile is cleaner because:
    /// - It's the public API that rendering code actually calls
    /// - We can access Map.IsPocketMap and Map.generatorDef directly
    /// - MapInfo doesn't have direct access to the MapGeneratorDef
    ///
    /// SCOPE OF EFFECT:
    /// This patch affects ALL uses of Map.Tile for our cargo vault pocket map, including:
    /// - World background rendering (the target fix)
    /// - Celestial calculations (sun/moon position)
    /// - Latitude-based calculations (seasons, plant growth)
    /// For an orbital cargo vault attached to a space station, inheriting the parent's
    /// world position for all these systems is actually correct behavior.
    /// </summary>
    [HarmonyPatch(typeof(Map), nameof(Map.Tile), MethodType.Getter)]
    public static class MapTile
    {
        [HarmonyPostfix]
        public static void Postfix(Map __instance, ref PlanetTile __result)
        {
            // Only process pocket maps
            if (!__instance.IsPocketMap)
            {
                return;
            }

            // DEFENSIVE: Only apply to our specific cargo vault pocket map (def reference comparison)
            if (__instance.generatorDef != MapGenerators.BTG_CargoVaultMapGenerator)
            {
                return;
            }

            // Only process if the current result is invalid
            if (__result.Valid)
            {
                return;
            }

            // Get the source map from the PocketMapParent
            if (__instance.Parent is PocketMapParent pocketMapParent && pocketMapParent.sourceMap != null)
            {
                // Return the source map's tile instead
                __result = pocketMapParent.sourceMap.Tile;
            }
        }
    }
}
