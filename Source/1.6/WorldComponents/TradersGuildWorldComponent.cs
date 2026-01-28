using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.Helpers;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.WorldComponents
{
    /// <summary>
    /// WorldComponent that persists trader kind selections for TradersGuild settlements.
    ///
    /// When stock is generated for a settlement, the selected TraderKindDef is cached here.
    /// This ensures that subsequent calls to TraderKind (trade dialog, inspect string, cargo vault)
    /// return the exact same trader that was used for stock generation, rather than relying on
    /// recalculation which could diverge if conditions change.
    /// </summary>
    /// <remarks>
    /// Cache lifecycle:
    /// - Populated: When RegenerateStock completes for a TG settlement, OR when preview is first requested
    /// - Evicted: When expiration tick passes, TryDestroyStock runs, or settlement is defeated
    /// - Persisted: Via ExposeData across save/load
    ///
    /// All cache entries (visited and unvisited) have an expiration tick calculated at cache time.
    /// This is necessary because vanilla's TryDestroyStock is not reliably called on time passage alone.
    /// Expiration ticks are updated when the rotation interval setting changes.
    /// </remarks>
    public class TradersGuildWorldComponent : WorldComponent
    {
        /// <summary>
        /// Cache mapping settlement ID to TraderKindDef defName.
        /// Using defName (string) rather than TraderKindDef directly for safe serialization.
        /// </summary>
        private Dictionary<int, string> cachedTraderKinds = new Dictionary<int, string>();

        /// <summary>
        /// Expiration ticks for all cache entries (both visited and unvisited settlements).
        /// Vanilla's TryDestroyStock is not reliably called on time passage alone,
        /// so we track expiration for all cached traders.
        /// </summary>
        private Dictionary<int, int> cacheExpirationTicks = new Dictionary<int, int>();

        public TradersGuildWorldComponent(World world) : base(world)
        {
        }

        /// <summary>
        /// Gets the TradersGuildWorldComponent from the current world.
        /// Returns null if no world is loaded.
        /// </summary>
        public static TradersGuildWorldComponent GetComponent()
        {
            return Find.World?.GetComponent<TradersGuildWorldComponent>();
        }

        /// <summary>
        /// Caches the trader kind for a settlement after stock generation.
        /// Sets expiration tick for rotation - vanilla's TryDestroyStock is not reliably called on time passage.
        /// </summary>
        /// <param name="settlementId">The settlement's unique ID</param>
        /// <param name="traderKind">The TraderKindDef that was selected</param>
        public void CacheTraderKind(int settlementId, TraderKindDef traderKind)
        {
            if (traderKind == null)
                return;

            cachedTraderKinds[settlementId] = traderKind.defName;

            // Set expiration tick for rotation
            // We can't rely on vanilla's TryDestroyStock being called automatically on time passage
            int expirationTick = TradersGuildTraderRotation.GetNextRestockTick(settlementId);
            cacheExpirationTicks[settlementId] = expirationTick;
        }

        /// <summary>
        /// Caches a preview trader kind for an unvisited settlement.
        /// Includes an expiration tick calculated at cache time.
        /// </summary>
        /// <param name="settlementId">The settlement's unique ID</param>
        /// <param name="traderKind">The TraderKindDef that was selected</param>
        public void CachePreviewTraderKind(int settlementId, TraderKindDef traderKind)
        {
            // Both preview and visited caches work the same way now
            CacheTraderKind(settlementId, traderKind);
        }

        /// <summary>
        /// Invalidates all cached traders. Called when the rotation interval setting changes.
        /// </summary>
        /// <remarks>
        /// When interval changes, the rotation schedule changes entirely - different virtual
        /// last stock ticks mean different seeds and potentially different traders.
        /// Rather than trying to update expiration times, we invalidate all caches and let
        /// them be recalculated on next access with the new interval.
        /// </remarks>
        public void InvalidateAllCaches()
        {
            cachedTraderKinds.Clear();
            cacheExpirationTicks.Clear();
        }

        /// <summary>
        /// Scales all cache expiration times proportionally when rotation interval changes.
        /// Preserves trader types while adjusting timing to match new interval.
        /// </summary>
        /// <param name="oldIntervalTicks">The previous rotation interval in ticks</param>
        /// <param name="newIntervalTicks">The new rotation interval in ticks</param>
        /// <remarks>
        /// Example: If interval changes from 30 days to 15 days (halved),
        /// a trader with 12 days remaining will now have 6 days remaining.
        /// This preserves the fraction of rotation period remaining.
        /// </remarks>
        public void ScaleExpirationsForIntervalChange(int oldIntervalTicks, int newIntervalTicks)
        {
            if (oldIntervalTicks <= 0 || newIntervalTicks <= 0)
                return;

            int currentTicks = Find.TickManager.TicksGame;

            foreach (var settlementId in cacheExpirationTicks.Keys.ToList())
            {
                int oldExpiration = cacheExpirationTicks[settlementId];
                int remainingTicks = oldExpiration - currentTicks;

                if (remainingTicks <= 0)
                {
                    // Already expired - remove stale entry
                    RemoveCachedTraderKind(settlementId);
                    continue;
                }

                // Scale proportionally: preserve the fraction of rotation period remaining
                // Use long arithmetic to avoid overflow with large tick values
                int newRemainingTicks = (int)((long)remainingTicks * newIntervalTicks / oldIntervalTicks);
                cacheExpirationTicks[settlementId] = currentTicks + newRemainingTicks;
            }
        }

        /// <summary>
        /// Attempts to retrieve a cached trader kind for a settlement.
        /// Checks expiration before returning.
        /// </summary>
        /// <param name="settlementId">The settlement's unique ID</param>
        /// <param name="traderKind">The cached TraderKindDef, or null if not found/expired</param>
        /// <returns>True if a valid cached value was found, false otherwise</returns>
        public bool TryGetCachedTraderKind(int settlementId, out TraderKindDef traderKind)
        {
            traderKind = null;

            if (!cachedTraderKinds.TryGetValue(settlementId, out string defName))
            {
                Log.Message($"[BTG DEBUG] TryGetCachedTraderKind({settlementId}): No cache entry");
                return false;
            }

            // Check if cache has expired
            if (cacheExpirationTicks.TryGetValue(settlementId, out int expirationTick))
            {
                int currentTicks = Find.TickManager.TicksGame;
                if (currentTicks >= expirationTick)
                {
                    // Expired - remove and return false to trigger recalculation
                    Log.Message($"[BTG DEBUG] TryGetCachedTraderKind({settlementId}): EXPIRED - currentTicks={currentTicks}, expirationTick={expirationTick}, trader was {defName}");
                    cachedTraderKinds.Remove(settlementId);
                    cacheExpirationTicks.Remove(settlementId);
                    return false;
                }
                Log.Message($"[BTG DEBUG] TryGetCachedTraderKind({settlementId}): Cache HIT - {defName}, expires in {(expirationTick - currentTicks) / 60000f:F1} days");
            }
            else
            {
                Log.Warning($"[BTG DEBUG] TryGetCachedTraderKind({settlementId}): Has trader {defName} but NO expiration tick!");
            }

            traderKind = DefDatabase<TraderKindDef>.GetNamedSilentFail(defName);

            // If the def no longer exists (mod removed), clean up the stale entry
            if (traderKind == null)
            {
                cachedTraderKinds.Remove(settlementId);
                cacheExpirationTicks.Remove(settlementId);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve the cached expiration tick for a settlement.
        /// Used by inspection string to show departure time without recalculating virtual schedule.
        /// </summary>
        /// <param name="settlementId">The settlement's unique ID</param>
        /// <param name="expirationTick">The cached expiration tick, or 0 if not found</param>
        /// <returns>True if a cached expiration was found, false otherwise</returns>
        public bool TryGetCachedExpirationTick(int settlementId, out int expirationTick)
        {
            return cacheExpirationTicks.TryGetValue(settlementId, out expirationTick);
        }

        /// <summary>
        /// Removes a cached trader kind entry for a settlement.
        /// Called when stock is destroyed or settlement is defeated.
        /// </summary>
        /// <param name="settlementId">The settlement's unique ID</param>
        public void RemoveCachedTraderKind(int settlementId)
        {
            cachedTraderKinds.Remove(settlementId);
            cacheExpirationTicks.Remove(settlementId);
        }

        /// <summary>
        /// Saves and loads the trader kind cache.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref cachedTraderKinds, "cachedTraderKinds", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref cacheExpirationTicks, "cacheExpirationTicks", LookMode.Value, LookMode.Value);

            // Ensure dictionaries are initialized after loading
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (cachedTraderKinds == null)
                    cachedTraderKinds = new Dictionary<int, string>();
                if (cacheExpirationTicks == null)
                    cacheExpirationTicks = new Dictionary<int, int>();
            }
        }
    }
}
