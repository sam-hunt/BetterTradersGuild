using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Helper to check if a tile has a friendly Traders Guild settlement
    /// Used by multiple patches to determine if we should allow caravan operations
    /// </summary>
    public static class TileHelper
    {
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
