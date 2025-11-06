using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Captain's Quarters.
    ///
    /// Spawns a secure bedroom subroom with an L-shaped prefab (front + right side walls only)
    /// that can be placed in corners (preferred) or along edges (with procedural wall completion).
    ///
    /// LEARNING NOTE: RoomContentsWorkers provide programmatic control over room generation,
    /// working alongside XML definitions. The three-phase system (PreFillRooms, FillRoom, PostFillRooms)
    /// allows custom structures to coexist with XML-defined prefabs, scatter items, and parts.
    /// </summary>
    public class RoomContents_CaptainsQuarters : RoomContentsWorker
    {
        // Prefab actual size (6×6) - the content defined in XML
        private const int PREFAB_SIZE = 6;

        // Semantic bedroom size (7×7) - includes conceptual space for missing walls
        // The prefab is 6×6, but occupies 7×7 when accounting for room walls it uses
        private const int BEDROOM_SIZE = 7;

        // Prefab defName for the L-shaped bedroom structure
        private const string BEDROOM_PREFAB_DEFNAME = "BTG_CaptainsBedroom";

        // NOTE: The offset formulas (using PREFAB_SIZE/2 and PREFAB_SIZE/2+1) are empirically
        // derived for the 6×6 bedroom prefab. Testing with a 5×5 prefab showed these formulas
        // are not easily generalizable - they appear to be specific to each prefab size/rotation.

        // Stores the 7x7 bedroom area to prevent other prefabs from spawning there
        private CellRect bedroomRect;

        /// <summary>
        /// Main room generation method. Orchestrates bedroom placement and calls base class
        /// to process XML-defined content (prefabs, scatter, parts) in remaining space.
        ///
        /// LEARNING NOTE: Call base.FillRoom() AFTER custom structure spawning to allow
        /// XML prefabs to spawn in the remaining valid space (controlled via IsValidCellBase).
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Find best location for bedroom (prefer corners, avoid walls with doors)
            PlacementResult placement = FindBestPlacementForBedroom(room, map);

            if (!placement.IsValid)
            {
                CellRect firstRect = room.rects?.FirstOrDefault() ?? default;
                Log.Warning($"[Better Traders Guild] Could not find valid placement for Captain's bedroom in room at {firstRect}");
                base.FillRoom(map, room, faction, threatPoints);
                return;
            }

            // 2. Calculate and store bedroom area for validation (MUST be before spawning prefab!)
            this.bedroomRect = GetBedroomRect(placement.Position, placement.Rotation);

            // 3. Spawn bedroom prefab using PrefabUtility API
            SpawnBedroomUsingPrefabAPI(map, placement);

            // 3.5. Spawn unique weapon on shelf in bedroom
            SpawnUniqueWeaponOnShelf(map, placement, room);

            // 4. Spawn missing walls based on placement type
            if (placement.IsCenter)
            {
                // Center placement: spawn BOTH missing walls (back + left side)
                // Prefab has front + right side, we need to complete all 4 walls
                SpawnMissingWallsForCenter(map, placement);
            }
            else if (!placement.IsCorner)
            {
                // Edge placement: spawn missing left side wall only
                // Back wall is provided by room edge
                SpawnMissingSideWall(map, placement);
            }
            // Corner placement: no additional walls needed (room provides back + left)

            // 5. Call base to process XML (prefabs, scatter, parts in remaining space)
            //    Lounge prefabs will avoid bedroom due to IsValidCellBase override
            base.FillRoom(map, room, faction, threatPoints);

            // 6. Fix bookcase contents (move books from map into innerContainer)
            //    CRITICAL: This must happen AFTER base.FillRoom() since the lounge
            //    bookshelves are spawned by base.FillRoom()
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                FixBookcaseContents(map, roomRect);
            }
        }

        /// <summary>
        /// Override to prevent lounge prefabs from spawning in bedroom area.
        ///
        /// CRITICAL: This MUST block placement before spawning occurs. Post-spawn removal
        /// doesn't work because lounge prefabs overwrite bedroom furniture at the same cells,
        /// and removing them afterward leaves the bedroom furniture already destroyed.
        ///
        /// Called by base.FillRoom() during prefab placement validation.
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            // Block lounge prefab placement in bedroom area (prevent furniture overwriting)
            if (this.bedroomRect.Width > 0 && this.bedroomRect.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Finds the best corner or edge placement for the bedroom.
        /// Selection strategy: corners (no doors) > edges > center (last resort).
        /// </summary>
        private PlacementResult FindBestPlacementForBedroom(LayoutRoom room, Map map)
        {
            if (room.rects == null || room.rects.Count == 0)
                return default;

            CellRect rect = room.rects.First();

            if (rect.Width < BEDROOM_SIZE || rect.Height < BEDROOM_SIZE)
            {
                Log.Warning($"[Better Traders Guild] Room too small for bedroom: {rect.Width}x{rect.Height}, need {BEDROOM_SIZE}x{BEDROOM_SIZE}");
                return default;
            }

            // TODO: Phase 1 - Corner selection with door validation
            // 1. Try all 4 corners (NW, NE, SE, SW)
            // 2. For each corner: check if bedroom walls would overlap with room doors
            // 3. Pick first valid corner (no door conflicts)
            // 4. If multiple valid, prefer corners with best spacing/aesthetics

            // TODO: Phase 2 - Edge fallback
            // 1. If all corners have doors, try 4 edge placements (North, East, South, West)
            // 2. Position bedroom centered along edge, back against room wall
            // 3. Spawn missing side wall procedurally (SpawnMissingSideWall)

            // TODO: Phase 3 - Center fallback (last resort)
            // 1. If all edges also fail, place in center of room
            // 2. Spawn both missing walls (SpawnMissingWallsForCenter)

            // TEMPORARY: Hardcode NE corner for testing
            PlacementResult result = CalculateNECornerPlacement(rect);

            return result;
        }

        /// <summary>
        /// Calculates bedroom placement for NW (top-left) corner.
        /// Door faces South, missing walls on North+West.
        /// NOTE: PrefabUtility.SpawnPrefab() uses CENTER-BASED positioning for the 6×6 prefab.
        /// </summary>
        private PlacementResult CalculateNWCornerPlacement(CellRect rect)
        {
            IntVec3 center = new IntVec3(rect.minX + PREFAB_SIZE / 2, 0, (rect.maxZ - 1) - PREFAB_SIZE / 2);

            return new PlacementResult
            {
                Position = center,
                Rotation = Rot4.North,
                IsCorner = true,
                IsCenter = false,
                IsValid = true
            };
        }

        /// <summary>
        /// Calculates bedroom placement for NE (top-right) corner.
        /// Door faces West, missing walls on North+East.
        /// </summary>
        private PlacementResult CalculateNECornerPlacement(CellRect rect)
        {
            IntVec3 center = new IntVec3(rect.maxX - (PREFAB_SIZE / 2 + 1), 0, rect.maxZ - PREFAB_SIZE / 2);

            return new PlacementResult
            {
                Position = center,
                Rotation = Rot4.East,
                IsCorner = true,
                IsCenter = false,
                IsValid = true
            };
        }

        /// <summary>
        /// Calculates bedroom placement for SE (bottom-right) corner.
        /// Door faces North, missing walls on South+East.
        /// </summary>
        private PlacementResult CalculateSECornerPlacement(CellRect rect)
        {
            IntVec3 center = new IntVec3(rect.maxX - PREFAB_SIZE / 2, 0, rect.minZ + (PREFAB_SIZE / 2 + 1));

            return new PlacementResult
            {
                Position = center,
                Rotation = Rot4.South,
                IsCorner = true,
                IsCenter = false,
                IsValid = true
            };
        }

        /// <summary>
        /// Calculates bedroom placement for SW (bottom-left) corner.
        /// Door faces East, missing walls on South+West.
        /// </summary>
        private PlacementResult CalculateSWCornerPlacement(CellRect rect)
        {
            IntVec3 center = new IntVec3(rect.minX + (PREFAB_SIZE / 2 + 1), 0, rect.minZ + PREFAB_SIZE / 2);

            return new PlacementResult
            {
                Position = center,
                Rotation = Rot4.West,
                IsCorner = true,
                IsCenter = false,
                IsValid = true
            };
        }

        // TODO: Implement edge placement methods
        // Edge placement positions bedroom along one wall, centered on that edge
        // Requires spawning missing side wall procedurally
        // Format: CalculateNorthEdgePlacement(), CalculateEastEdgePlacement(), etc.

        // TODO: Implement center placement method
        // Center placement is last resort when all corners and edges have door conflicts
        // Requires spawning both missing walls (back + left side)
        // Format: CalculateCenterPlacement()

        /// <summary>
        /// Calculates the 7×7 semantic bedroom area rectangle from center position.
        /// The semantic area includes the room walls that the bedroom uses.
        /// </summary>
        private CellRect GetBedroomRect(IntVec3 center, Rot4 rotation)
        {
            // Convert center to semantic min corner based on rotation
            // For corners, the semantic min depends on which walls are missing
            int halfSize = PREFAB_SIZE / 2;  // = 3

            switch (rotation.AsInt)
            {
                case 0: // North (NW corner): missing walls on North+West
                    return new CellRect(center.x - halfSize, center.z - halfSize, BEDROOM_SIZE, BEDROOM_SIZE);

                case 1: // East (NE corner): missing walls on North+East
                    return new CellRect(center.x - halfSize - 1, center.z - halfSize, BEDROOM_SIZE, BEDROOM_SIZE);

                case 2: // South (SE corner): missing walls on South+East
                    return new CellRect(center.x - halfSize, center.z - halfSize - 1, BEDROOM_SIZE, BEDROOM_SIZE);

                case 3: // West (SW corner): missing walls on South+West
                    return new CellRect(center.x - halfSize - 1, center.z - halfSize, BEDROOM_SIZE, BEDROOM_SIZE);

                default:
                    return new CellRect(center.x - halfSize, center.z - halfSize, BEDROOM_SIZE, BEDROOM_SIZE);
            }
        }

        /// <summary>
        /// Spawns the bedroom prefab using PrefabUtility API.
        /// The prefab contains the L-shaped walls (front + right side), furniture, and AncientBlastDoor.
        ///
        /// LEARNING NOTE: PrefabUtility.SpawnPrefab() uses CENTER-BASED positioning!
        /// The IntVec3 position parameter specifies the CENTER of the prefab, not the min corner.
        /// For a 6×6 prefab, the center is at (localX=3, localZ=3), and the prefab extends
        /// ±3 cells in each direction from that center point.
        /// </summary>
        private void SpawnBedroomUsingPrefabAPI(Map map, PlacementResult placement)
        {
            PrefabDef prefab = DefDatabase<PrefabDef>.GetNamed(BEDROOM_PREFAB_DEFNAME, true);

            if (prefab == null)
            {
                Log.Error($"[Better Traders Guild] Could not find PrefabDef '{BEDROOM_PREFAB_DEFNAME}'");
                return;
            }

            // Spawn the prefab at the specified CENTER position with rotation
            // IMPORTANT: placement.Position is the CENTER of the 6×6 prefab, not the min corner!
            PrefabUtility.SpawnPrefab(prefab, map, placement.Position, placement.Rotation, null);
        }

        /// <summary>
        /// Spawns the missing left side wall for edge placements.
        /// Corner placements don't need this because the room's corner walls provide both missing walls.
        /// </summary>
        private void SpawnMissingSideWall(Map map, PlacementResult placement)
        {
            // Calculate which wall is missing based on rotation
            // The L-shaped prefab includes front (z=0) and right side (x=6)
            // Missing: back (z=6) and left side (x=0)

            // For edge placement:
            // - Back wall is provided by room edge
            // - Left side wall needs to be spawned

            List<IntVec3> wallCells = new List<IntVec3>();

            // Calculate left side wall positions (x=0, z=1 to z=5 in local coordinates)
            // These need to be transformed by rotation and offset

            for (int z = 1; z <= BEDROOM_SIZE - 2; z++)
            {
                IntVec3 localPos = new IntVec3(0, 0, z);
                IntVec3 worldPos = placement.Position + localPos.RotatedBy(placement.Rotation);
                wallCells.Add(worldPos);
            }

            // Spawn walls
            ThingDef wallDef = ThingDefOf.OrbitalAncientFortifiedWall;
            foreach (IntVec3 cell in wallCells)
            {
                Thing wall = ThingMaker.MakeThing(wallDef);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        /// <summary>
        /// Spawns both missing walls for center placements.
        /// Center placements don't have any room walls to rely on, so we need to spawn:
        /// - Back wall (z=6 in local coordinates)
        /// - Left side wall (x=0 in local coordinates)
        /// Combined with prefab's front (z=0) and right side (x=6), this completes all 4 walls.
        /// </summary>
        private void SpawnMissingWallsForCenter(Map map, PlacementResult placement)
        {
            List<IntVec3> wallCells = new List<IntVec3>();
            ThingDef wallDef = ThingDefOf.OrbitalAncientFortifiedWall;

            // Back wall: z=6, x=1 to x=5 (in local coordinates)
            for (int x = 1; x <= BEDROOM_SIZE - 2; x++)
            {
                IntVec3 localPos = new IntVec3(x, 0, BEDROOM_SIZE - 1);
                IntVec3 worldPos = placement.Position + localPos.RotatedBy(placement.Rotation);
                wallCells.Add(worldPos);
            }

            // Left side wall: x=0, z=1 to z=5 (in local coordinates)
            for (int z = 1; z <= BEDROOM_SIZE - 2; z++)
            {
                IntVec3 localPos = new IntVec3(0, 0, z);
                IntVec3 worldPos = placement.Position + localPos.RotatedBy(placement.Rotation);
                wallCells.Add(worldPos);
            }

            // Spawn all walls
            foreach (IntVec3 cell in wallCells)
            {
                Thing wall = ThingMaker.MakeThing(wallDef);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        /// <summary>
        /// Fixes bookcase contents by moving books from map into innerContainer.
        ///
        /// LEARNING NOTE: Vanilla PrefabUtility.SpawnPrefab() spawns items at cell positions
        /// using GenSpawn.Spawn(), which does NOT automatically insert items into containers.
        /// This affects all IThingHolder containers (bookcases, shelves, crates, etc).
        ///
        /// This post-spawn fixup finds books spawned at the same position as bookcases and
        /// properly inserts them into the bookcase's innerContainer for correct rendering
        /// and interaction mechanics.
        /// </summary>
        private void FixBookcaseContents(Map map, CellRect searchArea)
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

        /// <summary>
        /// Spawns a unique weapon on the shelf in the captain's quarters.
        /// Searches for the ShelfSmall within the entire room area (bedroom + lounge).
        ///
        /// LEARNING NOTE: The shelf is in the bedroom prefab, but we search the full room
        /// to be robust. Also, we must add at least one trait BEFORE GoldInlay since it
        /// has canGenerateAlone="false" which prevents it from being the only trait.
        /// </summary>
        private void SpawnUniqueWeaponOnShelf(Map map, PlacementResult placement, LayoutRoom room)
        {
            // Generate the unique weapon
            Thing weapon = GenerateCaptainsWeapon();

            if (weapon == null)
            {
                Log.Warning("[Better Traders Guild] Failed to generate captain's unique weapon");
                return;
            }

            // Find the ShelfSmall within the entire room area (not just bedroom)
            Building_Storage shelf = null;
            int shelvesFound = 0;

            // Search full room rect (bedroom + lounge)
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                foreach (IntVec3 cell in roomRect.Cells)
                {
                    if (!cell.InBounds(map)) continue;

                    List<Thing> things = cell.GetThingList(map);
                    if (things != null)
                    {
                        foreach (Thing thing in things)
                        {
                            if (thing.def.defName == "ShelfSmall" && thing is Building_Storage storage)
                            {
                                shelvesFound++;
                                shelf = storage;
                                break;
                            }
                        }
                    }

                    if (shelf != null) break;
                }
            }

            if (shelf == null)
            {
                Log.Warning($"[Better Traders Guild] Could not find ShelfSmall in captain's quarters - spawning weapon at bedroom center");
                GenSpawn.Spawn(weapon, this.bedroomRect.CenterCell, map, Rot4.North);
                return;
            }

            // Spawn weapon at the shelf's position
            GenSpawn.Spawn(weapon, shelf.Position, map, Rot4.North);
        }

        /// <summary>
        /// Generates a unique high-quality weapon with gold inlay for the captain's bedroom.
        ///
        /// Weapon selection (weighted random):
        /// - 30% Revolver (Gold + HighPowerRounds + random)
        /// - 30% Charge Rifle (Gold + ChargeCapacitor + random)
        /// - 20% Charge Lance (Gold + ChargeCapacitor + random)
        /// - 20% Beam Repeater (Gold + FrequencyAmplifier + random)
        ///
        /// All weapons spawn with Excellent/Masterwork/Legendary quality via QualityUtility.GenerateQualitySuper()
        /// and have 3 unique traits (Gold Inlay + weapon-specific + random compatible).
        /// </summary>
        private Thing GenerateCaptainsWeapon()
        {
            // Weighted random weapon selection
            float roll = Rand.Value;
            string weaponDefName;
            if (roll < 0.3f)
                weaponDefName = "Gun_Revolver_Unique";
            else if (roll < 0.6f)
                weaponDefName = "Gun_ChargeRifle_Unique";
            else if (roll < 0.8f)
                weaponDefName = "Gun_ChargeLance_Unique";
            else
                weaponDefName = "Gun_BeamRepeater_Unique";

            // Create weapon with correct _Unique variant
            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamed(weaponDefName, false);
            if (weaponDef == null)
            {
                Log.Error($"[Better Traders Guild] Could not find ThingDef '{weaponDefName}'");
                return null;
            }

            Thing weapon = ThingMaker.MakeThing(weaponDef, null);

            // Set quality using Super Generator (biased toward high quality)
            CompQuality qualityComp = weapon.TryGetComp<CompQuality>();
            if (qualityComp != null)
            {
                QualityCategory quality = QualityUtility.GenerateQualitySuper();
                qualityComp.SetQuality(quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
            }

            // Add weapon traits
            CompUniqueWeapon uniqueComp = weapon.TryGetComp<CompUniqueWeapon>();
            if (uniqueComp != null)
            {
                // CRITICAL: Clear any randomly-generated traits that were added during ThingMaker.MakeThing()
                // This prevents ending up with 6+ traits (3 random + 3 ours)
                // Following vanilla pattern from DebugToolsMisc.RemoveTraitFromUniqueWeapon
                uniqueComp.TraitsListForReading.Clear();

                // Trait 1: Weapon-specific primary trait (MUST be added FIRST)
                // GoldInlay has canGenerateAlone="false" so it needs another trait to exist first
                string primaryTraitName;
                switch (weaponDefName)
                {
                    case "Gun_Revolver_Unique":
                        primaryTraitName = "PulseCharger";  // Retrofits pulse-charge tech
                        break;
                    case "Gun_ChargeRifle_Unique":
                        primaryTraitName = "ChargeCapacitor";
                        break;
                    case "Gun_ChargeLance_Unique":
                        primaryTraitName = "ChargeCapacitor";
                        break;
                    case "Gun_BeamRepeater_Unique":
                        primaryTraitName = "FrequencyAmplifier";
                        break;
                    default:
                        primaryTraitName = "AimAssistance";
                        break;
                }

                WeaponTraitDef primaryTrait = DefDatabase<WeaponTraitDef>.GetNamed(primaryTraitName, false);
                if (primaryTrait != null && uniqueComp.CanAddTrait(primaryTrait))
                {
                    uniqueComp.AddTrait(primaryTrait);
                }
                else
                {
                    Log.Warning($"[Better Traders Guild] Could not add {primaryTraitName} trait to captain's weapon");
                }

                // Trait 2: Gold Inlay (added SECOND after primary trait exists)
                WeaponTraitDef goldTrait = DefDatabase<WeaponTraitDef>.GetNamed("GoldInlay", false);
                if (goldTrait != null && uniqueComp.CanAddTrait(goldTrait))
                {
                    uniqueComp.AddTrait(goldTrait);
                }
                else
                {
                    Log.Warning("[Better Traders Guild] Could not add GoldInlay trait to captain's weapon");
                }

                // Trait 3: Random compatible third trait
                List<WeaponTraitDef> compatibleTraits = DefDatabase<WeaponTraitDef>.AllDefs
                    .Where(traitDef => uniqueComp.CanAddTrait(traitDef))
                    .ToList();

                if (compatibleTraits.Count > 0)
                {
                    WeaponTraitDef randomTrait = compatibleTraits.RandomElement();
                    uniqueComp.AddTrait(randomTrait);
                }
                else
                {
                    Log.Warning("[Better Traders Guild] No compatible traits available for third trait slot");
                }

                // CRITICAL: Regenerate weapon name and color based on new traits
                // PostPostMake() cannot be used here because it has an early return guard
                // in InitializeTraits() that prevents regeneration when traits already exist.
                // Instead, use reflection-based helper to set name/color fields directly.
                UniqueWeaponNameColorRegenerator.RegenerateNameAndColor(weapon, uniqueComp);
            }
            else
            {
                Log.Warning($"[Better Traders Guild] Weapon '{weaponDefName}' does not have CompUniqueWeapon");
            }

            return weapon;
        }

        /// <summary>
        /// Container for bedroom placement results.
        /// </summary>
        private struct PlacementResult
        {
            public IntVec3 Position;
            public Rot4 Rotation;
            public bool IsCorner;
            public bool IsCenter;  // True if placed in center (spawn all 4 walls)
            public bool IsValid;
        }
    }
}
