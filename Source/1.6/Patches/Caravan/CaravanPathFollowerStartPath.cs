using HarmonyLib;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.CaravanPatches
{
    /// <summary>
    /// Harmony patch: Prevent caravans on space tiles from pathing to other tiles.
    ///
    /// PURPOSE: Blocks movement initiation from space tiles, enforcing shuttle-only travel.
    ///
    /// CONTEXT: Caravans can exist on space tiles (at TradersGuild orbital settlements) for trading,
    /// but space is not pathable terrain. Without this patch, vanilla would allow path previews
    /// and movement attempts to other space sites since they share the same PlanetLayer.
    /// The only way to leave a space tile should be via shuttle launch gizmo.
    ///
    /// BEHAVIOR:
    /// - Same-tile arrivals allowed (e.g., trading actions that don't change position)
    /// - Cross-tile pathing blocked (returns false, no path preview shown)
    ///
    /// RELATED PATCHES:
    /// - WorldGridFindMostReasonableAdjacentTile: Prevents UI crashes when caravan selected
    /// - WorldGridGetRoadMovementDifficulty: Prevents UI crashes during cost calculations
    ///
    /// The WorldGrid patches handle display/UI code that runs regardless of movement.
    /// This patch handles the actual movement initiation logic.
    /// </summary>
    [HarmonyPatch(typeof(Caravan_PathFollower), nameof(Caravan_PathFollower.StartPath))]
    public static class CaravanPathFollowerStartPath
    {
        [HarmonyPrefix]
        public static bool Prefix(Caravan ___caravan, PlanetTile destTile, ref bool __result)
        {
            if (___caravan == null)
                return true; // Let vanilla handle null

            PlanetTile currentTile = ___caravan.Tile;

            if (!TileHelper.IsSpaceTile(currentTile))
                return true; // Non-space tiles use normal pathing

            if (currentTile == destTile)
                return true; // Same-tile arrivals allowed (e.g., trading)

            // Block cross-tile pathing from space - only shuttle launch can move the caravan
            __result = false;
            return false;
        }
    }
}
