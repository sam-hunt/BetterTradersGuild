using BetterTradersGuild.WorldComponents;
using RimWorld.Planet;

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

        // Checks if a tile has a friendly Traders Guild settlement.
        // Used to determine if we should allow caravan operations at this tile.
        // Delegates to TradersGuildWorldComponent, which maintains a
        // periodically-rebuilt cache, so this is an O(1) lookup on the hot
        // PlanetTile.LayerDef path. Returns false when no world is loaded.
        public static bool IsFriendlyTradersGuildTile(PlanetTile tile)
        {
            TradersGuildWorldComponent component = TradersGuildWorldComponent.GetComponent();
            return component != null && component.IsFriendlyTradersGuildTile(tile);
        }
    }
}
