using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;

namespace BetterTradersGuild.RoomContents.TransportRoom
{
    /// <summary>
    /// Spawns walls from PlacementCalculator.WallSegment lists with optional centered vac barriers.
    ///
    /// This helper handles:
    /// - Vertical and horizontal wall segments
    /// - VGE mod detection for barrier selection (5x1 VGE_VacBarrierQuintuple vs 1x1 VacBarrier)
    /// - Gap calculation for barrier placement at wall midpoint
    /// - Proper barrier rotation based on wall orientation
    ///
    /// Used by rooms that need procedural walls with atmospheric barriers (e.g., TransportRoom).
    /// </summary>
    public static class VacWallSegmentSpawner
    {
        /// <summary>
        /// Spawns walls from WallSegment list with centered vac barriers.
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="walls">List of wall segments from PlacementCalculator.</param>
        /// <param name="wallDef">ThingDef for wall (defaults to OrbitalAncientFortifiedWall).</param>
        public static void SpawnWallsWithBarriers(Map map, List<WallSegment> walls, ThingDef wallDef = null)
        {
            if (walls == null || walls.Count == 0)
                return;

            wallDef = wallDef ?? ThingDefOf.OrbitalAncientFortifiedWall;

            // Check if VGE is active for barrier selection
            bool vgeActive = DefDatabase<ThingDef>.GetNamedSilentFail("VGE_VacBarrierQuintuple") != null;
            string barrierDefName = vgeActive ? "VGE_VacBarrierQuintuple" : "VacBarrier";
            ThingDef barrierDef = DefDatabase<ThingDef>.GetNamed(barrierDefName, false);
            int barrierSize = vgeActive ? 5 : 1;

            foreach (var wall in walls)
            {
                SpawnSingleWallSegment(map, wall, wallDef, barrierDef, barrierSize);
            }
        }

        /// <summary>
        /// Spawns walls from WallSegment list without barriers (solid walls only).
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="walls">List of wall segments from PlacementCalculator.</param>
        /// <param name="wallDef">ThingDef for wall (defaults to OrbitalAncientFortifiedWall).</param>
        public static void SpawnWallsWithoutBarriers(Map map, List<WallSegment> walls, ThingDef wallDef = null)
        {
            if (walls == null || walls.Count == 0)
                return;

            wallDef = wallDef ?? ThingDefOf.OrbitalAncientFortifiedWall;

            foreach (var wall in walls)
            {
                SpawnSingleWallSegment(map, wall, wallDef, null, 0);
            }
        }

        /// <summary>
        /// Spawns a single wall segment with optional centered barrier.
        /// </summary>
        private static void SpawnSingleWallSegment(
            Map map,
            WallSegment wall,
            ThingDef wallDef,
            ThingDef barrierDef,
            int barrierSize)
        {
            bool isVertical = wall.StartX == wall.EndX;
            int wallLength = isVertical
                ? Math.Abs(wall.EndZ - wall.StartZ) + 1
                : Math.Abs(wall.EndX - wall.StartX) + 1;

            // Calculate barrier center position (midpoint of wall segment)
            int barrierCenterX, barrierCenterZ;
            Rot4 barrierRotation;

            if (isVertical)
            {
                barrierCenterX = wall.StartX;
                barrierCenterZ = (Math.Min(wall.StartZ, wall.EndZ) + Math.Max(wall.StartZ, wall.EndZ)) / 2;
                barrierRotation = Rot4.East;  // Clockwise from North = vertical barrier
            }
            else
            {
                barrierCenterX = (Math.Min(wall.StartX, wall.EndX) + Math.Max(wall.StartX, wall.EndX)) / 2;
                barrierCenterZ = wall.StartZ;
                barrierRotation = Rot4.North;  // No rotation = horizontal barrier
            }

            // Calculate which cells the barrier occupies (for skipping during wall spawn)
            HashSet<int> barrierCellCoords = new HashSet<int>();
            bool canFitBarrier = wallLength >= barrierSize && barrierDef != null;

            if (canFitBarrier)
            {
                int halfSize = barrierSize / 2;
                int barrierCenter = isVertical ? barrierCenterZ : barrierCenterX;
                for (int offset = -halfSize; offset <= halfSize; offset++)
                {
                    // For even-sized barriers (not applicable here but safe), skip center-0
                    if (barrierSize % 2 == 0 && offset == 0) continue;
                    barrierCellCoords.Add(barrierCenter + offset);
                }
            }

            // Spawn walls, skipping barrier cells
            if (isVertical)
            {
                for (int z = Math.Min(wall.StartZ, wall.EndZ); z <= Math.Max(wall.StartZ, wall.EndZ); z++)
                {
                    if (canFitBarrier && barrierCellCoords.Contains(z))
                        continue;  // Skip barrier cells

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
                    if (canFitBarrier && barrierCellCoords.Contains(x))
                        continue;  // Skip barrier cells

                    IntVec3 cell = new IntVec3(x, 0, wall.StartZ);
                    if (cell.InBounds(map) && cell.GetEdifice(map) == null)
                    {
                        Thing wallThing = ThingMaker.MakeThing(wallDef);
                        GenSpawn.Spawn(wallThing, cell, map);
                    }
                }
            }

            // Spawn barrier at center
            if (canFitBarrier)
            {
                IntVec3 barrierPos = new IntVec3(barrierCenterX, 0, barrierCenterZ);
                if (barrierPos.InBounds(map))
                {
                    Thing barrier = ThingMaker.MakeThing(barrierDef);
                    GenSpawn.Spawn(barrier, barrierPos, map, barrierRotation);
                    Log.Message($"[Better Traders Guild] Spawned {barrierDef.defName} at {barrierPos} rotation {barrierRotation}");
                }
            }
        }
    }
}
