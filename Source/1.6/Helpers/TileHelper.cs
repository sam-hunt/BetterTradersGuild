using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild
{
    // Helper to check tile properties for caravan-related patches
    public static class TileHelper
    {
        // Checks if a tile is in space (orbital layer).
        // Used by patches that need to protect against SurfaceTile cast exceptions.
        // Space tiles are NOT SurfaceTiles, so vanilla code that assumes SurfaceTile will crash.
        public static bool IsSpaceTile(PlanetTile tile)
        {
            return tile.LayerDef?.isSpace == true;
        }

        // Checks if a tile hosts a peacefully-visitable Traders Guild settlement.
        // Used to decide whether caravan operations are allowed at this tile.
        // Direct lookup (linear scan over settlements): every caller sits on a cold,
        // main-thread path (shuttle arrival, raid point calculation, float menus), so no
        // caching is needed. Returns false when no world is loaded.
        public static bool IsFriendlyTradersGuildTile(PlanetTile tile)
        {
            if (!tile.Valid)
                return false;

            Settlement settlement = Find.WorldObjects?.SettlementAt(tile);
            return settlement != null
                && TradersGuildHelper.IsTradersGuildSettlement(settlement)
                && TradersGuildHelper.CanPeacefullyVisit(settlement.Faction);
        }
    }
}
