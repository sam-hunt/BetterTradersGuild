using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Handles customization of small tables (Table1x2c) in CrewQuarters subrooms.
    /// Adds random items like meals, books, beer, berries, and unfinished crafting projects.
    /// </summary>
    internal static class TableCustomizer
    {
        /// <summary>
        /// Weighted outcomes for table customization.
        /// All vanilla items - no DLC gating needed. Lazy pattern for consistency.
        /// </summary>
        private static List<(float weight, Action<Map, IntVec3> action)> _outcomes;
        private static List<(float weight, Action<Map, IntVec3> action)> Outcomes => _outcomes ?? (_outcomes = BuildOutcomes());

        private static List<(float weight, Action<Map, IntVec3> action)> BuildOutcomes()
        {
            var outcomes = new List<(float weight, Action<Map, IntVec3> action)>
            {
                (30f, (map, pos) => SpawnItemOnTable(map, pos, "MealSurvivalPack", 1)),
                (5f,  (map, pos) => SpawnRandomBookOnTable(map, pos)),
                (3f,  (map, pos) => SpawnItemOnTable(map, pos, "RawBerries", Rand.RangeInclusive(10, 15))),
                (20f, (map, pos) => SpawnItemOnTable(map, pos, "Beer", 1)),
                (5f,  (map, pos) => SpawnUnfinishedItem(map, pos, "SculptureSmall", CrewQuartersHelpers.SteelDef)),
                (5f,  (map, pos) => SpawnUnfinishedItem(map, pos, "ComponentSpacer", null)),
                (3f,  (map, pos) => SpawnUnfinishedItem(map, pos, "Gun_ChargeRifle", null)),
                (29f, (map, pos) => { }) // Leave table empty
            };

            return outcomes;
        }

        /// <summary>
        /// Adds items to small tables (Table1x2c) in subrooms.
        /// </summary>
        internal static void Customize(Map map, List<CellRect> subroomRects)
        {
            ThingDef tableDef = DefDatabase<ThingDef>.GetNamed("Table1x2c", false);
            if (tableDef == null) return;

            // Find all small tables in subroom areas
            HashSet<Thing> tables = new HashSet<Thing>();
            foreach (CellRect subroomRect in subroomRects)
            {
                foreach (IntVec3 cell in subroomRect)
                {
                    if (!cell.InBounds(map)) continue;
                    foreach (Thing thing in cell.GetThingList(map))
                    {
                        if (thing.def == tableDef)
                        {
                            tables.Add(thing);
                        }
                    }
                }
            }

            foreach (Thing table in tables)
            {
                var (_, action) = Outcomes.RandomElementByWeight(x => x.weight);
                action(map, table.Position);
            }
        }

        /// <summary>
        /// Spawns a simple item on a table.
        /// </summary>
        private static void SpawnItemOnTable(Map map, IntVec3 pos, string defName, int count)
        {
            ThingDef itemDef = DefDatabase<ThingDef>.GetNamed(defName, false);
            if (itemDef == null) return;

            Thing item = ThingMaker.MakeThing(itemDef);
            item.stackCount = count;
            GenSpawn.Spawn(item, pos, map);
        }

        /// <summary>
        /// Spawns a random book on a table.
        /// </summary>
        private static void SpawnRandomBookOnTable(Map map, IntVec3 pos)
        {
            Thing book = GenerateRandomBook();
            if (book == null) return;
            GenSpawn.Spawn(book, pos, map);
        }

        /// <summary>
        /// Generates a random book with proper title/content initialization.
        /// Uses BookUtility.MakeBook for novels and textbooks.
        /// </summary>
        private static Thing GenerateRandomBook()
        {
            ThingDef novelDef = DefDatabase<ThingDef>.GetNamed("Novel", false);
            if (novelDef == null) return null;

            return BookUtility.MakeBook(novelDef, ArtGenerationContext.Outsider, null);
        }

        /// <summary>
        /// Spawns an UnfinishedThing for a recipe that produces the target item.
        /// Uses a faction pawn on the map as the creator. Falls back to raw materials
        /// if no valid recipe/pawn is found.
        /// </summary>
        private static void SpawnUnfinishedItem(Map map, IntVec3 pos, string targetDefName, ThingDef stuffDef)
        {
            ThingDef targetDef = DefDatabase<ThingDef>.GetNamed(targetDefName, false);
            if (targetDef == null) return;

            // Find a recipe that produces this item and has an unfinishedThingDef
            RecipeDef recipe = DefDatabase<RecipeDef>.AllDefs
                .FirstOrDefault(r => r.unfinishedThingDef != null &&
                                     r.products?.Any(p => p.thingDef == targetDef) == true);

            if (recipe == null || recipe.unfinishedThingDef == null) return;

            // Find a faction pawn on the map to use as creator (UnfinishedThing.Creator cannot be null)
            Pawn creator = map.mapPawns.AllPawns
                .FirstOrDefault(p => p.Faction != null && !p.Faction.IsPlayer && p.RaceProps.Humanlike);

            if (creator == null) return;

            // Determine stuff for the unfinished thing
            ThingDef unfinishedStuff = null;
            if (recipe.unfinishedThingDef.MadeFromStuff)
            {
                unfinishedStuff = stuffDef ?? GenStuff.DefaultStuffFor(recipe.unfinishedThingDef);
            }

            try
            {
                // Create the UnfinishedThing
                UnfinishedThing uft = (UnfinishedThing)ThingMaker.MakeThing(recipe.unfinishedThingDef, unfinishedStuff);

                // Set required fields
                uft.Creator = creator;
                uft.ingredients = new List<Thing>();  // Empty ingredients list (already "consumed")
                uft.workLeft = Rand.Range(100f, 500f);  // Random work remaining

                GenSpawn.Spawn(uft, pos, map);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Better Traders Guild] Failed to spawn unfinished {targetDefName}: {ex.Message}");
            }
        }
    }
}
