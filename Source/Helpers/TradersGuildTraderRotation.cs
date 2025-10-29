using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Helper class for managing TradersGuild trader rotation timing
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: This centralizes all logic for trader rotation timing,
    /// ensuring consistent behavior between visited and unvisited settlements.
    ///
    /// Key Concepts:
    /// - Virtual Schedule: Each settlement has a deterministic rotation schedule
    ///   based on its ID, independent of whether it's been visited
    /// - Settlement Offset: Uses settlement ID to desynchronize rotation times
    /// - Configurable Interval: Player can adjust rotation frequency via mod settings
    /// </remarks>
    public static class TradersGuildTraderRotation
    {
        // Prime number for settlement offset calculation (reduces pattern artifacts)
        private const int OffsetMultiplier = 123457;

        /// <summary>
        /// Gets the trader rotation interval in ticks from mod settings
        /// </summary>
        /// <returns>Rotation interval in game ticks (days × 60000)</returns>
        public static int GetRotationIntervalTicks()
        {
            // LEARNING NOTE: One in-game day = 60,000 ticks
            // This value is player-configurable via mod settings
            return BetterTradersGuildMod.Settings.traderRotationIntervalDays * 60000;
        }

        /// <summary>
        /// Calculates the virtual lastStockGenerationTicks for a settlement
        /// </summary>
        /// <param name="settlementID">The settlement's unique ID</param>
        /// <returns>Virtual lastStockGenerationTicks value (always >= 0)</returns>
        /// <remarks>
        /// This creates a stable, settlement-specific rotation schedule.
        /// The same settlement ID always returns the same schedule pattern,
        /// but different settlements have different schedules (desynchronized).
        /// </remarks>
        public static int GetVirtualLastStockTicks(int settlementID)
        {
            int interval = GetRotationIntervalTicks();
            int currentTicks = Find.TickManager.TicksGame;

            // Calculate settlement-specific offset
            // LEARNING NOTE: Using modulo ensures the offset is within [0, interval)
            // The prime multiplier helps avoid patterns where similar IDs have similar offsets
            int offset = (settlementID * OffsetMultiplier) % interval;

            // Round current ticks to the nearest interval boundary, adjusted by offset
            // LEARNING NOTE: This formula ensures:
            // 1. The result aligns with interval boundaries for this specific settlement
            // 2. The result is stable (doesn't change every tick)
            // 3. Different settlements have different boundaries (via offset)
            int boundary = ((currentTicks + offset) / interval) * interval;

            // Calculate the virtual lastStockTicks (boundary minus the offset)
            int virtualTicks = boundary - offset;

            // CRITICAL FIX: Ensure we never return negative values
            // In early game, offset might be larger than boundary, causing negative results
            // If negative, move forward one interval to get a positive value
            if (virtualTicks < 0)
            {
                virtualTicks += interval;
            }

            return virtualTicks;
        }

        /// <summary>
        /// Calculates the next restock tick for a settlement based on virtual schedule
        /// </summary>
        /// <param name="settlementID">The settlement's unique ID</param>
        /// <returns>Tick when the settlement should next regenerate stock</returns>
        public static int GetNextRestockTick(int settlementID)
        {
            int virtualLastStock = GetVirtualLastStockTicks(settlementID);
            int interval = GetRotationIntervalTicks();
            return virtualLastStock + interval;
        }

        /// <summary>
        /// Checks if a settlement should regenerate stock right now
        /// </summary>
        /// <param name="settlement">The settlement to check</param>
        /// <param name="currentLastStockTicks">Current lastStockGenerationTicks value</param>
        /// <returns>True if stock should regenerate</returns>
        public static bool ShouldRegenerateNow(Settlement settlement, int currentLastStockTicks)
        {
            if (currentLastStockTicks == -1)
                return true; // Never generated - should generate now

            int nextRestock = GetNextRestockTick(settlement.ID);
            return Find.TickManager.TicksGame >= nextRestock;
        }
    }
}
