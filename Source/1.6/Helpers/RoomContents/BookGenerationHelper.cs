using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Discovers all book-type ThingDefs (novels, textbooks, tomes, mod-added) and generates
    // random books from the full pool. Used by ShelfCustomizer and TableCustomizer.
    internal static class BookGenerationHelper
    {
        private static List<ThingDef> cachedBookDefs;
        private static bool initialized;

        // Gets all book-type ThingDefs by scanning for defs with CompBook.
        // Includes vanilla novels, textbooks, Anomaly tomes, and any mod-added book types.
        // Results are cached after first call.
        private static IReadOnlyList<ThingDef> GetBookDefs()
        {
            if (!initialized)
            {
                cachedBookDefs = DefDatabase<ThingDef>.AllDefs
                    .Where(d => d.HasComp(typeof(CompBook)))
                    .ToList();
                initialized = true;
            }
            return cachedBookDefs;
        }

        // Generates a random book from any available book type with proper content initialization.
        internal static Thing GenerateRandomBook()
        {
            var bookDefs = GetBookDefs();
            if (bookDefs.Count == 0) return null;

            ThingDef bookDef = bookDefs.RandomElement();
            return BookUtility.MakeBook(bookDef, ArtGenerationContext.Outsider, null);
        }
    }
}
