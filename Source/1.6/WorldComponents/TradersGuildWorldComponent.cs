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

        /// <summary>
        /// How often (in game ticks) the friendly Traders Guild tile cache is rebuilt from
        /// <see cref="WorldComponentTick"/>. ~4 seconds at 1x speed - settlement existence and
        /// faction relations change slowly, so a short staleness window is imperceptible for
        /// caravan formation.
        /// </summary>
        private const int FriendlyTileCacheRefreshInterval = 250;

        /// <summary>
        /// Tiles that currently host a peacefully-visitable Traders Guild settlement. Rebuilt
        /// periodically on the main thread by <see cref="WorldComponentTick"/> and read (never
        /// mutated) by <see cref="IsFriendlyTradersGuildTile"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="PlanetTile.LayerDef"/> getter is extremely hot - queried per-tile during
        /// world/orbital rendering, caravan path-cost recalculation, and reachability checks, some
        /// of which RimWorld runs off the main thread. The PlanetTileLayerDef patch consults this
        /// set on every space-layer tile access; the original direct
        /// <see cref="WorldObjectsHolder.SettlementAt"/> call was a per-call linear scan over all
        /// settlements (reported as ~10% of frame time in Dubs Performance Analyzer).
        ///
        /// This is a single-writer / multi-reader cache: only the main-thread tick rebuilds it,
        /// publishing a fully-built replacement set via the <c>volatile</c> reference below, and
        /// readers only ever capture that reference and call <see cref="HashSet{T}.Contains"/> (an
        /// O(1) lookup). Because the component is owned by the World, the set is discarded
        /// automatically on world unload - no manual lifecycle handling. Purely derived data, so it
        /// is not serialized.
        /// </remarks>
        private volatile HashSet<PlanetTile> friendlyTradersGuildTiles = new HashSet<PlanetTile>();

        /// <summary>
        /// Game tick of the last friendly-tile rebuild, or -1 if never built (just after
        /// construction or load), which forces a rebuild on the next <see cref="WorldComponentTick"/>.
        /// </summary>
        private int lastFriendlyTileRebuildTick = -1;

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
        /// Seeds the friendly tile cache as soon as the world is ready.
        /// </summary>
        /// <remarks>
        /// <see cref="WorldComponentTick"/> does not run while the game is paused, and a freshly
        /// loaded save starts paused, so without this a just-loaded world would report no friendly
        /// tiles until the player unpaused for a tick. On a brand-new game the player faction may
        /// not exist yet (created after worldgen FinalizeInit), so we leave the rebuild tick at -1
        /// and let the first <see cref="WorldComponentTick"/> build it once relations can resolve.
        /// </remarks>
        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);

            if (Faction.OfPlayerSilentFail != null)
            {
                RebuildFriendlyTileCache();
                lastFriendlyTileRebuildTick = Find.TickManager?.TicksGame ?? -1;
            }
        }

        /// <summary>
        /// Rebuilds the friendly Traders Guild tile cache at most once per
        /// <see cref="FriendlyTileCacheRefreshInterval"/>. Runs on the main thread only.
        /// </summary>
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            // WorldComponentTick only runs while the game is ticking, by which point the player
            // faction always exists, so CanPeacefullyVisit can resolve relations correctly (no
            // need for the early-load / worldgen special-casing the lazy cache once required).
            int currentTick = Find.TickManager.TicksGame;
            if (lastFriendlyTileRebuildTick < 0
                || currentTick - lastFriendlyTileRebuildTick >= FriendlyTileCacheRefreshInterval)
            {
                RebuildFriendlyTileCache();
                lastFriendlyTileRebuildTick = currentTick;
            }
        }

        /// <summary>
        /// Checks if a tile currently hosts a peacefully-visitable Traders Guild settlement.
        /// O(1) read of the periodically-rebuilt cache; safe to call from any thread.
        /// </summary>
        public bool IsFriendlyTradersGuildTile(PlanetTile tile)
        {
            // Capture the volatile reference once so a concurrent main-thread rebuild (which swaps
            // in a brand-new set) can never expose a half-populated collection to this reader.
            HashSet<PlanetTile> cache = friendlyTradersGuildTiles;
            return cache != null && cache.Contains(tile);
        }

        /// <summary>
        /// Forces an immediate rebuild of the friendly tile cache. Call from the main thread when
        /// membership is known to have just changed (e.g. a Traders Guild settlement being defeated)
        /// to eliminate the staleness window for that event.
        /// </summary>
        /// <remarks>
        /// The periodic <see cref="WorldComponentTick"/> rebuild remains the primary catch-all
        /// freshness mechanism; this is an optional accelerator and is cheap/safe to call spuriously.
        /// </remarks>
        public void InvalidateFriendlyTileCache()
        {
            RebuildFriendlyTileCache();
            if (Find.TickManager != null)
                lastFriendlyTileRebuildTick = Find.TickManager.TicksGame;
        }

        private void RebuildFriendlyTileCache()
        {
            HashSet<PlanetTile> rebuilt = new HashSet<PlanetTile>();

            WorldObjectsHolder worldObjects = Find.WorldObjects;
            if (worldObjects != null)
            {
                List<Settlement> settlements = worldObjects.Settlements;
                for (int i = 0; i < settlements.Count; i++)
                {
                    Settlement settlement = settlements[i];

                    // Tile.Valid (tileId >= 0) defensively skips a settlement with an unset tile,
                    // i.e. PlanetTile.Invalid {tileId:-1}, which should never be a friendly tile.
                    // (A real settlement always has a valid tile, so this rarely fires in practice.)
                    if (settlement.Tile.Valid
                        && TradersGuildHelper.IsTradersGuildSettlement(settlement)
                        && TradersGuildHelper.CanPeacefullyVisit(settlement.Faction))
                    {
                        rebuilt.Add(settlement.Tile);
                    }
                }
            }

            // Atomic publish (volatile write): readers see either the old or the fully-built new set.
            friendlyTradersGuildTiles = rebuilt;
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
                return false;
            }

            // Check if cache has expired
            if (cacheExpirationTicks.TryGetValue(settlementId, out int expirationTick))
            {
                int currentTicks = Find.TickManager.TicksGame;
                if (currentTicks >= expirationTick)
                {
                    // Expired - remove and return false to trigger recalculation
                    cachedTraderKinds.Remove(settlementId);
                    cacheExpirationTicks.Remove(settlementId);
                    return false;
                }
            }
            else
            {
                Log.Warning($"[BTG] TryGetCachedTraderKind({settlementId}): Has trader {defName} but NO expiration tick!");
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
