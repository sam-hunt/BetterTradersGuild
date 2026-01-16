using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace BetterTradersGuild.Patches.MapParentPatches
{
    /// <summary>
    /// Harmony patch: Fix world camera position for BTG cargo vault pocket maps.
    ///
    /// PROBLEM:
    /// Pocket maps don't have a valid world tile (PocketMapParent.Tile returns Invalid with tileId -1).
    /// When renderWorld is enabled (inherited from SpaceMapGenerator), the planet renderer calls
    /// MapParent.WorldCameraPosition which uses PlanetLayer.GetTileCenter(-1), causing repeated
    /// "Attempted to access a tile with ID -1" errors and incorrect planet background positioning.
    ///
    /// SOLUTION:
    /// Use a prefix to intercept PocketMapParent.WorldCameraPosition BEFORE the original method runs.
    /// For BTG cargo vault pocket maps, return the source map's parent's camera position instead.
    /// This prevents the error from being logged and provides the correct background view.
    ///
    /// DEFENSIVE DESIGN:
    /// This patch ONLY affects pocket maps using our BTG_CargoVault MapGeneratorDef.
    /// Other pocket maps (vanilla underground, anomaly, other mods) are unaffected.
    /// This is important because pocket maps are used for many different purposes:
    /// - Underground locations (anomaly flesh lairs, odyssey stockpiles, insect hives)
    /// - Asteroid interiors (no space background)
    /// - Disconnected dimensions (anomaly obelisk backrooms)
    /// Our cargo vault is unique in being a space pocket map attached to a space settlement.
    /// </summary>
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.WorldCameraPosition), MethodType.Getter)]
    public static class MapParentWorldCameraPosition
    {
        [HarmonyPrefix]
        public static bool Prefix(MapParent __instance, ref Vector3 __result)
        {
            // Only intercept for PocketMapParent instances
            if (__instance is not PocketMapParent pocketMapParent)
            {
                return true; // Run original for non-pocket maps
            }

            // DEFENSIVE: Only apply to our specific cargo vault pocket map (def reference comparison)
            if (pocketMapParent.mapGenerator != MapGenerators.BTG_CargoVaultMapGenerator)
            {
                return true; // Run original for other pocket maps
            }

            // Check if the pocket map has a valid source map with a parent
            if (pocketMapParent.sourceMap?.Parent != null)
            {
                // Use the source map's parent's world camera position
                __result = pocketMapParent.sourceMap.Parent.WorldCameraPosition;
                return false; // Skip original
            }

            // No source map or parent - let original run (will log error but we can't help it)
            return true;
        }
    }
}
