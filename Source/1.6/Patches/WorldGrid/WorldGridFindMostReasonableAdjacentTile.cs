using HarmonyLib;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.WorldGridPatches
{
    /// <summary>
    /// Harmony patch: Prevent InvalidCastException in WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost
    ///
    /// PURPOSE: Prevents UI crashes when a caravan is selected on a space tile.
    ///
    /// CONTEXT: This method is called by Gizmo_CaravanInfo.GizmoOnGUI to calculate "tiles per day"
    /// display info. It runs whenever a caravan is selected, regardless of whether it's moving.
    /// The vanilla implementation casts tiles to SurfaceTile, but space/orbital tiles are just Tile
    /// (not SurfaceTile), causing InvalidCastException.
    ///
    /// RELATED PATCHES:
    /// - WorldGridGetRoadMovementDifficulty: Also prevents UI crashes for road cost calculations
    /// - CaravanPathFollowerStartPath: Blocks actual movement initiation from space tiles
    ///
    /// Together these patches allow caravans to exist on space tiles (for TradersGuild trading)
    /// while preventing both UI crashes and unintended movement.
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.WorldGrid), "FindMostReasonableAdjacentTileForDisplayedPathCost")]
    public static class WorldGridFindMostReasonableAdjacentTile
    {
        [HarmonyPrefix]
        public static bool Prefix(PlanetTile fromTile, ref PlanetTile __result)
        {
            if (!TileHelper.IsSpaceTile(fromTile))
                return true; // Run original for non-space tiles

            // Space tiles are not SurfaceTiles - vanilla will crash trying to cast them.
            // Return the tile itself since there are no meaningful "adjacent tiles" in space.
            __result = fromTile;
            return false;
        }
    }
}
