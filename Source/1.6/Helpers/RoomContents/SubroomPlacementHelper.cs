using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Shared result type for subroom placement in RoomContentsWorkers.
    /// Adapts PlacementCalculator's pure types to RimWorld-specific types (IntVec3, Rot4).
    ///
    /// This struct is the RimWorld-specific counterpart to PlacementCalculator.PlacementResult,
    /// providing the same data using game engine types for direct use in spawn methods.
    /// </summary>
    public struct SubroomPlacementResult
    {
        public IntVec3 Position;
        public Rot4 Rotation;
        public PlacementType Type;
        public List<WallSegment> RequiredWalls;

        public bool IsValid => Type != PlacementType.Invalid;

        public static SubroomPlacementResult Invalid =>
            new SubroomPlacementResult { Type = PlacementType.Invalid };

        /// <summary>
        /// Converts a PlacementCalculator result to the RimWorld-specific type.
        /// </summary>
        public static SubroomPlacementResult FromCalculatorResult(PlacementCalculator.PlacementResult calcResult)
        {
            if (calcResult.Type == PlacementType.Invalid)
                return Invalid;

            return new SubroomPlacementResult
            {
                Position = new IntVec3(calcResult.CenterX, 0, calcResult.CenterZ),
                Rotation = calcResult.Rotation.AsRot4(),
                Type = calcResult.Type,
                RequiredWalls = calcResult.RequiredWalls
            };
        }
    }

    /// <summary>
    /// Helper class providing RimWorld-specific adapters for subroom placement operations.
    ///
    /// This class bridges the gap between PlacementCalculator (pure, testable logic)
    /// and RoomContentsWorker implementations (RimWorld-dependent). It consolidates
    /// common patterns used across multiple room types (Commander's Quarters, Nursery,
    /// Computer Room, etc.) without modifying PlacementCalculator's interface.
    ///
    /// Design rationale:
    /// - PlacementCalculator remains pure and fully unit-tested
    /// - RimWorld-specific conversions are centralized here
    /// - RoomContentsWorkers become simpler, focusing on room-specific logic
    /// </summary>
    public static class SubroomPlacementHelper
    {
        /// <summary>
        /// Finds the best placement for a subroom prefab within a LayoutRoom.
        ///
        /// Consolidates the common pattern of:
        /// 1. Getting edge blockers (doors) from the room
        /// 2. Converting RimWorld CellRect to PlacementCalculator's SimpleRect
        /// 3. Calling PlacementCalculator.CalculateBestPlacement
        /// 4. Converting the result back to RimWorld types
        /// </summary>
        /// <param name="room">The LayoutRoom to place the subroom in</param>
        /// <param name="map">The map being generated</param>
        /// <param name="prefabSize">Size of the subroom prefab (e.g., 6 for 6×6)</param>
        /// <returns>Placement result with RimWorld-specific types, or Invalid if no placement found</returns>
        public static SubroomPlacementResult FindBestPlacement(LayoutRoom room, Map map, int prefabSize)
        {
            if (room.rects == null || room.rects.Count == 0)
                return SubroomPlacementResult.Invalid;

            // Get edge blockers (doors) from the room
            List<DoorPosition> doors = RoomEdgeBlockersHelper.GetEdgeBlockers(room, map);

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
                prefabSize,
                doors);

            return SubroomPlacementResult.FromCalculatorResult(calcResult);
        }

        /// <summary>
        /// Calculates the blocking rectangle for a subroom placement.
        ///
        /// Returns the area that should be reserved to prevent other prefabs from
        /// overlapping with the subroom. Used in IsValidCellBase overrides.
        /// </summary>
        /// <param name="center">Center position of the subroom</param>
        /// <param name="rotation">Rotation of the subroom</param>
        /// <param name="prefabSize">Size of the subroom prefab</param>
        /// <returns>CellRect representing the blocked area</returns>
        public static CellRect GetBlockingRect(IntVec3 center, Rot4 rotation, int prefabSize)
        {
            var intRotation = (PlacementRotation)rotation.AsInt;
            var prefabBounds = PlacementCalculator.GetPrefabSpawnBounds(
                center.x, center.z, intRotation, prefabSize);

            return new CellRect(prefabBounds.MinX, prefabBounds.MinZ, prefabBounds.Width, prefabBounds.Height);
        }

        /// <summary>
        /// Spawns walls from PlacementCalculator wall segments.
        ///
        /// Handles both vertical and horizontal wall segments by iterating through
        /// the segment coordinates and spawning individual wall cells. Skips cells
        /// that already have an edifice (wall, door, etc.) to avoid overwriting.
        /// </summary>
        /// <param name="map">Map to spawn walls on</param>
        /// <param name="walls">List of wall segments from PlacementCalculator</param>
        /// <param name="wallDef">ThingDef for the wall type (defaults to OrbitalAncientFortifiedWall)</param>
        public static void SpawnWalls(Map map, List<WallSegment> walls, ThingDef wallDef = null)
        {
            if (walls == null || walls.Count == 0)
                return;

            wallDef = wallDef ?? ThingDefOf.OrbitalAncientFortifiedWall;

            foreach (var wall in walls)
            {
                if (wall.StartX == wall.EndX)  // Vertical wall
                {
                    for (int z = Math.Min(wall.StartZ, wall.EndZ); z <= Math.Max(wall.StartZ, wall.EndZ); z++)
                    {
                        SpawnWallCell(map, new IntVec3(wall.StartX, 0, z), wallDef);
                    }
                }
                else  // Horizontal wall
                {
                    for (int x = Math.Min(wall.StartX, wall.EndX); x <= Math.Max(wall.StartX, wall.EndX); x++)
                    {
                        SpawnWallCell(map, new IntVec3(x, 0, wall.StartZ), wallDef);
                    }
                }
            }
        }

        /// <summary>
        /// Spawns a single wall cell if the position is valid and empty.
        /// </summary>
        private static void SpawnWallCell(Map map, IntVec3 cell, ThingDef wallDef)
        {
            if (cell.InBounds(map) && cell.GetEdifice(map) == null)
            {
                Thing wallThing = ThingMaker.MakeThing(wallDef);
                GenSpawn.Spawn(wallThing, cell, map);
            }
        }
    }
}
