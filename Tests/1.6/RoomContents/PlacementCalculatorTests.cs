using System.Collections.Generic;
using Xunit;
using BetterTradersGuild.Helpers.RoomContents;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;
using PlacementRotation = BetterTradersGuild.Helpers.RoomContents.PlacementCalculator.PlacementRotation;

namespace BetterTradersGuild.Tests.RoomContents
{
    /// <summary>
    /// Tests for bedroom placement calculation logic.
    /// Each test includes a visual diagram showing the room layout.
    /// </summary>
    public class PlacementCalculatorTests
    {
        #region NW Corner Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (3,6), rotation 0 (North - door faces south ↓)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
         *   z=8  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
         *   z=7  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
         *   z=6  ■ ↓ ↓ C ↓ ↓ ↓ . . . . . ■  ← Prefab center
         *   z=5  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . D
         *   z=4  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
         *   z=3  D . . . . . . . . . . . ■
         *   z=2  ■ . . . . . . . . . . . ■
         *   z=1  ■ . . . . . . . . . . . ■
         *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void NW_Corner_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 12, Z = 5 },  // NE, SE, E blocked
                    new DoorPosition { X = 6, Z = 0 },  // SW, SE, S blocked
                    new DoorPosition { X = 7, Z = 10 },  // NW blocked
                    new DoorPosition { X = 0, Z = 3 },  // SW blocked
                }
            );
            Assert.Equal(PlacementType.Corner, result.Type);
            Assert.Equal(PlacementRotation.North, result.Rotation);
            Assert.Equal(3, result.CenterX);
            Assert.Equal(6, result.CenterZ);
        }

        #endregion

        #region NE Corner Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (8,7), rotation 1 (East - door faces west ←)
         *
         *  z=10  ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . . . ← ← ← ← ← ← ■
         *   z=8  ■ . . . . . ← ← ← ← ← ← ■
         *   z=7  ■ . . . . . ← ← C ← ← ← ■  ← Prefab center
         *   z=6  ■ . . . . . ← ← ← ← ← ← ■
         *   z=5  D . . . . . ← ← ← ← ← ← ■
         *   z=4  ■ . . . . . ← ← ← ← ← ← ■
         *   z=3  ■ . . . . . . . . . . . D
         *   z=2  ■ . . . . . . . . . . . ■
         *   z=1  ■ . . . . . . . . . . . ■
         *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void NE_Corner_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 0, Z = 5 },   // NW, SW, W blocked
                    new DoorPosition { X = 6, Z = 0 },    // SW, SE, S blocked
                    new DoorPosition { X = 5, Z = 10 },    // NW blocked
                    new DoorPosition { X = 12, Z = 3 },    // SE blocked
                }
            );
            Assert.Equal(PlacementType.Corner, result.Type);
            Assert.Equal(PlacementRotation.East, result.Rotation);
            Assert.Equal(8, result.CenterX);
            Assert.Equal(7, result.CenterZ);
        }

        #endregion

        #region SE Corner Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (9,4), rotation 2 (South - door faces north ↑)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . . . . . . . . . ■
         *   z=8  ■ . . . . . . . . . . . ■
         *   z=7  ■ . . . . . . . . . . . ■
         *   z=6  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
         *   z=5  D . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
         *   z=4  ■ . . . . . ↑ ↑ ↑ C ↑ ↑ ■  ← Prefab center
         *   z=3  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
         *   z=2  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
         *   z=1  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
         *   z=0  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void SE_Corner_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 6, Z = 10 },   // NW, NE corners blocked
                    new DoorPosition { X = 0, Z = 5 }    // NE, SE corners blocked
                }
            );
            Assert.Equal(PlacementType.Corner, result.Type);
            Assert.Equal(PlacementRotation.South, result.Rotation);
            Assert.Equal(9, result.CenterX);
            Assert.Equal(4, result.CenterZ);
        }

        #endregion

        #region SW Corner Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (4,3), rotation 3 (West - door faces east →)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . . . . . . . . . ■
         *   z=8  ■ . . . . . . . . . . . ■
         *   z=7  ■ . . . . . . . . . . . ■
         *   z=6  ■ → → → → → → . . . . . ■
         *   z=5  ■ → → → → → → . . . . . D
         *   z=4  ■ → → → → → → . . . . . ■
         *   z=3  ■ → → → C → → . . . . . ■  ← Prefab center
         *   z=2  ■ → → → → → → . . . . . ■
         *   z=1  ■ → → → → → → . . . . . ■
         *   z=0  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void SW_Corner_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 6, Z = 10 },   // NW, NE corners blocked
                    new DoorPosition { X = 12, Z = 5 },  // NE, SE corners blocked
                }
            );
            Assert.Equal(PlacementType.Corner, result.Type);
            Assert.Equal(PlacementRotation.West, result.Rotation);
            Assert.Equal(4, result.CenterX);
            Assert.Equal(3, result.CenterZ);
        }

        #endregion

        #region NE Edge Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (5,6), rotation 0 (North - door faces south ↓)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■ ■ ■  ← North wall
         *   z=9  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
         *   z=8  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
         *   z=7  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
         *   z=6  ■ . W ↓ ↓ C ↓ ↓ ↓ . . . ■  ← Prefab center
         *   z=5  D . W ↓ ↓ ↓ ↓ ↓ ↓ . . . D
         *   z=4  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
         *   z=3  ■ . . . . . . . . . . . ■
         *   z=2  ■ . . . . . . . . . . . ■
         *   z=1  ■ . . . . . . . . . . . ■
         *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void NE_Edge_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 9, Z = 10 },  // NE corner blocked
                    new DoorPosition { X = 6, Z = 0 },  // SW, SE corners blocked
                    new DoorPosition { X = 0, Z = 5 },  // NW corner, W edge blocked
                    new DoorPosition { X = 12, Z = 5 },  // E edge blocked
                }
            );

            Assert.Equal(PlacementType.Edge, result.Type);
            Assert.Equal(PlacementRotation.North, result.Rotation);
            Assert.Equal(5, result.CenterX);
            Assert.Equal(6, result.CenterZ);

            Assert.Single(result.RequiredWalls);
            AssertWallSegment(result.RequiredWalls[0], startX: 2, startZ: 4, endX: 2, endZ: 9);
        }

        #endregion

        #region NW Edge Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (7,6), rotation 0 (North - door faces south ↓)
         *
         *  z=10  ■ ■ ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
         *   z=8  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
         *   z=7  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
         *   z=6  ■ . . . W ↓ ↓ C ↓ ↓ ↓ . ■  ← Prefab center
         *   z=5  D . . . W ↓ ↓ ↓ ↓ ↓ ↓ . D
         *   z=4  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
         *   z=3  ■ . . . . . . . . . . . ■
         *   z=2  ■ . . . . . . . . . . . ■
         *   z=1  ■ . . . . . . . . . . . ■
         *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void NW_Edge_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 3, Z = 10 },   // NW corner blocked
                    new DoorPosition { X = 6, Z = 0 },  // SW, SE corners blocked
                    new DoorPosition { X = 0, Z = 5 },  // W edge blocked
                    new DoorPosition { X = 12, Z = 5 }  // E edge blocked
                }
            );
            Assert.Equal(PlacementType.Edge, result.Type);
            Assert.Equal(PlacementRotation.North, result.Rotation);
            Assert.Equal(7, result.CenterX);
            Assert.Equal(6, result.CenterZ);

            Assert.Single(result.RequiredWalls);
            AssertWallSegment(result.RequiredWalls[0], startX: 4, startZ: 4, endX: 4, endZ: 9);
        }

        #endregion

        #region E Edge Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (8,5), rotation 1 (East - door faces west ←)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . . . . . . . . . D
         *   z=8  ■ . . . . . W W W W W W ■
         *   z=7  ■ . . . . . ← ← ← ← ← ← ■
         *   z=6  ■ . . . . . ← ← ← ← ← ← ■
         *   z=5  D . . . . . ← ← C ← ← ← ■  ← Prefab center
         *   z=4  ■ . . . . . ← ← ← ← ← ← ■
         *   z=3  ■ . . . . . ← ← ← ← ← ← ■
         *   z=2  ■ . . . . . ← ← ← ← ← ← ■
         *   z=1  ■ . . . . . . . . . . . D
         *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void E_Edge_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 12, Z = 9 },  // NE corner blocked
                    new DoorPosition { X = 12, Z = 1 },  // SE corner blocked
                    new DoorPosition { X = 0, Z = 5 },  // NW, SW corners blocked
                    new DoorPosition { X = 6, Z = 10 },  // N edge blocked
                    new DoorPosition { X = 6, Z = 0 },  // S edge blocked
                }
            );
            Assert.Equal(PlacementType.Edge, result.Type);
            Assert.Equal(PlacementRotation.East, result.Rotation);
            Assert.Equal(8, result.CenterX);
            Assert.Equal(5, result.CenterZ);

            Assert.Single(result.RequiredWalls);
            AssertWallSegment(result.RequiredWalls[0], startX: 6, startZ: 8, endX: 11, endZ: 8);
        }

        #endregion

        #region SE Edge Placement


        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (7,4), rotation 2 (South - door faces north ↑)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . . . . . . . . . ■
         *   z=8  ■ . . . . . . . . . . . ■
         *   z=7  ■ . . . . . . . . . . . ■
         *   z=6  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ W . ■
         *   z=5  D . . . ↑ ↑ ↑ ↑ ↑ ↑ W . D
         *   z=4  ■ . . . ↑ ↑ ↑ C ↑ ↑ W . ■  ← Prefab center
         *   z=3  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ W . ■
         *   z=2  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ W . ■
         *   z=1  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ W . ■
         *   z=0  ■ ■ ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void SE_Edge_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 6, Z = 10 },   // NW, NE corners blocked
                    new DoorPosition { X = 3, Z = 0 },    // SW corner blocked
                    new DoorPosition { X = 0, Z = 5 },    // W edge blocked
                    new DoorPosition { X = 12, Z = 5 },    // E edge blocked
                }
            );
            Assert.Equal(PlacementType.Edge, result.Type);
            Assert.Equal(PlacementRotation.South, result.Rotation);
            Assert.Equal(7, result.CenterX);
            Assert.Equal(4, result.CenterZ);

            Assert.Single(result.RequiredWalls);
            AssertWallSegment(result.RequiredWalls[0], startX: 10, startZ: 1, endX: 10, endZ: 6);
        }

        #endregion

        #region SW Edge Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (5,4), rotation 2 (South - door faces north ↑)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . . . . . . . . . ■
         *   z=8  ■ . . . . . . . . . . . ■
         *   z=7  ■ . . . . . . . . . . . ■
         *   z=6  ■ . ↑ ↑ ↑ ↑ ↑ ↑ W . . . ■
         *   z=5  D . ↑ ↑ ↑ ↑ ↑ ↑ W . . . D
         *   z=4  ■ . ↑ ↑ ↑ C ↑ ↑ W . . . ■  ← Prefab center
         *   z=3  ■ . ↑ ↑ ↑ ↑ ↑ ↑ W . . . ■
         *   z=2  ■ . ↑ ↑ ↑ ↑ ↑ ↑ W . . . ■
         *   z=1  ■ . ↑ ↑ ↑ ↑ ↑ ↑ W . . . ■
         *   z=0  ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void SW_Edge_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 6, Z = 10 },   // NW, NE, N blocked
                    new DoorPosition { X = 12, Z = 5 },  // E blocked
                    new DoorPosition { X = 9, Z = 0 },  // SE blocked
                    new DoorPosition { X = 0, Z = 5 },    // W blocked
                }
            );
            Assert.Equal(PlacementType.Edge, result.Type);
            Assert.Equal(PlacementRotation.South, result.Rotation);
            Assert.Equal(5, result.CenterX);
            Assert.Equal(4, result.CenterZ);

            Assert.Single(result.RequiredWalls);
            AssertWallSegment(result.RequiredWalls[0], startX: 8, startZ: 1, endX: 8, endZ: 6);
        }

        #endregion

        #region W Edge Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (4,5), rotation 3 (West - door faces east →)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . . . . . . . . . ■
         *   z=8  ■ → → → → → → . . . . . ■
         *   z=7  ■ → → → → → → . . . . . ■
         *   z=6  ■ → → → → → → . . . . . ■
         *   z=5  ■ → → → C → → . . . . . D  ← Prefab center
         *   z=4  ■ → → → → → → . . . . . ■
         *   z=3  ■ → → → → → → . . . . . ■
         *   z=2  ■ W W W W W W . . . . . ■
         *   z=1  ■ . . . . . . . . . . . ■
         *   z=0  ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void W_Edge_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 6, Z = 10 },  // N blocked
                    new DoorPosition { X = 12, Z = 5 },  // NE, SE, E blocked
                    new DoorPosition { X = 5, Z = 0 },  // S blocked
                }
            );
            Assert.Equal(PlacementType.Edge, result.Type);
            Assert.Equal(PlacementRotation.West, result.Rotation);
            Assert.Equal(4, result.CenterX);
            Assert.Equal(5, result.CenterZ);

            Assert.Single(result.RequiredWalls);
            AssertWallSegment(result.RequiredWalls[0], startX: 1, startZ: 2, endX: 6, endZ: 2);
        }

        #endregion

        #region Center Placement

        /*  <!-- DIAGRAM_START -->
         *  Visual: 13x11 room (0,0 to 12,10)
         *  Prefab: 6×6 at center (5,4), rotation 0 (North - door faces south ↓)
         *
         *  z=10  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← North wall
         *   z=9  ■ . . . . . . . . . . . ■
         *   z=8  ■ . W W W W W W W . . . ■
         *   z=7  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
         *   z=6  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
         *   z=5  D . W ↓ ↓ ↓ ↓ ↓ ↓ . . . D
         *   z=4  ■ . W ↓ ↓ C ↓ ↓ ↓ . . . ■  ← Prefab center
         *   z=3  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
         *   z=2  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
         *   z=1  ■ . . . . . . . . . . . ■
         *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12
         *
         *  Generated: 2025-11-23 00:03:16
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void Center_PlacesCorrectly()
        {
            var result = CalculateBestPlacement(
                new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
                prefabSize: 6,
                doors: new List<DoorPosition>
                {
                    new DoorPosition { X = 6, Z = 10 }, // N edge blocked
                    new DoorPosition { X = 12, Z = 5 }, // E edge blocked
                    new DoorPosition { X = 6, Z = 0 },  // S edge blocked
                    new DoorPosition { X = 0, Z = 5 }  // W edge blocked
                }
            );
            Assert.Equal(PlacementType.Center, result.Type);
            Assert.Equal(PlacementRotation.North, result.Rotation);
            Assert.Equal(5, result.CenterX);
            Assert.Equal(4, result.CenterZ);

            AssertWallSegment(result.RequiredWalls[0], startX: 2, startZ: 2, endX: 2, endZ: 7);
            AssertWallSegment(result.RequiredWalls[1], startX: 2, startZ: 8, endX: 8, endZ: 8);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method for asserting wall segment coordinates in a compact, parseable format.
        /// Makes test assertions easier to read and simpler for RegenerateDiagrams to parse.
        /// </summary>
        private static void AssertWallSegment(WallSegment wall, int startX, int startZ, int endX, int endZ)
        {
            Assert.Equal(startX, wall.StartX);
            Assert.Equal(startZ, wall.StartZ);
            Assert.Equal(endX, wall.EndX);
            Assert.Equal(endZ, wall.EndZ);
        }

        #endregion
    }
}
