using HarmonyLib;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.WorldGridPatches
{
    /// <summary>
    /// Harmony patch: Prevent InvalidCastException in WorldGrid.GetRoadMovementDifficultyMultiplier
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.WorldGrid), "GetRoadMovementDifficultyMultiplier")]
    public static class WorldGridGetRoadMovementDifficulty
    {
        [HarmonyPrefix]
        public static bool Prefix(PlanetTile fromTile, PlanetTile toTile, ref float __result)
        {
            // If either tile is a Traders Guild tile, return 1.0 (no road bonus/penalty)
            if (TileHelper.IsFriendlyTradersGuildTile(fromTile) || TileHelper.IsFriendlyTradersGuildTile(toTile))
            {
                __result = 1f;
                return false; // Skip original
            }
            return true; // Run original
        }
    }
}
