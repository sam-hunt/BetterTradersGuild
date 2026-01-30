using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Helper class for finding and filling shelves during room generation.
    /// Provides methods for locating Building_Storage shelves and adding items
    /// with proper capacity checking (respects maxItemsInCell).
    ///
    /// LEARNING NOTE: Building_Storage shelves don't have an inner container API
    /// like bookcases. Items are spawned at cell positions and the storage building
    /// automatically tracks them. However, spawning must respect maxItemsInCell
    /// (3 for shelves) by checking StoreUtility.IsValidStorageFor() before spawning.
    ///
    /// USAGE: Designed for reuse in any RoomContentsWorker. Call this AFTER base.FillRoom()
    /// to populate shelves placed by XML prefabs.
    /// </summary>
    public static class RoomShelfHelper
    {
        /// <summary>
        /// Finds all Building_Storage shelves of a specific def in the search area.
        /// Uses HashSet internally to deduplicate multi-cell buildings.
        /// </summary>
        /// <param name="map">The map to search</param>
        /// <param name="searchArea">Area to search (typically the full room rect)</param>
        /// <param name="shelfDef">The ThingDef of the shelf (default: Things.Shelf)</param>
        /// <param name="requiredWidth">Required width of shelf (default: 2 for 2x1 shelves). Pass null for any size.</param>
        /// <returns>List of unique Building_Storage shelves found in the area</returns>
        public static List<Building_Storage> GetShelvesInRoom(
            Map map,
            CellRect searchArea,
            ThingDef shelfDef = null,
            int? requiredWidth = 2)
        {
            List<Building_Storage> shelves = new List<Building_Storage>();

            // Default to Things.Shelf
            shelfDef = shelfDef ?? Things.Shelf;
            if (shelfDef == null) return shelves;

            // Use HashSet to avoid duplicates (multi-cell buildings appear at multiple positions)
            HashSet<Building_Storage> uniqueShelves = new HashSet<Building_Storage>();

            foreach (IntVec3 cell in searchArea.Cells)
            {
                if (!cell.InBounds(map)) continue;

                List<Thing> things = cell.GetThingList(map);
                if (things == null) continue;

                foreach (Thing thing in things)
                {
                    if (thing.def == shelfDef && thing is Building_Storage storage)
                    {
                        // Check width if required
                        if (requiredWidth.HasValue)
                        {
                            if (thing.def.size.x != requiredWidth.Value &&
                                thing.def.size.z != requiredWidth.Value)
                            {
                                continue;
                            }
                        }

                        uniqueShelves.Add(storage);
                    }
                }
            }

            shelves.AddRange(uniqueShelves);
            return shelves;
        }

        /// <summary>
        /// Spawns a stack of items into a shelf's first available cell.
        /// Uses StoreUtility.IsValidStorageFor() to check capacity before spawning.
        /// Respects maxItemsInCell limit (3 for shelves).
        /// </summary>
        /// <param name="map">The map containing the shelf</param>
        /// <param name="shelf">The Building_Storage shelf to add items to</param>
        /// <param name="itemDef">The ThingDef of the item to spawn</param>
        /// <param name="stackCount">Number of items in the stack</param>
        /// <param name="setForbidden">Whether to mark spawned items as forbidden (default: true)</param>
        /// <returns>The spawned Thing, or null if no space available or def is null</returns>
        public static Thing AddItemsToShelf(
            Map map,
            Building_Storage shelf,
            ThingDef itemDef,
            int stackCount,
            bool setForbidden = true)
        {
            if (itemDef == null)
            {
                Log.Warning("[Better Traders Guild] AddItemsToShelf called with null itemDef");
                return null;
            }

            Thing item = ThingMaker.MakeThing(itemDef);
            item.stackCount = stackCount;

            if (AddItemToShelf(map, shelf, item, setForbidden))
            {
                return item;
            }

            // Clean up if spawn failed
            item.Destroy(DestroyMode.Vanish);
            return null;
        }

        /// <summary>
        /// Adds a pre-created Thing to a shelf, prioritizing empty cells first.
        /// Uses StoreUtility.IsValidStorageFor() to check capacity before spawning.
        /// Respects maxItemsInCell limit.
        ///
        /// LEARNING NOTE: For items that need quality, stuff, or other pre-configuration,
        /// create the Thing first with ThingMaker.MakeThing(), configure it, then call this method.
        ///
        /// This method prioritizes empty cells to spread items across the shelf for
        /// a cleaner visual appearance, rather than stacking everything in the first cell.
        /// </summary>
        /// <param name="map">The map containing the shelf</param>
        /// <param name="shelf">The Building_Storage shelf to add the item to</param>
        /// <param name="item">The pre-created Thing to add</param>
        /// <param name="setForbidden">Whether to mark spawned items as forbidden (default: true)</param>
        /// <returns>True if spawn succeeded, false if no space available</returns>
        public static bool AddItemToShelf(
            Map map,
            Building_Storage shelf,
            Thing item,
            bool setForbidden = true)
        {
            if (shelf == null || item == null)
            {
                return false;
            }

            List<IntVec3> slotCells = shelf.AllSlotCellsList();

            // First pass: prioritize empty cells for better visual spread
            foreach (IntVec3 cell in slotCells)
            {
                if (IsCellEmptyOfItems(map, cell) && CanCellAcceptItem(map, cell, item))
                {
                    GenSpawn.Spawn(item, cell, map);
                    item.SetForbidden(setForbidden, false);
                    return true;
                }
            }

            // Second pass: fall back to any cell that can accept the item
            foreach (IntVec3 cell in slotCells)
            {
                if (CanCellAcceptItem(map, cell, item))
                {
                    GenSpawn.Spawn(item, cell, map);
                    item.SetForbidden(setForbidden, false);
                    return true;
                }
            }

            // No space available in any cell
            return false;
        }

        /// <summary>
        /// Checks if a cell contains no items (ignores buildings, terrain, etc.).
        /// Used to prioritize spreading items across shelf cells.
        /// </summary>
        /// <param name="map">The map containing the cell</param>
        /// <param name="cell">The cell to check</param>
        /// <returns>True if the cell has no items</returns>
        public static bool IsCellEmptyOfItems(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            foreach (Thing thing in things)
            {
                if (thing.def.category == ThingCategory.Item)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the list of slot cells for a shelf.
        /// Convenience wrapper for shelf.AllSlotCellsList().
        /// </summary>
        /// <param name="shelf">The Building_Storage shelf</param>
        /// <returns>List of cells covered by the shelf, or empty list if shelf is null</returns>
        public static List<IntVec3> GetShelfSlotCells(Building_Storage shelf)
        {
            if (shelf == null)
            {
                return new List<IntVec3>();
            }

            return shelf.AllSlotCellsList();
        }

        /// <summary>
        /// Checks if a cell can accept a thing (respects maxItemsInCell and storage filters).
        /// Uses StoreUtility.IsValidStorageFor() internally.
        ///
        /// LEARNING NOTE: StoreUtility.IsValidStorageFor() internally calls the private
        /// NoStorageBlockersIn() method which checks maxItemsInCell, existing items,
        /// and whether the item can stack with existing items.
        /// </summary>
        /// <param name="map">The map containing the cell</param>
        /// <param name="cell">The cell to check</param>
        /// <param name="item">The item to check acceptance for</param>
        /// <returns>True if the cell can accept the item</returns>
        public static bool CanCellAcceptItem(Map map, IntVec3 cell, Thing item)
        {
            if (!cell.InBounds(map) || item == null)
            {
                return false;
            }

            return StoreUtility.IsValidStorageFor(cell, map, item);
        }
    }
}
