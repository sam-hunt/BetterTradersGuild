using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Handles customization of small shelves in CrewQuarters subrooms.
    /// Includes adding random items to shelves and replacing empty shelves
    /// with interesting furniture like bookcases, cribs, weapons, or outfit stands.
    /// </summary>
    internal static class ShelfCustomizer
    {
        #region Weighted Outcome Tables

        /// <summary>
        /// Weighted outcomes for empty shelf replacement.
        /// Lazily built to filter out DLC-gated outcomes when those DLCs aren't present.
        /// </summary>
        private static List<(float weight, Action<Building_Storage, Map, Faction> action)> _emptyShelfOutcomes;
        private static List<(float weight, Action<Building_Storage, Map, Faction> action)> EmptyShelfOutcomes => _emptyShelfOutcomes ?? (_emptyShelfOutcomes = BuildEmptyShelfOutcomes());

        private static List<(float weight, Action<Building_Storage, Map, Faction> action)> BuildEmptyShelfOutcomes()
        {
            var outcomes = new List<(float weight, Action<Building_Storage, Map, Faction> action)>
            {
                (20f, (shelf, map, faction) => ReplaceShelfWithBookcase(shelf, map)),
                (5f,  (shelf, map, faction) => TryReplaceWithChessTable(shelf, map)),
                (5f,  (shelf, map, faction) => SpawnUniqueWeaponOnShelf(map, shelf, "Gun_ChargeRifle_Unique")),
                (2f,  (shelf, map, faction) => SpawnUniqueWeaponOnShelf(map, shelf, "Gun_ChargeLance_Unique")),
                (2f,  (shelf, map, faction) => SpawnUniqueWeaponOnShelf(map, shelf, "Gun_BeamRepeater_Unique")),
                (4f,  (shelf, map, faction) => SpawnUniqueWeaponOnShelf(map, shelf, "Gun_Revolver_Unique")),
                (4f,  (shelf, map, faction) => TryReplaceWithAncientSafe(shelf, map)),
                (4f,  (shelf, map, faction) => TryReplaceWithAncientCrate(shelf, map)),
                (25f, (shelf, map, faction) => ReplaceShelfWithOutfitStand(shelf, map)),
                (20f, (shelf, map, faction) => ReplaceShelfWithSculpture(shelf, map))
            };

            // Biotech DLC - Cribs/children
            if (DefDatabase<ThingDef>.GetNamed("Crib", false) != null)
                outcomes.Add((10f, (shelf, map, faction) => ReplaceShelfWithCrib(shelf, map, faction)));

            // Anomaly DLC - Golden cube
            if (CrewQuartersHelpers.GoldenCubeDef != null && CrewQuartersHelpers.ScrapCubeDef != null)
                outcomes.Add((3f, (shelf, map, faction) => SpawnGoldenCubeOrScrapCube(shelf, map)));

            // Vanilla Furniture Expanded - Spacer Module
            if (CrewQuartersHelpers.InteractiveTableDef != null)
                outcomes.Add((5f, (shelf, map, faction) => CrewQuartersHelpers.ReplaceThingAt(shelf, CrewQuartersHelpers.InteractiveTableDef, null, map)));

            return outcomes;
        }

        /// <summary>
        /// Weighted outcomes for shelf contents (items to add).
        /// All vanilla items - no DLC gating needed.
        /// </summary>
        private static readonly List<(float weight, Action<Map, Building_Storage> action)> ShelfContentsOutcomes = new List<(float, Action<Map, Building_Storage>)>
        {
            (1f,    (map, shelf) => RoomShelfHelper.AddItemsToShelf(map, shelf, "Gold", Rand.RangeInclusive(10, 50))),
            (25f,   (map, shelf) => RoomShelfHelper.AddItemsToShelf(map, shelf, "Silver", Rand.RangeInclusive(15, 50))),
            (1f,    (map, shelf) => RoomShelfHelper.AddItemsToShelf(map, shelf, "ComponentIndustrial", Rand.RangeInclusive(1, 2))),
            (3f,    (map, shelf) => RoomShelfHelper.AddItemsToShelf(map, shelf, "ComponentSpacer", Rand.RangeInclusive(2, 4))),
            (4f,    (map, shelf) => RoomShelfHelper.AddItemsToShelf(map, shelf, "MedicineIndustrial", Rand.RangeInclusive(2, 3))),
            (1.5f,  (map, shelf) => RoomShelfHelper.AddItemsToShelf(map, shelf, "MedicineUltratech", Rand.RangeInclusive(2, 3))),
            (5f,    (map, shelf) => RoomShelfHelper.AddItemsToShelf(map, shelf, "Beer", Rand.Bool ? 6 : 12)),
            (59.5f, (map, shelf) => { }) // No item added
        };

        #endregion

        #region Public Entry Points

        /// <summary>
        /// Adds random items to small shelves in subrooms. Rolls twice per shelf.
        /// </summary>
        internal static void CustomizeSmallShelves(Map map, List<CellRect> subroomRects)
        {
            if (CrewQuartersHelpers.ShelfSmallDef == null) return;

            List<Building_Storage> smallShelves = FindSmallShelves(map, subroomRects);

            // Roll twice per shelf
            foreach (Building_Storage shelf in smallShelves)
            {
                for (int i = 0; i < 2; i++)
                {
                    RollShelfContents(map, shelf);
                }
            }
        }

        /// <summary>
        /// Replaces empty small shelves with interesting furniture/items.
        /// </summary>
        internal static void CustomizeEmptyShelves(Map map, List<CellRect> subroomRects, Faction faction)
        {
            if (CrewQuartersHelpers.ShelfSmallDef == null) return;

            List<Building_Storage> smallShelves = FindSmallShelves(map, subroomRects);

            foreach (Building_Storage shelf in smallShelves)
            {
                // Check if shelf is empty
                if (!IsShelfEmpty(shelf, map)) continue;

                var (_, action) = EmptyShelfOutcomes.RandomElementByWeight(x => x.weight);
                action(shelf, map, faction);
            }
        }

        #endregion

        #region Shelf Finding

        /// <summary>
        /// Finds all small shelves within the given subroom rects.
        /// </summary>
        private static List<Building_Storage> FindSmallShelves(Map map, List<CellRect> subroomRects)
        {
            List<Building_Storage> smallShelves = new List<Building_Storage>();
            foreach (CellRect subroomRect in subroomRects)
            {
                foreach (IntVec3 cell in subroomRect)
                {
                    if (!cell.InBounds(map)) continue;
                    foreach (Thing thing in cell.GetThingList(map))
                    {
                        if (thing.def == CrewQuartersHelpers.ShelfSmallDef && thing is Building_Storage storage)
                        {
                            if (!smallShelves.Contains(storage))
                            {
                                smallShelves.Add(storage);
                            }
                        }
                    }
                }
            }
            return smallShelves;
        }

        /// <summary>
        /// Checks if a shelf has no items stored.
        /// </summary>
        private static bool IsShelfEmpty(Building_Storage shelf, Map map)
        {
            foreach (IntVec3 cell in shelf.AllSlotCellsList())
            {
                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing.def.category == ThingCategory.Item)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region Shelf Contents Rolling

        /// <summary>
        /// Single roll on the shelf contents table.
        /// </summary>
        private static void RollShelfContents(Map map, Building_Storage shelf)
        {
            var (_, action) = ShelfContentsOutcomes.RandomElementByWeight(x => x.weight);
            action(map, shelf);
        }

        #endregion

        #region Shelf Replacements

        /// <summary>
        /// Replaces a shelf with a small bookshelf and adds a random book.
        /// </summary>
        private static void ReplaceShelfWithBookcase(Building_Storage shelf, Map map)
        {
            ThingDef bookshelfDef = DefDatabase<ThingDef>.GetNamed("BookcaseSmall", false);
            if (bookshelfDef == null) return;

            IntVec3 pos = shelf.Position;
            Rot4 rot = shelf.Rotation;
            shelf.Destroy(DestroyMode.Vanish);

            Thing bookshelf = ThingMaker.MakeThing(bookshelfDef, CrewQuartersHelpers.SteelDef);
            GenSpawn.Spawn(bookshelf, pos, map, rot);

            // Add a random book if bookshelf is a bookcase
            if (bookshelf is Building_Bookcase bookcase)
            {
                Thing book = GenerateRandomBook();
                if (book != null)
                {
                    var container = bookcase.GetDirectlyHeldThings();
                    if (container != null && container.CanAcceptAnyOf(book, true))
                    {
                        container.TryAdd(book, true);
                    }
                    else
                    {
                        book.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        /// <summary>
        /// Replaces a shelf with a steel chess table.
        /// </summary>
        private static void TryReplaceWithChessTable(Building_Storage shelf, Map map)
        {
            ThingDef chessTableDef = DefDatabase<ThingDef>.GetNamed("ChessTable", false);
            if (chessTableDef == null) return;
            CrewQuartersHelpers.ReplaceThingAt(shelf, chessTableDef, CrewQuartersHelpers.SteelDef, map);
        }

        /// <summary>
        /// Replaces a shelf with an ancient safe.
        /// </summary>
        private static void TryReplaceWithAncientSafe(Building_Storage shelf, Map map)
        {
            ThingDef ancientSafeDef = DefDatabase<ThingDef>.GetNamed("AncientSafe", false);
            if (ancientSafeDef == null) return;
            CrewQuartersHelpers.ReplaceThingAt(shelf, ancientSafeDef, null, map);
        }

        /// <summary>
        /// Replaces a shelf with an ancient sealed crate.
        /// </summary>
        private static void TryReplaceWithAncientCrate(Building_Storage shelf, Map map)
        {
            ThingDef ancientCrateDef = DefDatabase<ThingDef>.GetNamed("AncientSealedCrate", false);
            if (ancientCrateDef == null) return;
            CrewQuartersHelpers.ReplaceThingAt(shelf, ancientCrateDef, null, map);
        }

        /// <summary>
        /// Replaces a shelf with a small steel sculpture of random quality.
        /// </summary>
        private static void ReplaceShelfWithSculpture(Building_Storage shelf, Map map)
        {
            ThingDef sculptureDef = DefDatabase<ThingDef>.GetNamed("SculptureSmall", false);
            if (sculptureDef == null) return;

            Thing sculpture = CrewQuartersHelpers.ReplaceThingAt(shelf, sculptureDef, CrewQuartersHelpers.SteelDef, map);
            if (sculpture == null) return;

            CompQuality compQuality = sculpture.TryGetComp<CompQuality>();
            if (compQuality == null) return;

            QualityCategory quality = (QualityCategory)Rand.RangeInclusive(
                (int)QualityCategory.Normal, (int)QualityCategory.Excellent);
            compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
        }

        /// <summary>
        /// Spawns a golden cube on the shelf (or replaces shelf with scrap cube if golden cube
        /// already exists on map), and always spawns an additional scrap cube sculpture nearby.
        /// This ensures only one golden cube spawns per map across all CrewQuarters rooms.
        /// </summary>
        private static void SpawnGoldenCubeOrScrapCube(Building_Storage shelf, Map map)
        {
            if (CrewQuartersHelpers.ScrapCubeDef == null) return;

            IntVec3 shelfPos = shelf.Position;

            // Check if a golden cube already exists on this map
            bool goldenCubeExists = CrewQuartersHelpers.GoldenCubeDef != null
                && map.listerThings.ThingsOfDef(CrewQuartersHelpers.GoldenCubeDef).Any();

            if (goldenCubeExists || CrewQuartersHelpers.GoldenCubeDef == null)
            {
                // Golden cube already exists (or def not found) - replace shelf with scrap cube
                CrewQuartersHelpers.ReplaceThingAt(shelf, CrewQuartersHelpers.ScrapCubeDef, null, map);
            }
            else
            {
                // No golden cube on map yet - spawn golden cube on shelf
                Thing goldenCube = ThingMaker.MakeThing(CrewQuartersHelpers.GoldenCubeDef);
                RoomShelfHelper.AddItemToShelf(map, shelf, goldenCube);
            }

            // Always spawn an additional scrap cube nearby
            // Search for nearest meditation spot to replace with scrap cube
            ThingDef meditationSpotDef = DefDatabase<ThingDef>.GetNamed("MeditationSpot", false);
            if (meditationSpotDef != null)
            {
                Thing nearestSpot = GenClosest.ClosestThingReachable(
                    shelfPos,
                    map,
                    ThingRequest.ForDef(meditationSpotDef),
                    PathEndMode.Touch,
                    TraverseParms.For(TraverseMode.PassDoors),
                    maxDistance: 20f);

                if (nearestSpot != null)
                {
                    CrewQuartersHelpers.ReplaceThingAt(nearestSpot, CrewQuartersHelpers.ScrapCubeDef, null, map);
                    return;
                }
            }

            // No meditation spot found - find nearest unoccupied standable tile
            IntVec3 scrapCubePos = FindNearestUnoccupiedTile(shelfPos, map);
            if (scrapCubePos.IsValid)
            {
                Thing scrapCube = ThingMaker.MakeThing(CrewQuartersHelpers.ScrapCubeDef);
                GenSpawn.Spawn(scrapCube, scrapCubePos, map);
            }
        }

        /// <summary>
        /// Finds the nearest unoccupied standable tile from a position.
        /// </summary>
        private static IntVec3 FindNearestUnoccupiedTile(IntVec3 fromPos, Map map)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(fromPos, 15f, true))
            {
                if (!cell.InBounds(map)) continue;
                if (!cell.Standable(map)) continue;

                // Check if tile has any buildings or items
                bool occupied = false;
                foreach (Thing t in cell.GetThingList(map))
                {
                    if (t.def.category == ThingCategory.Building || t.def.category == ThingCategory.Item)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    return cell;
                }
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Replaces a shelf with a crib and spawns a newborn.
        /// Also places baby food on the nearest table if available.
        /// Requires Biotech DLC (Crib def won't exist otherwise).
        /// </summary>
        private static void ReplaceShelfWithCrib(Building_Storage shelf, Map map, Faction faction)
        {
            ThingDef cribDef = DefDatabase<ThingDef>.GetNamed("Crib", false);
            if (cribDef == null) return;

            IntVec3 pos = shelf.Position;
            Rot4 rot = shelf.Rotation;
            shelf.Destroy(DestroyMode.Vanish);

            Thing crib = ThingMaker.MakeThing(cribDef, CrewQuartersHelpers.SteelDef);
            GenSpawn.Spawn(crib, pos, map, rot);

            // Spawn newborn in crib
            if (crib is Building_Bed bed)
            {
                SpawnNewbornInCrib(bed, map, faction);
            }

            // Place baby food on nearest empty table
            SpawnBabyFoodOnNearestTable(pos, map);
        }

        /// <summary>
        /// Spawns a newborn pawn in a crib (similar to Nursery pattern).
        /// </summary>
        private static void SpawnNewbornInCrib(Building_Bed crib, Map map, Faction faction)
        {
            PawnKindDef childKind = DefDatabase<PawnKindDef>.GetNamed("TradersGuild_Child", false);
            if (childKind == null)
            {
                childKind = DefDatabase<PawnKindDef>.GetNamed("TradersGuild_Citizen", false);
            }
            if (childKind == null) return;

            try
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: childKind,
                    faction: faction,
                    context: PawnGenerationContext.NonPlayer,
                    tile: map.Tile,
                    forceGenerateNewPawn: false,
                    allowDead: false,
                    allowDowned: true,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: false,
                    colonistRelationChanceFactor: 0f,
                    allowFood: true,
                    inhabitant: true,
                    developmentalStages: DevelopmentalStage.Newborn,
                    biologicalAgeRange: new FloatRange(0.01f, 0.9f));

                Pawn newborn = PawnGenerator.GeneratePawn(request);

                // Get sleeping position
                IntVec3 sleepPos = RestUtility.GetBedSleepingSlotPosFor(newborn, crib);
                if (sleepPos.InBounds(map))
                {
                    GenSpawn.Spawn(newborn, sleepPos, map);

                    // Assign crib ownership
                    var compAssignable = crib.TryGetComp<CompAssignableToPawn>();
                    compAssignable?.TryAssignPawn(newborn);

                    // Start lying down job
                    Job layDownJob = JobMaker.MakeJob(JobDefOf.LayDownResting, crib);
                    newborn.jobs.StartJob(layDownJob, JobCondition.None, null, resumeCurJobAfterwards: false, cancelBusyStances: true);
                }
                else
                {
                    newborn.Destroy();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Better Traders Guild] ShelfCustomizer: Failed to spawn newborn in crib: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawns a unique weapon with random traits on a shelf.
        /// </summary>
        private static void SpawnUniqueWeaponOnShelf(Map map, Building_Storage shelf, string weaponDefName)
        {
            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamed(weaponDefName, false);
            if (weaponDef == null) return;

            Thing weapon = ThingMaker.MakeThing(weaponDef);

            // Set high quality
            CompQuality qualityComp = weapon.TryGetComp<CompQuality>();
            if (qualityComp != null)
            {
                QualityCategory quality = QualityUtility.GenerateQualitySuper();
                qualityComp.SetQuality(quality, ArtGenerationContext.Outsider);
            }

            // Add random traits if unique weapon
            CompUniqueWeapon uniqueComp = weapon.TryGetComp<CompUniqueWeapon>();
            if (uniqueComp != null)
            {
                // Clear any auto-generated traits
                uniqueComp.TraitsListForReading.Clear();

                // Add random compatible traits (up to 3)
                List<WeaponTraitDef> allTraits = DefDatabase<WeaponTraitDef>.AllDefs.ToList();
                for (int i = 0; i < 3; i++)
                {
                    var compatible = allTraits.Where(t => uniqueComp.CanAddTrait(t)).ToList();
                    if (compatible.Count > 0)
                    {
                        uniqueComp.AddTrait(compatible.RandomElement());
                    }
                }

                // Regenerate name and color
                UniqueWeaponNameColorRegenerator.RegenerateNameAndColor(weapon, uniqueComp);
            }

            // Add to shelf
            if (!RoomShelfHelper.AddItemToShelf(map, shelf, weapon))
            {
                weapon.Destroy(DestroyMode.Vanish);
            }
        }

        /// <summary>
        /// Replaces a shelf with an outfit stand and adds a random apparel set.
        /// </summary>
        private static void ReplaceShelfWithOutfitStand(Building_Storage shelf, Map map)
        {
            ThingDef outfitStandDef = DefDatabase<ThingDef>.GetNamed("Building_OutfitStand", false);
            if (outfitStandDef == null) return;

            IntVec3 pos = shelf.Position;
            Rot4 rot = shelf.Rotation;
            shelf.Destroy(DestroyMode.Vanish);

            Thing standThing = ThingMaker.MakeThing(outfitStandDef);
            GenSpawn.Spawn(standThing, pos, map, rot);

            // Paint with orbital steel color
            ColorDef orbitalSteelColor = DefDatabase<ColorDef>.GetNamed("BTG_OrbitalSteel", false);
            if (orbitalSteelColor != null && standThing is Building building)
            {
                building.ChangePaint(orbitalSteelColor);
            }

            // Add random apparel set
            if (standThing is Building_OutfitStand outfitStand)
            {
                AddRandomApparelSet(outfitStand);
            }
        }

        /// <summary>
        /// Adds a random apparel set to an outfit stand.
        /// </summary>
        private static void AddRandomApparelSet(Building_OutfitStand stand)
        {
            // Build list of available apparel sets
            List<List<(string defName, string stuff)>> apparelSets = new List<List<(string, string)>>
            {
                // Synthread shirt + pants
                new List<(string, string)>
                {
                    ("Apparel_BasicShirt", "Synthread"),
                    ("Apparel_Pants", "Synthread")
                },
                // Power armor set
                new List<(string, string)>
                {
                    ("Apparel_PowerArmor", null),
                    ("Apparel_PowerArmorHelmet", null)
                },
                // Vacsuit set
                new List<(string, string)>
                {
                    ("Apparel_Vacsuit", null),
                    ("Apparel_VacsuitHelmet", null)
                },
                // Recon armor set
                new List<(string, string)>
                {
                    ("Apparel_ArmorRecon", null),
                    ("Apparel_ArmorReconHelmet", null)
                }
            };

            // Add slave harness set if available (Ideology)
            if (CrewQuartersHelpers.SlaveHarnessDef != null)
            {
                apparelSets.Add(new List<(string, string)>
                {
                    ("Apparel_SlaveBodyStrap", "Leather_Panther")
                });
            }

            // Pick random set
            var selectedSet = apparelSets.RandomElement();

            // Spawn each piece
            foreach (var (defName, stuffName) in selectedSet)
            {
                ThingDef apparelDef = DefDatabase<ThingDef>.GetNamed(defName, false);
                if (apparelDef == null) continue;

                ThingDef stuffDef = null;
                if (!string.IsNullOrEmpty(stuffName))
                {
                    stuffDef = DefDatabase<ThingDef>.GetNamed(stuffName, false);
                }
                else if (apparelDef.MadeFromStuff)
                {
                    stuffDef = GenStuff.DefaultStuffFor(apparelDef);
                }

                Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuffDef);

                // Set quality
                CompQuality compQuality = apparel.TryGetComp<CompQuality>();
                if (compQuality != null)
                {
                    QualityCategory quality = (QualityCategory)Rand.RangeInclusive(
                        (int)QualityCategory.Normal, (int)QualityCategory.Excellent);
                    compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
                }

                // Add to outfit stand
                if (!stand.AddApparel(apparel))
                {
                    apparel.Destroy(DestroyMode.Vanish);
                }
            }
        }

        #endregion

        #region Helper Methods

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
        /// Spawns baby food on the nearest empty Table1x2c.
        /// </summary>
        private static void SpawnBabyFoodOnNearestTable(IntVec3 fromPos, Map map)
        {
            ThingDef babyFoodDef = DefDatabase<ThingDef>.GetNamed("BabyFood", false);
            ThingDef tableDef = DefDatabase<ThingDef>.GetNamed("Table1x2c", false);

            if (babyFoodDef == null || tableDef == null) return;

            // Find nearest table using pathfinding
            Thing nearestTable = GenClosest.ClosestThingReachable(
                fromPos,
                map,
                ThingRequest.ForDef(tableDef),
                PathEndMode.Touch,
                TraverseParms.For(TraverseMode.PassDoors),
                maxDistance: 20f);

            if (nearestTable == null) return;

            // Check if table is empty (no items on it)
            bool tableEmpty = true;
            foreach (Thing t in nearestTable.Position.GetThingList(map))
            {
                if (t.def.category == ThingCategory.Item)
                {
                    tableEmpty = false;
                    break;
                }
            }

            if (tableEmpty)
            {
                Thing babyFood = ThingMaker.MakeThing(babyFoodDef);
                babyFood.stackCount = Rand.RangeInclusive(25, 45);
                GenSpawn.Spawn(babyFood, nearestTable.Position, map);
            }
        }

        #endregion
    }
}
