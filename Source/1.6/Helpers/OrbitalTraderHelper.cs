using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Shared helper for orbital trader type filtering and selection.
    ///
    /// Extracted from SettlementTraderTrackerGetTraderKind to allow reuse by
    /// the smuggler's den quest reward system (QuestNode_BTG_SmugglersDen_Rewards).
    /// </summary>
    public static class OrbitalTraderHelper
    {
        /// <summary>
        /// Gets all available orbital traders, filtered for the current world state.
        ///
        /// Filters:
        /// 1. Must be orbital trader (t.orbital == true)
        /// 2. If tied to a faction, that faction must exist in the current world
        /// 3. If faction doesn't approve of slavery, slave ship traders are excluded
        /// </summary>
        /// <param name="factionForSlaveryCheck">
        /// Faction to check ideology slavery approval against.
        /// If null, no slavery filtering is applied.
        /// </param>
        /// <returns>Filtered list of available orbital TraderKindDefs</returns>
        public static List<TraderKindDef> GetAvailableOrbitalTraders(Faction factionForSlaveryCheck = null)
        {
            List<TraderKindDef> allOrbitalTraders = DefDatabase<TraderKindDef>.AllDefsListForReading
                .Where(t => t.orbital)
                .ToList();

            // Filter out traders tied to factions not present in the current world
            allOrbitalTraders.RemoveAll(t =>
                t.faction != null && Find.FactionManager.FirstFactionOfDef(t.faction) == null);

            // Filter out slave ship traders if faction's ideology doesn't approve
            if (factionForSlaveryCheck != null && !FactionApprovesOfSlavery(factionForSlaveryCheck))
            {
                allOrbitalTraders.RemoveAll(t => HasSlavesStockGenerator(t));
            }

            return allOrbitalTraders;
        }

        /// <summary>
        /// Checks if a faction's ideology approves of slavery.
        /// Returns true if no ideology system is active (classic mode) or if any ideo approves.
        /// </summary>
        public static bool FactionApprovesOfSlavery(Faction faction)
        {
            if (faction?.ideos == null)
                return true;

            foreach (var ideo in faction.ideos.AllIdeos)
            {
                if (IdeoUtility.IdeoApprovesOfSlavery(ideo))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a trader kind has a StockGenerator_Slaves in its stock generators.
        /// </summary>
        public static bool HasSlavesStockGenerator(TraderKindDef trader)
        {
            if (trader.stockGenerators == null)
                return false;

            foreach (var sg in trader.stockGenerators)
            {
                if (sg is StockGenerator_Slaves)
                    return true;
            }
            return false;
        }
    }
}
