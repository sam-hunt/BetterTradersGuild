using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Helper class for fixing bookcase contents in room generation.
    /// Provides methods for inserting books spawned by prefabs into bookcase innerContainers.
    ///
    /// LEARNING NOTE: Vanilla PrefabUtility.SpawnPrefab() spawns items at cell positions
    /// using GenSpawn.Spawn(), which does NOT automatically insert items into containers.
    /// This affects all IThingHolder containers (bookcases, shelves, crates, etc).
    ///
    /// USAGE: Designed for reuse in any RoomContentsWorker. Call this AFTER base.FillRoom()
    /// to fix books spawned by XML prefabs that include bookcases with books.
    /// </summary>
    public static class RoomBookcaseHelper
    {
        /// <summary>
        /// Fixes bookcase contents by moving books from map into innerContainer.
        ///
        /// This post-spawn fixup finds books spawned at the same position (or adjacent to)
        /// bookcases and properly inserts them into the bookcase's innerContainer for correct
        /// rendering and interaction mechanics.
        ///
        /// DESIGN NOTE: Searches bookcase cell AND adjacent 8-way cells because books might
        /// be slightly offset in prefab definitions. This ensures we catch all intended books.
        /// </summary>
        /// <param name="map">The map to search for bookcases and books</param>
        /// <param name="searchArea">Area to search (typically the full room rect)</param>
        public static void InsertBooksIntoBookcases(Map map, CellRect searchArea)
        {
            // Find all unique bookcases in search area
            // Use HashSet to avoid duplicates (multi-cell buildings appear at multiple positions)
            HashSet<Building_Bookcase> uniqueBookcases = new HashSet<Building_Bookcase>();
            foreach (IntVec3 cell in searchArea.Cells)
            {
                List<Thing> things = cell.GetThingList(map);
                if (things != null)
                {
                    foreach (Thing thing in things)
                    {
                        if (thing is Building_Bookcase bookcase)
                        {
                            uniqueBookcases.Add(bookcase);
                        }
                    }
                }
            }

            if (uniqueBookcases.Count == 0)
            {
                return;  // No bookcases found (may not be an error - some prefab variations might not include them)
            }

            List<Building_Bookcase> bookcases = uniqueBookcases.ToList();

            // Fix each bookcase by inserting books into container
            foreach (Building_Bookcase bookcase in bookcases)
            {
                IntVec3 pos = bookcase.Position;

                // Find books at same position AND adjacent cells (books might be slightly offset)
                List<Book> booksToInsert = new List<Book>();

                // Check the bookcase's cell and all adjacent cells
                List<IntVec3> cellsToCheck = new List<IntVec3> { pos };
                cellsToCheck.AddRange(GenAdj.CellsAdjacent8Way(pos, Rot4.North, bookcase.def.size));

                foreach (IntVec3 cell in cellsToCheck)
                {
                    if (!cell.InBounds(map)) continue;

                    List<Thing> thingsAtPos = cell.GetThingList(map);
                    if (thingsAtPos != null)
                    {
                        foreach (Thing thing in thingsAtPos)
                        {
                            if (thing is Book book)
                            {
                                booksToInsert.Add(book);
                            }
                        }
                    }
                }

                // Insert books into bookcase container
                foreach (Book book in booksToInsert)
                {
                    // Get the innerContainer (ThingOwner) using the public API
                    Verse.ThingOwner container = bookcase.GetDirectlyHeldThings();

                    // Check if bookcase can accept this book
                    if (container != null && container.CanAcceptAnyOf(book, true))
                    {
                        // Remove from map
                        book.DeSpawn(DestroyMode.Vanish);

                        // Insert into bookcase container
                        bool inserted = container.TryAdd(book, true);

                        if (!inserted)
                        {
                            // Re-spawn the book if insertion failed
                            Log.Warning($"[Better Traders Guild] Failed to insert book '{book.def.defName}' into bookcase at {pos}");
                            GenSpawn.Spawn(book, pos, map);
                        }
                    }
                }
            }
        }
    }
}
