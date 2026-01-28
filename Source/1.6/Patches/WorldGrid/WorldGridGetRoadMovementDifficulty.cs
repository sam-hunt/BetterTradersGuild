using HarmonyLib;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.WorldGridPatches
{
    /// <summary>
    /// Harmony patch: Prevent InvalidCastException in WorldGrid.GetRoadMovementDifficultyMultiplier
    ///
    /// PURPOSE: Prevents UI crashes during path cost calculations involving space tiles.
    ///
    /// CONTEXT: This method is called during movement cost calculations and display rendering.
    /// The vanilla implementation casts tiles to SurfaceTile to check for roads, but space/orbital
    /// tiles are just Tile (not SurfaceTile), causing InvalidCastException.
    ///
    /// RELATED PATCHES:
    /// - WorldGridFindMostReasonableAdjacentTile: Also prevents UI crashes for adjacent tile lookups
    /// - CaravanPathFollowerStartPath: Blocks actual movement initiation from space tiles
    ///
    /// Together these patches allow caravans to exist on space tiles (for TradersGuild trading)
    /// while preventing both UI crashes and unintended movement.
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.WorldGrid), "GetRoadMovementDifficultyMultiplier")]
    public static class WorldGridGetRoadMovementDifficulty
    {
        [HarmonyPrefix]
        public static bool Prefix(PlanetTile fromTile, PlanetTile toTile, ref float __result)
        {
            if (!TileHelper.IsSpaceTile(fromTile) && !TileHelper.IsSpaceTile(toTile))
                return true; // Run original for non-space tiles

            // Space tiles are not SurfaceTiles - vanilla will crash trying to cast them.
            // Return 1.0 (no road modifier) since there are no roads in space.
            __result = 1f;
            return false;
        }
    }
}
