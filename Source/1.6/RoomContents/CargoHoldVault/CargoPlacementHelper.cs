using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    /// <summary>
    /// Helper class for clustered cargo placement in the cargo vault.
    /// Groups items by type and places them adjacently on shelves, with each
    /// item type starting at a random shelf to spread cargo across the room.
    /// </summary>
    public static class CargoPlacementHelper
    {
        /// <summary>
        /// Tracks a single storage cell with its capacity.
        /// </summary>
        private class CellState
        {
            public IntVec3 Cell;
            public int CurrentItems;
            public int MaxItems;

            public bool HasSpace => CurrentItems < MaxItems;
            public bool IsEmpty => CurrentItems == 0;
        }

        /// <summary>
        /// Tracks a shelf and all its cells.
        /// </summary>
        private class ShelfState
        {
            public Building_Storage Shelf;
            public List<CellState> Cells;
            public IntVec3 Position;

            public bool HasSpace => Cells.Any(c => c.HasSpace);
            public bool HasEmptyCell => Cells.Any(c => c.IsEmpty);
        }

        /// <summary>
        /// Checks if an item can be stored on any shelf.
        /// Uses the shelf's storage settings to check filters.
        /// Falls back to def's fixed storage settings if runtime settings aren't initialized.
        /// </summary>
        public static bool CanBeStoredOnShelf(Thing item, List<Building_Storage> shelves)
        {
            if (shelves == null || shelves.Count == 0 || item == null)
                return false;

            // Minified buildings are bulky - place on floor, not shelves
            if (item is MinifiedThing)
                return false;

            Building_Storage anyShelf = shelves[0];

            // During map generation, shelf settings may not be fully initialized.
            // Try runtime settings first, then fall back to def's fixed settings.
            if (anyShelf.settings?.filter != null)
            {
                return anyShelf.settings.AllowedToAccept(item);
            }

            // Fall back to the def's fixed storage settings
            StorageSettings fixedSettings = anyShelf.def.building?.fixedStorageSettings;
            if (fixedSettings?.filter != null)
            {
                return fixedSettings.AllowedToAccept(item);
            }

            // Last resort: allow any storable item
            return item.def.EverStorable(true);
        }

        /// <summary>
        /// Categorizes items into shelf-compatible and floor-only.
        /// </summary>
        public static void CategorizeForPlacement(
            List<Thing> items,
            List<Building_Storage> shelves,
            out List<Thing> shelfItems,
            out List<Thing> floorItems)
        {
            shelfItems = new List<Thing>();
            floorItems = new List<Thing>();

            foreach (Thing item in items)
            {
                if (CanBeStoredOnShelf(item, shelves))
                    shelfItems.Add(item);
                else
                    floorItems.Add(item);
            }
        }

        /// <summary>
        /// Places items on shelves using clustered placement.
        /// Items of the same type are placed adjacently, with each type
        /// starting at a random shelf to spread cargo across the room.
        /// </summary>
        /// <param name="map">The map to spawn on</param>
        /// <param name="items">Items to place (should be pre-filtered for shelf compatibility)</param>
        /// <param name="shelves">Available storage buildings</param>
        /// <returns>Items that couldn't fit (overflow)</returns>
        public static List<Thing> PlaceItemsClustered(
            Map map,
            List<Thing> items,
            List<Building_Storage> shelves)
        {
            var overflow = new List<Thing>();

            if (items == null || items.Count == 0)
                return overflow;

            if (shelves == null || shelves.Count == 0)
            {
                overflow.AddRange(items);
                return overflow;
            }

            // Build shelf state tracking
            List<ShelfState> shelfStates = BuildShelfStates(map, shelves);

            // Group items by ThingDef (same item type together)
            var itemGroups = items.GroupBy(t => t.def).ToList();

            // Separate stackable vs non-stackable groups
            // Process stackables first so they fill cells, then non-stackables can claim empty cells
            var stackableGroups = itemGroups.Where(g => g.Key.stackLimit > 1).ToList();
            var nonStackableGroups = itemGroups.Where(g => g.Key.stackLimit == 1).ToList();

            // Shuffle within each category for variety
            stackableGroups.Shuffle();
            nonStackableGroups.Shuffle();

            // Process stackables first, then non-stackables
            var orderedGroups = stackableGroups.Concat(nonStackableGroups);

            foreach (var group in orderedGroups)
            {
                List<Thing> itemsOfType = group.ToList();
                bool isNonStackable = group.Key.stackLimit == 1;

                // Find shelves with space and pick a random starting shelf
                List<ShelfState> availableShelves = shelfStates.Where(s => s.HasSpace).ToList();
                if (availableShelves.Count == 0)
                {
                    overflow.AddRange(itemsOfType);
                    continue;
                }

                ShelfState currentShelf = availableShelves.RandomElement();

                foreach (Thing item in itemsOfType)
                {
                    bool placed = false;

                    // Try to place on current shelf
                    CellState targetCell = FindCellOnShelf(map, currentShelf, item, isNonStackable);
                    if (targetCell != null)
                    {
                        PlaceItem(map, item, targetCell);
                        placed = true;
                    }
                    else
                    {
                        // Current shelf full, find closest shelf with space
                        ShelfState closestShelf = FindClosestShelfWithSpace(
                            currentShelf.Position, shelfStates, currentShelf);

                        if (closestShelf != null)
                        {
                            currentShelf = closestShelf;
                            targetCell = FindCellOnShelf(map, currentShelf, item, isNonStackable);
                            if (targetCell != null)
                            {
                                PlaceItem(map, item, targetCell);
                                placed = true;
                            }
                        }
                    }

                    if (!placed)
                    {
                        overflow.Add(item);
                    }
                }
            }

            return overflow;
        }

        /// <summary>
        /// Builds state tracking for all shelves and their cells.
        /// </summary>
        private static List<ShelfState> BuildShelfStates(Map map, List<Building_Storage> shelves)
        {
            var states = new List<ShelfState>();

            foreach (Building_Storage shelf in shelves)
            {
                int maxPerCell = shelf.def.building?.maxItemsInCell ?? 3;
                var cells = new List<CellState>();

                foreach (IntVec3 cell in shelf.AllSlotCellsList())
                {
                    int existing = cell.GetThingList(map)
                        .Count(t => t.def.category == ThingCategory.Item);

                    cells.Add(new CellState
                    {
                        Cell = cell,
                        CurrentItems = existing,
                        MaxItems = maxPerCell
                    });
                }

                states.Add(new ShelfState
                {
                    Shelf = shelf,
                    Cells = cells,
                    Position = shelf.Position
                });
            }

            return states;
        }

        /// <summary>
        /// Finds a suitable cell on the shelf for the item.
        /// For non-stackable items (stackLimit=1), prefers empty cells if available.
        /// </summary>
        private static CellState FindCellOnShelf(Map map, ShelfState shelf, Thing item, bool preferEmptyCell)
        {
            // For non-stackable items, try to find an empty cell first
            if (preferEmptyCell)
            {
                CellState emptyCell = shelf.Cells.FirstOrDefault(c =>
                    c.IsEmpty && StoreUtility.IsValidStorageFor(c.Cell, map, item));

                if (emptyCell != null)
                    return emptyCell;
            }

            // Find any cell with space that accepts this item
            return shelf.Cells.FirstOrDefault(c =>
                c.HasSpace && StoreUtility.IsValidStorageFor(c.Cell, map, item));
        }

        /// <summary>
        /// Finds the closest shelf with available space.
        /// </summary>
        private static ShelfState FindClosestShelfWithSpace(
            IntVec3 fromPosition,
            List<ShelfState> allShelves,
            ShelfState exclude)
        {
            ShelfState closest = null;
            float closestDist = float.MaxValue;

            foreach (ShelfState shelf in allShelves)
            {
                if (shelf == exclude || !shelf.HasSpace)
                    continue;

                float dist = fromPosition.DistanceToSquared(shelf.Position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = shelf;
                }
            }

            return closest;
        }

        /// <summary>
        /// Places an item in a cell and updates tracking.
        /// </summary>
        private static void PlaceItem(Map map, Thing item, CellState cell)
        {
            GenSpawn.Spawn(item, cell.Cell, map);
            item.SetForbidden(true, false);
            cell.CurrentItems++;
        }
    }
}
