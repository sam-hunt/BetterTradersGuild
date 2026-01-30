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
        /// <returns>Virtual lastStockGenerationTicks value (may be negative in early game)</returns>
        /// <remarks>
        /// This creates a stable, settlement-specific rotation schedule.
        /// The same settlement ID always returns the same schedule pattern,
        /// but different settlements have different schedules (desynchronized).
        ///
        /// The returned value represents the most recent rotation boundary at or before
        /// the current tick. In early game, this may be negative (conceptually before
        /// game start), which is valid for seed calculation and timing purposes.
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

            // Ensure virtualTicks represents the most recent PAST rotation boundary
            // Two adjustments may be needed:

            // 1. Handle negative values (early game, large offset)
            if (virtualTicks < 0)
            {
                virtualTicks += interval;
            }

            // 2. Handle overshoot into future (can happen after negative adjustment)
            // The "last stock" tick must be at or before current time
            if (virtualTicks > currentTicks)
            {
                virtualTicks -= interval;
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
        /// Determines the correct lastStockTicks to use for trader selection.
        /// This is the unified logic for both preview and stock generation flows.
        /// </summary>
        /// <param name="settlementID">The settlement's unique ID</param>
        /// <param name="storedLastStockTicks">The value stored in Settlement_TraderTracker.lastStockGenerationTicks</param>
        /// <returns>The effective lastStockTicks to use for deterministic trader selection</returns>
        /// <remarks>
        /// This method handles three cases:
        /// 1. Unvisited settlement (storedLastStockTicks == -1): Use virtual schedule
        /// 2. Visited settlement, rotation occurred: Use NEW virtual schedule for current rotation cycle
        /// 3. Visited settlement, no rotation: Use stored value (preserves consistency within rotation period)
        ///
        /// By using this helper in both GetTraderKind (preview) and RegenerateStockAlignment (generation),
        /// we ensure both paths produce the same trader type for the same rotation cycle.
        /// </remarks>
        public static int GetEffectiveLastStockTicks(int settlementID, int storedLastStockTicks)
        {
            int interval = GetRotationIntervalTicks();
            int currentTicks = Find.TickManager.TicksGame;

            // Case 1: Never visited - use virtual schedule
            if (storedLastStockTicks == -1)
            {
                return GetVirtualLastStockTicks(settlementID);
            }

            // Case 2 & 3: Visited - check if rotation occurred
            int expirationTick = storedLastStockTicks + interval;
            bool rotationOccurred = currentTicks >= expirationTick;

            if (rotationOccurred)
            {
                // Rotation occurred - use NEW virtual schedule for current rotation cycle
                return GetVirtualLastStockTicks(settlementID);
            }

            // No rotation - use stored value to maintain consistency within rotation period
            return storedLastStockTicks;
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
