using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Helper to check tile properties for caravan-related patches
    /// </summary>
    public static class TileHelper
    {
        /// <summary>
        /// Checks if a tile is in space (orbital layer).
        /// Used by patches that need to protect against SurfaceTile cast exceptions.
        /// Space tiles are NOT SurfaceTiles, so vanilla code that assumes SurfaceTile will crash.
        /// </summary>
        public static bool IsSpaceTile(PlanetTile tile)
        {
            return tile.LayerDef?.isSpace == true;
        }

        /// <summary>
        /// Checks if a tile has a friendly Traders Guild settlement.
        /// Used to determine if we should allow caravan operations at this tile.
        /// </summary>
        public static bool IsFriendlyTradersGuildTile(PlanetTile tile)
        {
            WorldObjectsHolder worldObjects = Find.WorldObjects;
            if (worldObjects == null)
                return false;

            Settlement settlement = worldObjects.SettlementAt(tile);
            if (settlement == null)
                return false;

            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return false;

            return TradersGuildHelper.CanPeacefullyVisit(settlement.Faction);
        }
    }
}
