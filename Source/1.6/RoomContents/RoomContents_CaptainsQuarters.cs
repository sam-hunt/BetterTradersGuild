using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using UnityEngine;
using BetterTradersGuild.Helpers.RoomContents;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;

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
        private const int BEDROOM_PREFAB_SIZE = 6;

        // Semantic bedroom size (7×7) - includes conceptual space for missing walls
        // The prefab is 6×6, but occupies 7×7 when accounting for room walls it uses
        private const int BEDROOM_SIZE = 7;

        // Prefab defName for the L-shaped bedroom structure
        private const string BEDROOM_PREFAB_DEFNAME = "BTG_CaptainsBedroom";

        // NOTE: The offset formulas (using BEDROOM_PREFAB_SIZE/2 and BEDROOM_PREFAB_SIZE/2+1) are empirically
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
            // Explicitly initialize bedroomRect to default (safety mechanism)
            // If bedroom placement fails, Width = 0, so IsValidCellBase won't block lounge prefabs
            this.bedroomRect = default;

            // 1. Find best location for bedroom (prefer corners, avoid walls with doors)
            PlacementResult placement = FindBestPlacementForBedroom(room, map);

            if (placement.Type != PlacementType.Invalid)
            {
                // 2. Calculate and store bedroom area for validation (prevents lounge overlap)
                this.bedroomRect = GetBedroomRect(placement.Position, placement.Rotation);

                // 3. Spawn bedroom prefab using PrefabUtility API
                SpawnBedroomUsingPrefabAPI(map, placement);

                // 4. Spawn unique weapon on shelf in bedroom
                SpawnUniqueWeaponOnShelf(map, placement, room);

                // 5. Spawn required walls from PlacementCalculator (consolidated wall spawning)
                // PlacementCalculator.RequiredWalls contains all walls needed for this placement type:
                // - Corner: empty list (room walls provide everything)
                // - Edge: one wall segment (left side)
                // - Center: two wall segments (back + left)
                if (placement.RequiredWalls != null && placement.RequiredWalls.Count > 0)
                {
                    SpawnWallsFromSegments(map, placement.RequiredWalls);
                }
            }
            else
            {
                // Log warning but CONTINUE (lounge still spawns for graceful degradation)
                CellRect firstRect = room.rects?.FirstOrDefault() ?? default;
                Log.Warning($"[Better Traders Guild] Could not find valid placement for Captain's bedroom in room at {firstRect}");
                // NO RETURN - continue to spawn lounge furniture
            }

            // 6. Call base to process XML (prefabs, scatter, parts)
            //    ALWAYS runs - spawns lounge even if bedroom failed
            //    Lounge prefabs will avoid bedroom area if bedroomRect.Width > 0
            base.FillRoom(map, room, faction, threatPoints);

            // 7. Post-processing: Fix bookcase contents and spawn plants
            //    CRITICAL: This must happen AFTER base.FillRoom() since lounge
            //    bookshelves are spawned by base.FillRoom()
            //    ALWAYS runs - fixes books even if bedroom placement failed
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                RoomBookcaseHelper.InsertBooksIntoBookcases(map, roomRect);

                // 8. Spawn decorative plants (roses) in all plant pots
                ThingDef rosePlant = DefDatabase<ThingDef>.GetNamed("Plant_Rose", false);
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, rosePlant, growth: 1.0f);
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
                return new PlacementResult { Type = PlacementType.Invalid };

            // Get door positions from the room
            List<DoorPosition> doors = RoomDoorsHelper.GetDoorPositions(room, map);

            // Convert CellRect to SimpleRect
            CellRect rect = room.rects.First();
            SimpleRect simpleRoom = new SimpleRect
            {
                MinX = rect.minX,
                MinZ = rect.minZ,
                Width = rect.Width,
                Height = rect.Height
            };

            // Use unified placement algorithm
            PlacementCalculator.PlacementResult calcResult = PlacementCalculator.CalculateBestPlacement(
                simpleRoom,
                BEDROOM_PREFAB_SIZE,
                doors);

            // Convert back to RoomContents PlacementResult
            if (calcResult.Type == PlacementType.Invalid)
            {
                Log.Warning($"[Better Traders Guild] Could not find valid placement for bedroom in room at {rect}");
                return new PlacementResult { Type = PlacementType.Invalid };
            }

            return new PlacementResult
            {
                Position = new IntVec3(calcResult.CenterX, 0, calcResult.CenterZ),
                Rotation = calcResult.Rotation.AsRot4(),
                Type = calcResult.Type,
                RequiredWalls = calcResult.RequiredWalls
            };
        }


        /// <summary>
        /// Calculates the bedroom blocking area from placement result.
        /// Returns the area that should be reserved to prevent lounge furniture overlap.
        /// For corner placements: returns prefab bounds (room walls provide the rest).
        /// For edge/center: returns prefab bounds + extra cell(s) for spawned walls.
        /// </summary>
        private CellRect GetBedroomRect(IntVec3 center, Rot4 rotation)
        {
            // Get the actual prefab spawn bounds
            var intRotation = (PlacementCalculator.PlacementRotation)rotation.AsInt;
            var prefabBounds = PlacementCalculator.GetPrefabSpawnBounds(
                center.x, center.z, intRotation, BEDROOM_PREFAB_SIZE);

            // For now, just return the prefab bounds
            // TODO: Expand this for edge/center placements when they spawn additional walls
            return new CellRect(prefabBounds.MinX, prefabBounds.MinZ, prefabBounds.Width, prefabBounds.Height);
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
        /// Spawns walls from PlacementCalculator.RequiredWalls list.
        /// Handles both vertical and horizontal wall segments by iterating through
        /// the segment coordinates and spawning individual wall cells.
        /// </summary>
        /// <param name="map">Map to spawn walls on</param>
        /// <param name="walls">List of wall segments from PlacementCalculator</param>
        private void SpawnWallsFromSegments(Map map, List<PlacementCalculator.WallSegment> walls)
        {
            ThingDef wallDef = ThingDefOf.OrbitalAncientFortifiedWall;

            foreach (var wall in walls)
            {
                // Iterate through wall segment
                if (wall.StartX == wall.EndX)  // Vertical wall
                {
                    for (int z = Math.Min(wall.StartZ, wall.EndZ); z <= Math.Max(wall.StartZ, wall.EndZ); z++)
                    {
                        IntVec3 cell = new IntVec3(wall.StartX, 0, z);
                        if (cell.InBounds(map) && cell.GetEdifice(map) == null)
                        {
                            Thing wallThing = ThingMaker.MakeThing(wallDef);
                            GenSpawn.Spawn(wallThing, cell, map);
                        }
                    }
                }
                else  // Horizontal wall
                {
                    for (int x = Math.Min(wall.StartX, wall.EndX); x <= Math.Max(wall.StartX, wall.EndX); x++)
                    {
                        IntVec3 cell = new IntVec3(x, 0, wall.StartZ);
                        if (cell.InBounds(map) && cell.GetEdifice(map) == null)
                        {
                            Thing wallThing = ThingMaker.MakeThing(wallDef);
                            GenSpawn.Spawn(wallThing, cell, map);
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

                    if (shelf != null)
                    {
                        // Spawn weapon at the shelf's position
                        GenSpawn.Spawn(weapon, shelf.Position, map, Rot4.North);
                        break;
                    }
                }
            }

            if (shelf == null)
            {
                Log.Warning($"[Better Traders Guild] Could not find ShelfSmall in captain's quarters - spawning weapon at bedroom center");
                GenSpawn.Spawn(weapon, this.bedroomRect.CenterCell, map, Rot4.North);
            }

            weapon.SetForbidden(true, false);
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
            public PlacementType Type;
            public List<PlacementCalculator.WallSegment> RequiredWalls;
        }
    }
}
