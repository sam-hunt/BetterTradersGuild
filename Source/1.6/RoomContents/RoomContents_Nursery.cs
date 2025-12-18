using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using Verse.AI;
using BetterTradersGuild.Helpers.RoomContents;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Nursery rooms (Biotech DLC).
    ///
    /// Spawns a crib subroom prefab with an L-shaped wall configuration
    /// that can be placed in corners (preferred) or along edges (with procedural wall completion).
    ///
    /// Uses the same placement strategy as Commander's Quarters:
    /// - Corner placement (preferred): Uses 2 room walls, no additional walls needed
    /// - Edge placement (fallback): Uses 1 room wall, spawns 1 side wall
    /// - Center placement (last resort): Uses 0 room walls, spawns 2 walls (back + left)
    /// </summary>
    public class RoomContents_Nursery : RoomContentsWorker
    {
        // Prefab actual size (6×6) - the content defined in XML
        private const int CRIB_SUBROOM_SIZE = 6;

        /// <summary>
        /// Weighted developmental stages for young pawn generation.
        /// Slight skew towards younger stages for variety - pure random by age
        /// would give ~77% Child (10-year span) vs ~23% Baby/Newborn (~1 year combined).
        ///
        /// Distribution: ~17% Newborn, ~33% Baby, ~50% Child
        /// </summary>
        private static readonly List<(DevelopmentalStage stage, float weight, FloatRange ageRange)> YoungStageWeights =
            new List<(DevelopmentalStage, float, FloatRange)>
        {
            (DevelopmentalStage.Newborn, 1f, new FloatRange(0.01f, 0.9f)),    // 3-36 days old
            (DevelopmentalStage.Baby, 2f, new FloatRange(1f, 2.8f)),     // 1-3 years
            (DevelopmentalStage.Child, 3f, new FloatRange(3f, 12.8f)),        // 3-13 years
        };

        // Prefab defName for the crib subroom structure
        private const string CRIB_PREFAB_DEFNAME = "BTG_CribSubroom";

        // Stores the crib subroom area to prevent other prefabs from spawning there
        private CellRect cribSubroomRect;

        /// <summary>
        /// Main room generation method. Orchestrates crib subroom placement and calls base class
        /// to process XML-defined content (prefabs, scatter, parts) in remaining space.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Explicitly initialize cribSubroomRect to default (safety mechanism)
            // If placement fails, Width = 0, so IsValidCellBase won't block other prefabs
            this.cribSubroomRect = default;

            // 1. Find best location for crib subroom (prefer corners, avoid walls with doors)
            PlacementResult placement = FindBestPlacementForCribSubroom(room, map);

            if (placement.Type != PlacementType.Invalid)
            {
                // 2. Calculate and store subroom area for validation (prevents other prefab overlap)
                this.cribSubroomRect = GetSubroomRect(placement.Position, placement.Rotation);

                // 3. Spawn crib subroom prefab using PrefabUtility API
                SpawnCribSubroomUsingPrefabAPI(map, placement);

                // 4. Spawn required walls from PlacementCalculator (consolidated wall spawning)
                // PlacementCalculator.RequiredWalls contains all walls needed for this placement type:
                // - Corner: empty list (room walls provide everything)
                // - Edge: one wall segment (left side)
                // - Center: two wall segments (back + left)
                if (placement.RequiredWalls != null && placement.RequiredWalls.Count > 0)
                {
                    SpawnWallsFromSegments(map, placement.RequiredWalls);
                }

                // 5. Spawn civilians sheltering in the nursery (behind blast door)
                // These represent non-combatants who have locked themselves in for safety
                SpawnShelteringCivilians(map, faction, this.cribSubroomRect);

                // 6. Populate nursery shelf with baby food and survival meals
                PopulateNurseryShelf(map, this.cribSubroomRect);
            }
            else
            {
                // Log warning but CONTINUE (other prefabs still spawn for graceful degradation)
                CellRect firstRect = room.rects?.FirstOrDefault() ?? default;
                Log.Warning($"[Better Traders Guild] Could not find valid placement for Crib Subroom in Nursery at {firstRect}");
                // NO RETURN - continue to spawn other room furniture
            }

            // 5. Call base to process XML (prefabs, scatter, parts)
            //    ALWAYS runs - spawns other nursery furniture even if subroom failed
            //    Other prefabs will avoid subroom area if cribSubroomRect.Width > 0
            base.FillRoom(map, room, faction, threatPoints);

            // 6. Post-processing: Spawn daylilies in plant pots
            //    CRITICAL: This must happen AFTER base.FillRoom() since plant pots
            //    are spawned by XML prefabs in base.FillRoom()
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();

                // Spawn daylilies in plant pots
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, null, growth: 1.0f);
            }
        }

        /// <summary>
        /// Override to prevent other prefabs from spawning in crib subroom area.
        ///
        /// CRITICAL: This MUST block placement before spawning occurs. Post-spawn removal
        /// doesn't work because other prefabs overwrite subroom furniture at the same cells,
        /// and removing them afterward leaves the subroom furniture already destroyed.
        ///
        /// Called by base.FillRoom() during prefab placement validation.
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            // Block prefab placement in crib subroom area (prevent furniture overwriting)
            if (this.cribSubroomRect.Width > 0 && this.cribSubroomRect.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Finds the best corner or edge placement for the crib subroom.
        /// Selection strategy: corners (no doors) > edges > center (last resort).
        /// </summary>
        private PlacementResult FindBestPlacementForCribSubroom(LayoutRoom room, Map map)
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

            // Use unified placement algorithm with 4×4 prefab size
            PlacementCalculator.PlacementResult calcResult = PlacementCalculator.CalculateBestPlacement(
                simpleRoom,
                CRIB_SUBROOM_SIZE,
                doors);

            // Convert back to RoomContents PlacementResult
            if (calcResult.Type == PlacementType.Invalid)
            {
                Log.Warning($"[Better Traders Guild] Could not find valid placement for crib subroom in room at {rect}");
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
        /// Calculates the crib subroom blocking area from placement result.
        /// Returns the area that should be reserved to prevent other furniture overlap.
        /// </summary>
        private CellRect GetSubroomRect(IntVec3 center, Rot4 rotation)
        {
            // Get the actual prefab spawn bounds
            var intRotation = (PlacementCalculator.PlacementRotation)rotation.AsInt;
            var prefabBounds = PlacementCalculator.GetPrefabSpawnBounds(
                center.x, center.z, intRotation, CRIB_SUBROOM_SIZE);

            return new CellRect(prefabBounds.MinX, prefabBounds.MinZ, prefabBounds.Width, prefabBounds.Height);
        }

        /// <summary>
        /// Spawns the crib subroom prefab using PrefabUtility API.
        /// The prefab contains the L-shaped walls, door, cribs, and end table.
        ///
        /// IMPORTANT: PrefabUtility.SpawnPrefab() uses CENTER-BASED positioning.
        /// The IntVec3 position parameter specifies the CENTER of the prefab, not the min corner.
        /// </summary>
        private void SpawnCribSubroomUsingPrefabAPI(Map map, PlacementResult placement)
        {
            PrefabDef prefab = DefDatabase<PrefabDef>.GetNamed(CRIB_PREFAB_DEFNAME, true);

            if (prefab == null)
            {
                Log.Error($"[Better Traders Guild] Could not find PrefabDef '{CRIB_PREFAB_DEFNAME}'");
                return;
            }

            // Spawn the prefab at the specified CENTER position with rotation
            PrefabUtility.SpawnPrefab(prefab, map, placement.Position, placement.Rotation, null);
        }

        /// <summary>
        /// Spawns walls from PlacementCalculator.RequiredWalls list.
        /// Handles both vertical and horizontal wall segments by iterating through
        /// the segment coordinates and spawning individual wall cells.
        /// </summary>
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
        /// Spawns civilians (a caretaker and their children) sheltering inside the crib subroom.
        /// These represent non-combatants who have locked themselves behind the blast door.
        ///
        /// Spawn counts:
        /// - 1 TradersGuild_Citizen (caretaker/parent)
        /// - 2-4 young pawns (Newborn/Baby/Child mix, requires Biotech DLC)
        ///
        /// Placement strategy:
        /// - Newborns and Babies are placed in cribs with a LayDown job
        /// - Children (who can walk) are spawned at standable cells
        /// - Caretaker is spawned at a standable cell
        ///
        /// Each young pawn is assigned the caretaker as their parent for realism.
        /// </summary>
        private void SpawnShelteringCivilians(Map map, Faction faction, CellRect subroomRect)
        {
            // Get standable cells inside the subroom (avoid walls, doors, furniture)
            List<IntVec3> standableCells = subroomRect.Cells
                .Where(c => c.InBounds(map) && c.Standable(map))
                .ToList();

            if (standableCells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No standable cells in nursery subroom for civilians.");
                return;
            }

            // Find available cribs in the subroom (beds sized for babies)
            List<Building_Bed> availableCribs = subroomRect.Cells
                .Where(c => c.InBounds(map))
                .SelectMany(c => c.GetThingList(map))
                .OfType<Building_Bed>()
                .Where(bed => bed.ForHumanBabies && bed.AnyUnownedSleepingSlot)
                .Distinct()
                .ToList();

            // Determine spawn counts
            int childCount = Rand.RangeInclusive(2, 4);

            // Get PawnKindDefs
            PawnKindDef citizenKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("TradersGuild_Citizen");
            PawnKindDef childKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("TradersGuild_Child");

            int spawned = 0;
            int placedInCribs = 0;
            Pawn caretaker = null;

            // Spawn caretaker (parent of the children)
            if (citizenKind != null && standableCells.Count > 0)
            {
                caretaker = GenerateCivilian(citizenKind, faction, map.Tile);
                if (caretaker != null)
                {
                    IntVec3 cell = standableCells.RandomElement();
                    GenSpawn.Spawn(caretaker, cell, map);
                    standableCells.Remove(cell);
                    spawned++;
                }
            }

            // Spawn young pawns (Newborn/Baby/Child) - requires Biotech DLC
            // Each child is assigned the caretaker as their parent
            if (childKind != null)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Pawn youngPawn = GenerateYoungPawn(childKind, faction, map.Tile);
                    if (youngPawn == null)
                        continue;

                    // Assign caretaker as parent for realism
                    if (caretaker != null)
                    {
                        youngPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, caretaker);
                    }

                    // Check if this is a newborn or baby (can't walk - should be in crib)
                    bool needsCrib = youngPawn.DevelopmentalStage == DevelopmentalStage.Newborn ||
                                     youngPawn.DevelopmentalStage == DevelopmentalStage.Baby;

                    if (needsCrib && availableCribs.Count > 0)
                    {
                        // Place in crib
                        if (TrySpawnPawnInCrib(youngPawn, availableCribs, map))
                        {
                            spawned++;
                            placedInCribs++;
                            continue;
                        }
                        // If crib placement failed, fall through to standable cell
                    }

                    // Spawn at standable cell (for children who can walk, or if no cribs available)
                    if (standableCells.Count > 0)
                    {
                        IntVec3 cell = standableCells.RandomElement();
                        GenSpawn.Spawn(youngPawn, cell, map);
                        standableCells.Remove(cell);
                        spawned++;
                    }
                    else
                    {
                        // No space left - destroy the pawn to avoid orphaned pawns
                        youngPawn.Destroy();
                    }
                }
            }

            if (spawned > 0)
            {
                string cribInfo = placedInCribs > 0 ? $" ({placedInCribs} in cribs)" : "";
                Log.Message($"[Better Traders Guild] Spawned {spawned} civilians sheltering in nursery subroom{cribInfo}.");
            }
        }

        /// <summary>
        /// Attempts to spawn a pawn in an available crib with a LayDown job.
        /// The pawn is spawned at the crib's sleeping position and given a resting job.
        /// </summary>
        /// <param name="pawn">The pawn to place in the crib (should be a newborn or baby)</param>
        /// <param name="availableCribs">List of cribs with available slots (will be modified)</param>
        /// <param name="map">The map to spawn on</param>
        /// <returns>True if successfully placed in a crib, false otherwise</returns>
        private bool TrySpawnPawnInCrib(Pawn pawn, List<Building_Bed> availableCribs, Map map)
        {
            if (availableCribs.Count == 0)
                return false;

            // Select a random available crib
            Building_Bed crib = availableCribs.RandomElement();

            // Get the sleeping position for this crib
            IntVec3 sleepPos = RestUtility.GetBedSleepingSlotPosFor(pawn, crib);

            // Verify the position is valid
            if (!sleepPos.InBounds(map))
            {
                Log.Warning($"[Better Traders Guild] Crib sleeping position {sleepPos} is out of bounds.");
                return false;
            }

            // Spawn the pawn at the sleeping position
            GenSpawn.Spawn(pawn, sleepPos, map);

            // Assign the crib to this pawn (ownership)
            var compAssignable = crib.TryGetComp<CompAssignableToPawn>();
            compAssignable?.TryAssignPawn(pawn);

            // Start a LayDown job so the pawn appears to be lying in the crib
            Job layDownJob = JobMaker.MakeJob(JobDefOf.LayDownResting, crib);
            pawn.jobs.StartJob(layDownJob, JobCondition.None, null, resumeCurJobAfterwards: false, cancelBusyStances: true);

            // Remove crib from available list if it no longer has free slots
            if (!crib.AnyUnownedSleepingSlot)
            {
                availableCribs.Remove(crib);
            }

            return true;
        }

        /// <summary>
        /// Populates the nursery shelf with baby food and packaged survival meals.
        /// Uses RoomShelfHelper to find and fill shelves in the subroom.
        ///
        /// Contents:
        /// - 30-50 baby food (for infants)
        /// - 12-20 packaged survival meals in two stacks (max stack size is 10)
        /// </summary>
        private void PopulateNurseryShelf(Map map, CellRect subroomRect)
        {
            // Find shelves in the subroom
            List<Building_Storage> shelves = RoomShelfHelper.GetShelvesInRoom(map, subroomRect, "Shelf", null);

            if (shelves.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No shelves found in nursery subroom for food storage.");
                return;
            }

            int itemsAdded = 0;

            // Add baby food (30-50 units)
            int babyFoodCount = Rand.RangeInclusive(30, 50);
            Thing babyFood = RoomShelfHelper.AddItemsToShelf(map, shelves[0], "BabyFood", babyFoodCount, setForbidden: true);
            if (babyFood != null)
            {
                itemsAdded++;
            }

            // Add packaged survival meals in two stacks (max stack size is 10)
            // Stack 1: Full stack of 10
            Thing meals1 = RoomShelfHelper.AddItemsToShelf(map, shelves[0], "MealSurvivalPack", 10, setForbidden: true);
            if (meals1 != null)
            {
                itemsAdded++;
            }
            // Stack 2: Partial stack of 2-10
            int partialMealCount = Rand.RangeInclusive(2, 10);
            Thing meals2 = RoomShelfHelper.AddItemsToShelf(map, shelves[0], "MealSurvivalPack", partialMealCount, setForbidden: true);
            if (meals2 != null)
            {
                itemsAdded++;
            }

            if (itemsAdded > 0)
            {
                Log.Message($"[Better Traders Guild] Stocked nursery shelf with {babyFoodCount} baby food and {10 + partialMealCount} survival meals.");
            }
        }

        /// <summary>
        /// Generates a single adult civilian pawn for the TradersGuild faction.
        /// </summary>
        private Pawn GenerateCivilian(PawnKindDef kindDef, Faction faction, int tile)
        {
            if (kindDef == null || faction == null)
                return null;

            try
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: kindDef,
                    faction: faction,
                    context: PawnGenerationContext.NonPlayer,
                    tile: tile,
                    forceGenerateNewPawn: false,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: false,
                    colonistRelationChanceFactor: 0f,
                    forceAddFreeWarmLayerIfNeeded: false,
                    allowGay: true,
                    allowPregnant: true,
                    allowFood: true,
                    allowAddictions: true,
                    inhabitant: true
                );

                return PawnGenerator.GeneratePawn(request);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Better Traders Guild] Failed to generate civilian ({kindDef.defName}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a young pawn (Newborn, Baby, or Child) for the TradersGuild faction.
        /// Uses weighted random selection to provide variety while slightly favoring younger stages.
        ///
        /// Must explicitly specify DevelopmentalStage since the PawnKindDef's
        /// pawnGroupDevelopmentStage field only applies to group generation, not individual spawns.
        /// </summary>
        private Pawn GenerateYoungPawn(PawnKindDef kindDef, Faction faction, int tile)
        {
            if (kindDef == null || faction == null)
                return null;

            try
            {
                // Select developmental stage with weighted random
                var stageChoice = YoungStageWeights.RandomElementByWeight(x => x.weight);

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: kindDef,
                    faction: faction,
                    context: PawnGenerationContext.NonPlayer,
                    tile: tile,
                    forceGenerateNewPawn: false,
                    allowDead: false,
                    allowDowned: true,
                    canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: false,
                    colonistRelationChanceFactor: 0f,
                    forceAddFreeWarmLayerIfNeeded: false,
                    allowGay: true,
                    allowPregnant: false,
                    allowFood: true,
                    allowAddictions: false,
                    inhabitant: true,
                    developmentalStages: stageChoice.stage,
                    biologicalAgeRange: stageChoice.ageRange
                );

                return PawnGenerator.GeneratePawn(request);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Better Traders Guild] Failed to generate young pawn ({kindDef.defName}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Container for crib subroom placement results.
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
