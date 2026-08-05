using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.MapComponents
{
    // MapComponent that preserves trade inventory when a TradersGuild settlement is defeated.
    //
    // Problem: When SettlementDefeatUtility.CheckDefeated() runs:
    // 1. Creates a DestroyedSettlement
    // 2. Reassigns map.info.parent = destroyedSettlement
    // 3. Destroys the original Settlement via settlement.Destroy()
    // 4. Settlement.PostRemove() calls trader.TryDestroyStock() - stock is destroyed
    //
    // Solution: This MapComponent caches the trade inventory before defeat.
    // The settlement map survives defeat (just gets a new parent), so this component persists.
    //
    // Access pattern from pocket map:
    // pocketMap.Parent -> PocketMapParent
    // pocketMapParent.sourceMap -> Settlement Map (still exists!)
    // settlementMap.GetComponent<SettlementStockCache>() -> Cached stock
    // LEARNING NOTE: Implements IThingHolder because ThingOwner requires a holder.
    // The IThingHolder interface provides the container hierarchy for things.
    public class SettlementStockCache : MapComponent, IThingHolder
    {
        // Trade inventory preserved from the defeated settlement.
        // Items are transferred here before TryDestroyStock runs.
        public ThingOwner<Thing> preservedStock;

        // The ID of the original settlement before defeat.
        // Used for deterministic seeding in cargo vault generation.
        public int originalSettlementId;

        // Scratch list for save-safe pawn handling; see ExposeData.
        private List<Pawn> tmpSavedPawns = new List<Pawn>();

        public SettlementStockCache(Map map) : base(map)
        {
            preservedStock = new ThingOwner<Thing>(this);
        }

        #region IThingHolder Impl

        // Returns the parent thing holder (the map).
        public IThingHolder ParentHolder => map;

        // Returns all direct thing holders (our preserved stock).
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        // Returns all things directly held (the preserved stock contents).
        public ThingOwner GetDirectlyHeldThings()
        {
            return preservedStock;
        }

        #endregion

        // Saves and loads the preserved stock and original settlement ID.
        // Pawns in stock are registered world pawns, which WorldPawns already deep-saves.
        // Deep-saving them here too would scribe each pawn twice and duplicate it on load,
        // so mirror Settlement_TraderTracker.ExposeData: strip pawns before the deep scribe,
        // save them by reference, and re-add them afterwards (on save AND after load).
        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                tmpSavedPawns.Clear();
                if (preservedStock != null)
                {
                    for (int i = preservedStock.Count - 1; i >= 0; i--)
                    {
                        if (preservedStock[i] is Pawn pawn)
                        {
                            preservedStock.Remove(pawn);
                            tmpSavedPawns.Add(pawn);
                        }
                    }
                }
            }

            Scribe_Collections.Look(ref tmpSavedPawns, "tmpSavedPawns", LookMode.Reference);
            Scribe_Deep.Look(ref preservedStock, "preservedStock");
            Scribe_Values.Look(ref originalSettlementId, "originalSettlementId");

            if (Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving)
            {
                // Reinitialize after loading old saves that predate this component's data
                if (preservedStock == null)
                    preservedStock = new ThingOwner<Thing>(this);
                if (tmpSavedPawns == null)
                    tmpSavedPawns = new List<Pawn>();

                for (int i = 0; i < tmpSavedPawns.Count; i++)
                {
                    preservedStock.TryAdd(tmpSavedPawns[i], canMergeWithExistingStacks: false);
                }
                tmpSavedPawns.Clear();
            }
        }

        // Called when the map is being removed from the game.
        // Cleans up remaining preserved stock to mirror TryDestroyStock behavior.
        public override void MapRemoved()
        {
            base.MapRemoved();

            // Clean up if settlement was defeated or quest site is being removed
            if ((map.Parent is DestroyedSettlement || map.Parent is Site) && preservedStock != null)
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
