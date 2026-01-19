using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.MapComponents
{
    /// <summary>
    /// MapComponent that preserves trade inventory when a TradersGuild settlement is defeated.
    ///
    /// Problem: When SettlementDefeatUtility.CheckDefeated() runs:
    /// 1. Creates a DestroyedSettlement
    /// 2. Reassigns map.info.parent = destroyedSettlement
    /// 3. Destroys the original Settlement via settlement.Destroy()
    /// 4. Settlement.PostRemove() calls trader.TryDestroyStock() - stock is destroyed
    ///
    /// Solution: This MapComponent caches the trade inventory before defeat.
    /// The settlement map survives defeat (just gets a new parent), so this component persists.
    ///
    /// Access pattern from pocket map:
    /// pocketMap.Parent -> PocketMapParent
    /// pocketMapParent.sourceMap -> Settlement Map (still exists!)
    /// settlementMap.GetComponent&lt;SettlementStockCache&gt;() -> Cached stock
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: Implements IThingHolder because ThingOwner requires a holder.
    /// The IThingHolder interface provides the container hierarchy for things.
    /// </remarks>
    public class SettlementStockCache : MapComponent, IThingHolder
    {
        /// <summary>
        /// Trade inventory preserved from the defeated settlement.
        /// Items are transferred here before TryDestroyStock runs.
        /// </summary>
        public ThingOwner<Thing> preservedStock;

        /// <summary>
        /// The ID of the original settlement before defeat.
        /// Used for deterministic seeding in cargo vault generation.
        /// </summary>
        public int originalSettlementId;

        public SettlementStockCache(Map map) : base(map)
        {
            preservedStock = new ThingOwner<Thing>(this);
        }

        #region IThingHolder Implementation

        /// <summary>
        /// Returns the parent thing holder (the map).
        /// </summary>
        public IThingHolder ParentHolder => map;

        /// <summary>
        /// Returns all direct thing holders (our preserved stock).
        /// </summary>
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        /// <summary>
        /// Returns all things directly held (the preserved stock contents).
        /// </summary>
        public ThingOwner GetDirectlyHeldThings()
        {
            return preservedStock;
        }

        #endregion

        /// <summary>
        /// Saves and loads the preserved stock and original settlement ID.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref preservedStock, "preservedStock");
            Scribe_Values.Look(ref originalSettlementId, "originalSettlementId");

            // Reinitialize ThingOwner if null after loading
            if (Scribe.mode == LoadSaveMode.PostLoadInit && preservedStock == null)
            {
                preservedStock = new ThingOwner<Thing>(this);
            }
        }

        /// <summary>
        /// Called when the map is being removed from the game.
        /// Cleans up remaining preserved stock to mirror TryDestroyStock behavior.
        /// </summary>
        public override void MapRemoved()
        {
            base.MapRemoved();

            // Only clean up if settlement was defeated (parent is DestroyedSettlement)
            if (map.Parent is DestroyedSettlement && preservedStock != null)
            {
                // Mirror TryDestroyStock behavior:
                // - Pawns: don't destroy (they're world pawns)
                // - Items: destroy with DestroyMode.Vanish
                for (int i = preservedStock.Count - 1; i >= 0; i--)
                {
                    Thing item = preservedStock[i];
                    preservedStock.Remove(item);

                    if (!(item is Pawn) && !item.Destroyed)
                    {
                        item.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }
    }
}
