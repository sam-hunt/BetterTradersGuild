using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
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
        // Prefab actual size (4×4) - the content defined in XML
        private const int CRIB_SUBROOM_SIZE = 4;

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

                // Spawn daylilies in decorative plant pots (uses pot's default if null)
                // Lower growth for young/budding appearance appropriate for a nursery
                float potGrowth = Rand.Range(0.25f, 0.65f);
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, null, potGrowth);
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
