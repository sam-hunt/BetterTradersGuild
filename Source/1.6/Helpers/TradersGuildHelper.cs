using BetterTradersGuild.DefRefs;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Helper class for checking Traders Guild faction status and relations
    /// </summary>
    public static class TradersGuildHelper
    {
        /// <summary>
        /// Checks if a settlement belongs to the Traders Guild faction.
        /// </summary>
        public static bool IsTradersGuildSettlement(Settlement settlement)
        {
            // Null check - make sure the settlement and its faction exist
            if (settlement == null || settlement.Faction == null)
                return false;

            // Check if the faction's def matches the Traders Guild
            return settlement.Faction.def == Factions.TradersGuild;
        }

        /// <summary>
        /// Checks if the player can peacefully visit a faction's settlement
        /// Requires non-hostile relations (neutral or better)
        /// </summary>
        public static bool CanPeacefullyVisit(Faction faction)
        {
            // Null check
            if (faction == null)
                return false;

            // Check if the faction is hostile to the player
            // PlayerRelationKind.Hostile means they're enemies
            // We want Neutral or Ally
            return faction.PlayerRelationKind != FactionRelationKind.Hostile;
        }

        /// <summary>
        /// Checks if a map belongs to a TradersGuild settlement.
        /// Used by patches that need to determine context during map events.
        /// </summary>
        public static bool IsMapInTradersGuildSettlement(Verse.Map map)
        {
            if (map == null)
                return false;

            Settlement settlement = map.Parent as Settlement;
            return IsTradersGuildSettlement(settlement);
        }

        /// <summary>
        /// Gets the TradersGuild faction, or null if not found.
        /// </summary>
        public static Faction GetTradersGuildFaction()
        {
            return Find.FactionManager?.FirstFactionOfDef(Factions.TradersGuild);
        }
    }
}
