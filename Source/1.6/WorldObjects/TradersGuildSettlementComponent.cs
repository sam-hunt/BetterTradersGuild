using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.WorldObjects
{
    /// <summary>
    /// WorldObjectComp attached to TradersGuild settlements to track cargo refresh state.
    ///
    /// PURPOSE:
    /// Prevents exploit farming by tracking when shuttle bay cargo was last refreshed.
    /// Cargo should only refresh when trader rotation occurs (Phase 2 system), not on
    /// every map entry.
    ///
    /// ANTI-EXPLOIT MECHANISM:
    /// - lastCargoRefreshTicks: Stores the tick value when cargo was last spawned/refreshed
    /// - Compared against Settlement_TraderTracker.lastStockGenerationTicks
    /// - If lastCargoRefreshTicks < lastStockGenerationTicks: trader rotated, refresh cargo
    /// - If lastCargoRefreshTicks >= lastStockGenerationTicks: already refreshed, skip
    ///
    /// ARCHITECTURE:
    /// - Initialized on first map generation (Phase 3.2)
    /// - Read by TradersGuildCargoRefresher MapComponent (Phase 3.5)
    /// - Saves/loads with settlement world object data
    /// </summary>
    public class TradersGuildSettlementComponent : WorldObjectComp
    {
        /// <summary>
        /// The tick value when shuttle bay cargo was last spawned or refreshed.
        ///
        /// Values:
        /// - -1: Never generated (first-time generation needed)
        /// - >= 0: Tick when cargo was last refreshed
        ///
        /// Used to prevent exploit farming by only refreshing cargo when
        /// trader rotation occurs (indicated by change in lastStockGenerationTicks).
        /// </summary>
        private long lastCargoRefreshTicks = -1;

        /// <summary>
        /// Public accessor for last cargo refresh tick value.
        /// </summary>
        public long LastCargoRefreshTicks
        {
            get => lastCargoRefreshTicks;
            set => lastCargoRefreshTicks = value;
        }

        /// <summary>
        /// Checks if cargo needs to be refreshed based on trader rotation.
        ///
        /// Returns true if:
        /// - Never generated before (lastCargoRefreshTicks == -1), OR
        /// - Trader has rotated since last refresh (lastCargoRefreshTicks < traderLastStockTicks)
        ///
        /// This prevents exploit farming while enabling dynamic cargo rotation.
        /// </summary>
        /// <param name="traderLastStockTicks">Current value of Settlement_TraderTracker.lastStockGenerationTicks</param>
        /// <returns>True if cargo should be refreshed</returns>
        public bool ShouldRefreshCargo(long traderLastStockTicks)
        {
            // First generation (never spawned cargo before)
            if (lastCargoRefreshTicks == -1)
            {
                return true;
            }

            // Trader has rotated since last cargo refresh
            if (lastCargoRefreshTicks < traderLastStockTicks)
            {
                return true;
            }

            // Already refreshed for current trader rotation
            return false;
        }

        /// <summary>
        /// Save/load component data with settlement.
        /// Required for persistence across game saves.
        ///
        /// LEARNING NOTE (WorldObjectComp vs ThingComp):
        /// WorldObjectComp uses PostExposeData() instead of ExposeData().
        /// This is called by the parent WorldObject after its own data is exposed.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastCargoRefreshTicks, "lastCargoRefreshTicks", -1);
        }
    }
}
