using System.Collections.Generic;
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
    /// - Populated: When RegenerateStock completes for a TG settlement
    /// - Evicted: When TryDestroyStock runs (stock expiry/rotation) or settlement is defeated
    /// - Persisted: Via ExposeData across save/load
    ///
    /// For unvisited settlements (no stock generated), the cache has no entry and the
    /// TraderKind getter falls back to deterministic virtual schedule calculation.
    /// </remarks>
    public class TradersGuildWorldComponent : WorldComponent
    {
        /// <summary>
        /// Cache mapping settlement ID to TraderKindDef defName.
        /// Using defName (string) rather than TraderKindDef directly for safe serialization.
        /// </summary>
        private Dictionary<int, string> cachedTraderKinds = new Dictionary<int, string>();

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
        /// </summary>
        /// <param name="settlementId">The settlement's unique ID</param>
        /// <param name="traderKind">The TraderKindDef that was selected</param>
        public void CacheTraderKind(int settlementId, TraderKindDef traderKind)
        {
            if (traderKind == null)
                return;

            cachedTraderKinds[settlementId] = traderKind.defName;
        }

        /// <summary>
        /// Attempts to retrieve a cached trader kind for a settlement.
        /// </summary>
        /// <param name="settlementId">The settlement's unique ID</param>
        /// <param name="traderKind">The cached TraderKindDef, or null if not found</param>
        /// <returns>True if a cached value was found, false otherwise</returns>
        public bool TryGetCachedTraderKind(int settlementId, out TraderKindDef traderKind)
        {
            traderKind = null;

            if (!cachedTraderKinds.TryGetValue(settlementId, out string defName))
                return false;

            traderKind = DefDatabase<TraderKindDef>.GetNamedSilentFail(defName);

            // If the def no longer exists (mod removed), clean up the stale entry
            if (traderKind == null)
            {
                cachedTraderKinds.Remove(settlementId);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes a cached trader kind entry for a settlement.
        /// Called when stock is destroyed or settlement is defeated.
        /// </summary>
        /// <param name="settlementId">The settlement's unique ID</param>
        public void RemoveCachedTraderKind(int settlementId)
        {
            cachedTraderKinds.Remove(settlementId);
        }

        /// <summary>
        /// Saves and loads the trader kind cache.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref cachedTraderKinds, "cachedTraderKinds", LookMode.Value, LookMode.Value);

            // Ensure dictionary is initialized after loading
            if (Scribe.mode == LoadSaveMode.PostLoadInit && cachedTraderKinds == null)
            {
                cachedTraderKinds = new Dictionary<int, string>();
            }
        }
    }
}
