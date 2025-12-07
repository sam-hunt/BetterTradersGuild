using HarmonyLib;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.WorldGridPatches
{
    /// <summary>
    /// Harmony patch: Prevent InvalidCastException in WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost
    /// This method is called when calculating caravan speed for the caravan info gizmo
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.WorldGrid), "FindMostReasonableAdjacentTileForDisplayedPathCost")]
    public static class WorldGridFindMostReasonableAdjacentTile
    {
        [HarmonyPrefix]
        public static bool Prefix(PlanetTile fromTile, ref PlanetTile __result)
        {
            // If this is a Traders Guild tile, return itself as the "most reasonable adjacent tile"
            // This prevents the cast exception when iterating through adjacent tiles
            if (TileHelper.IsFriendlyTradersGuildTile(fromTile))
            {
                __result = fromTile;
                return false; // Skip original
            }
            return true; // Run original
        }
    }
}
