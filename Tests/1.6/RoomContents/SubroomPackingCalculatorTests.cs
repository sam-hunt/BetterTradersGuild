using System.Collections.Generic;
using System.Linq;
using Xunit;
using BetterTradersGuild.Helpers.RoomContents;
using BetterTradersGuild.RoomContents.CrewQuarters;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;
using static BetterTradersGuild.RoomContents.CrewQuarters.SubroomPackingCalculator;

namespace BetterTradersGuild.Tests.RoomContents
{
    /// <summary>
    /// Tests for subroom packing calculation logic.
    /// Each test includes a visual diagram showing the room layout.
    /// </summary>
    public class SubroomPackingCalculatorTests
    {
        #region Full Room Test - 20x17

        /*  <!-- DIAGRAM_START -->
         *  Visual: 20x17 room (0,0 to 19,16)
         *  Doors: West (0,2), South (10,0), North (7,16), East (19,8)
         *
         *  z=16  ■ ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  North wall
         *  z=15  ■ T T T T T . . . T T T T T T T T T T ■  ┐
         *  z=14  ■ T T T T T . . . T T T T T T T T T T ■  │ Top strip (depth 4)
         *  z=13  ■ T T T T T . . . T T T T T T T T T T ■  │ faces SOUTH ↓
         *  z=12  ■ T T T T T . . . T T T T T T T T T T ■  ┘
         *  z=11  ■ C C C C C C C C C C C C C C C C C C ■  ┐ Upper corridor
         *  z=10  ■ C C C C C C C C C C C C C C C C C C ■  ┘ (2 cells)
         *  z=9   ■ M M M M M M M M M M M M M M M M . . ■  ┐
         *  z=8   ■ M M M M M M M M M M M M M M M M . . D  │ Middle strip (depth 4)
         *  z=7   ■ M M M M M M M M M M M M M M M M . . ■  │ faces SOUTH ↓
         *  z=6   ■ M M M M M M M M M M M M M M M M . . ■  ┘
         *  z=5   ■ C C C C C C C C C C C C C C C C C C ■  Lower corridor (1 cell)
         *  z=4   ■ . . B B B B B B . . . B B B B B B B ■  ┐
         *  z=3   ■ . . B B B B B B . . . B B B B B B B ■  │ Bottom strip (depth 4)
         *  z=2   D . . B B B B B B . . . B B B B B B B ■  │ faces NORTH ↑
         *  z=1   ■ . . B B B B B B . . . B B B B B B B ■  ┘
         *  z=0   ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■  South wall
         *      x=0 1 2 3 4 5 6 7 8 9 10  12  14  16  18
         *
         *  Legend: T=Top subroom area, M=Middle subroom area, B=Bottom subroom area
         *          C=Corridor, .=Exclusion zone, ■=Wall, D=Door
         *
         *  Expected strips:
         *    - Top strip (z=12-15, depth 4, faces South)
         *    - Upper corridor (z=10-11, depth 2)
         *    - Middle strip (z=6-9, depth 4, faces South)
         *    - Lower corridor (z=5, depth 1)
         *    - Bottom strip (z=1-4, depth 4, faces North)
         *
         *  Expected exclusion zones:
         *    - Top strip: x=6-8 (north door at x=7)
         *    - Middle strip: x=17-18 (east door at z=8)
         *    - Bottom strip: x=1-2 (west door at z=2), x=9-11 (south door at x=10)
         *
         *  Expected usable regions:
         *    - Top strip: x=1-5 (5 cells), x=9-18 (10 cells)
         *    - Middle strip: x=1-16 (16 cells)
         *    - Bottom strip: x=3-8 (6 cells), x=12-18 (7 cells)
         *
         *  <!-- DIAGRAM_END -->
        */
        [Fact]
        public void Room20x17_CalculatesCorrectStrips()
        {
            var input = CreateTestInput();
            var result = Calculate(input);

            // Verify we got 5 strips (3 subroom + 2 corridor)
            Assert.Equal(5, result.Strips.Count);

            // Verify strip types and positions
            var strips = result.Strips;

            // Top strip - doors face SOUTH (toward corridor below)
            // PlacementRotation.North = Rot4.North = no rotation = door at z=0 = faces south
            Assert.Equal(StripType.Subroom, strips[0].Type);
            Assert.Equal(12, strips[0].MinZ);
            Assert.Equal(15, strips[0].MaxZ);
            Assert.Equal(PlacementRotation.North, strips[0].Facing);

            // Upper corridor
            Assert.Equal(StripType.Corridor, strips[1].Type);
            Assert.Equal(10, strips[1].MinZ);
            Assert.Equal(11, strips[1].MaxZ);

            // Middle strip - doors face SOUTH (toward corridor below)
            Assert.Equal(StripType.Subroom, strips[2].Type);
            Assert.Equal(6, strips[2].MinZ);
            Assert.Equal(9, strips[2].MaxZ);
            Assert.Equal(PlacementRotation.North, strips[2].Facing);

            // Lower corridor
            Assert.Equal(StripType.Corridor, strips[3].Type);
            Assert.Equal(5, strips[3].MinZ);
            Assert.Equal(5, strips[3].MaxZ);

            // Bottom strip - doors face NORTH (toward corridor above)
            // PlacementRotation.South = Rot4.South = 180° rotation = door at z=max = faces north
            Assert.Equal(StripType.Subroom, strips[4].Type);
            Assert.Equal(1, strips[4].MinZ);
            Assert.Equal(4, strips[4].MaxZ);
            Assert.Equal(PlacementRotation.South, strips[4].Facing);
        }

        [Fact]
        public void Room20x17_CalculatesCorrectRegions_TopStrip()
        {
            var input = CreateTestInput();
            var result = Calculate(input);

            var topStrip = result.Strips[0];
            Assert.Equal(3, topStrip.Regions.Count);

            // Region 1: x=1-5 (usable, 5 cells)
            Assert.Equal(1, topStrip.Regions[0].MinX);
            Assert.Equal(5, topStrip.Regions[0].MaxX);
            Assert.False(topStrip.Regions[0].IsExclusionZone);

            // Region 2: x=6-8 (exclusion, north door)
            Assert.Equal(6, topStrip.Regions[1].MinX);
            Assert.Equal(8, topStrip.Regions[1].MaxX);
            Assert.True(topStrip.Regions[1].IsExclusionZone);

            // Region 3: x=9-18 (usable, 10 cells)
            Assert.Equal(9, topStrip.Regions[2].MinX);
            Assert.Equal(18, topStrip.Regions[2].MaxX);
            Assert.False(topStrip.Regions[2].IsExclusionZone);
        }

        [Fact]
        public void Room20x17_CalculatesCorrectRegions_MiddleStrip()
        {
            var input = CreateTestInput();
            var result = Calculate(input);

            var middleStrip = result.Strips[2];
            Assert.Equal(2, middleStrip.Regions.Count);

            // Region 1: x=1-16 (usable, 16 cells)
            Assert.Equal(1, middleStrip.Regions[0].MinX);
            Assert.Equal(16, middleStrip.Regions[0].MaxX);
            Assert.False(middleStrip.Regions[0].IsExclusionZone);

            // Region 2: x=17-18 (exclusion, east door)
            Assert.Equal(17, middleStrip.Regions[1].MinX);
            Assert.Equal(18, middleStrip.Regions[1].MaxX);
            Assert.True(middleStrip.Regions[1].IsExclusionZone);
        }

        [Fact]
        public void Room20x17_CalculatesCorrectRegions_BottomStrip()
        {
            var input = CreateTestInput();
            var result = Calculate(input);

            var bottomStrip = result.Strips[4];
            Assert.Equal(4, bottomStrip.Regions.Count);

            // Region 1: x=1-2 (exclusion, west door)
            Assert.Equal(1, bottomStrip.Regions[0].MinX);
            Assert.Equal(2, bottomStrip.Regions[0].MaxX);
            Assert.True(bottomStrip.Regions[0].IsExclusionZone);

            // Region 2: x=3-8 (usable, 6 cells)
            Assert.Equal(3, bottomStrip.Regions[1].MinX);
            Assert.Equal(8, bottomStrip.Regions[1].MaxX);
            Assert.False(bottomStrip.Regions[1].IsExclusionZone);

            // Region 3: x=9-11 (exclusion, south door)
            Assert.Equal(9, bottomStrip.Regions[2].MinX);
            Assert.Equal(11, bottomStrip.Regions[2].MaxX);
            Assert.True(bottomStrip.Regions[2].IsExclusionZone);

            // Region 4: x=12-18 (usable, 7 cells)
            Assert.Equal(12, bottomStrip.Regions[3].MinX);
            Assert.Equal(18, bottomStrip.Regions[3].MaxX);
            Assert.False(bottomStrip.Regions[3].IsExclusionZone);
        }

        [Fact]
        public void Room20x17_FitsSubroomsCorrectly()
        {
            var input = CreateTestInput();
            var result = Calculate(input);

            // Verify we have subrooms
            Assert.NotEmpty(result.Subrooms);

            // Verify all subrooms have valid prefab names
            foreach (var subroom in result.Subrooms)
            {
                Assert.StartsWith("BTG_CrewBedSubroom", subroom.PrefabDefName);
                Assert.True(subroom.Width >= 3 && subroom.Width <= 4);
                Assert.Equal(4, subroom.Depth); // All strips are depth 4
            }

            // Verify subroom placements don't overlap
            for (int i = 0; i < result.Subrooms.Count; i++)
            {
                for (int j = i + 1; j < result.Subrooms.Count; j++)
                {
                    var a = result.Subrooms[i];
                    var b = result.Subrooms[j];

                    // Only check non-overlap within same strip
                    if (a.MinZ == b.MinZ)
                    {
                        // Allow shared wall (1 cell overlap)
                        int aMaxX = a.MinX + a.Width - 1;
                        int bMaxX = b.MinX + b.Width - 1;

                        // One should end where the other begins (shared wall)
                        bool validSharing = (aMaxX == b.MinX) || (bMaxX == a.MinX);
                        bool noOverlap = (aMaxX < b.MinX) || (bMaxX < a.MinX);

                        Assert.True(validSharing || noOverlap,
                            $"Subrooms overlap incorrectly: ({a.MinX}-{aMaxX}) and ({b.MinX}-{bMaxX})");
                    }
                }
            }
        }

        [Fact]
        public void Room20x17_PrefabNamesMatchWidths()
        {
            var input = CreateTestInput();
            var result = Calculate(input);

            // Verify each subroom's PrefabDefName matches its Width and Depth
            foreach (var subroom in result.Subrooms)
            {
                string expectedName = $"BTG_CrewBedSubroom{subroom.Width}x{subroom.Depth}";
                Assert.Equal(expectedName, subroom.PrefabDefName);
            }

            // Top strip (z=12-15): left region (5 cells) should have 1 subroom width 4
            var topStripSubrooms = result.Subrooms.Where(s => s.MinZ == 12).OrderBy(s => s.MinX).ToList();
            Assert.True(topStripSubrooms.Count > 0, "Should have subrooms in top strip");

            // First subroom in top strip (left region, 5 cells) should be expanded to width 4
            var leftRegionSubroom = topStripSubrooms.First();
            Assert.Equal(4, leftRegionSubroom.Width);
            Assert.Equal("BTG_CrewBedSubroom4x4", leftRegionSubroom.PrefabDefName);
        }

        [Fact]
        public void Room20x17_GeneratesWalls()
        {
            var input = CreateTestInput();
            var result = Calculate(input);

            // Verify we have walls
            Assert.NotEmpty(result.Walls);

            // Verify all walls are valid segments
            foreach (var wall in result.Walls)
            {
                // Wall should be either horizontal or vertical
                bool isHorizontal = wall.StartZ == wall.EndZ;
                bool isVertical = wall.StartX == wall.EndX;
                Assert.True(isHorizontal || isVertical,
                    $"Wall is neither horizontal nor vertical: ({wall.StartX},{wall.StartZ}) to ({wall.EndX},{wall.EndZ})");
            }
        }

        #endregion

        #region Strip Calculation Tests

        [Fact]
        public void CalculateStrips_15CellHeight_ReturnsThreeStripsDepth4()
        {
            // 15 cells = 4 + 2 + 4 + 1 + 4 (3 strips of depth 4 with corridors)
            var input = new SubroomPackingInput
            {
                Room = new SimpleRect { MinX = 0, MinZ = 0, Width = 20, Height = 17 },
                Doors = new List<DoorPosition>(),
                AvailableWidths = new List<int> { 3, 4 },
                AvailableDepths = new List<int> { 4, 5 }
            };

            var strips = CalculateStrips(input);

            // Should have 5 elements: 3 subroom strips + 2 corridors
            Assert.Equal(5, strips.Count);

            // Count subroom strips
            int subroomCount = strips.Count(s => s.Type == StripType.Subroom);
            Assert.Equal(3, subroomCount);

            // All subroom strips should be depth 4
            foreach (var strip in strips.Where(s => s.Type == StripType.Subroom))
            {
                Assert.Equal(4, strip.Depth);
            }
        }

        #endregion

        #region Region Calculation Tests

        [Fact]
        public void CalculateRegions_NorthDoor_CreatesThreeCellExclusion()
        {
            var strip = new Strip
            {
                MinZ = 12,
                MaxZ = 15,
                Type = StripType.Subroom,
                Facing = PlacementRotation.South,
                Regions = new List<Region>()
            };

            var room = new SimpleRect { MinX = 0, MinZ = 0, Width = 20, Height = 17 };
            var doors = new List<DoorPosition>
            {
                new DoorPosition { X = 7, Z = 16 } // North wall door
            };

            var regions = CalculateRegions(strip, room, doors);

            // Should have 3 regions: usable, exclusion, usable
            Assert.Equal(3, regions.Count);

            // Exclusion should be 3 cells centered on door (x=6-8)
            var exclusion = regions[1];
            Assert.True(exclusion.IsExclusionZone);
            Assert.Equal(6, exclusion.MinX);
            Assert.Equal(8, exclusion.MaxX);
        }

        [Fact]
        public void CalculateRegions_EastDoor_CreatesTwoCellExclusion()
        {
            var strip = new Strip
            {
                MinZ = 6,
                MaxZ = 9,
                Type = StripType.Subroom,
                Facing = PlacementRotation.South,
                Regions = new List<Region>()
            };

            var room = new SimpleRect { MinX = 0, MinZ = 0, Width = 20, Height = 17 };
            var doors = new List<DoorPosition>
            {
                new DoorPosition { X = 19, Z = 8 } // East wall door (within strip z=6-9)
            };

            var regions = CalculateRegions(strip, room, doors);

            // Should have 2 regions: usable, exclusion
            Assert.Equal(2, regions.Count);

            // Exclusion should be 2 cells at east edge (x=17-18)
            var exclusion = regions[1];
            Assert.True(exclusion.IsExclusionZone);
            Assert.Equal(17, exclusion.MinX);
            Assert.Equal(18, exclusion.MaxX);
        }

        #endregion

        #region Subroom Fitting Tests

        [Fact]
        public void FitSubrooms_5CellRegion_FitsOneSubroom()
        {
            // 5 cells with non-overlapping: can only fit 1 subroom
            // 2 subrooms would need 3 + 1 (wall) + 3 = 7 cells > 5
            var region = new Region { MinX = 1, MaxX = 5, IsExclusionZone = false };
            var room = new SimpleRect { MinX = 0, MinZ = 0, Width = 20, Height = 17 };

            var subrooms = FitSubrooms(
                region,
                stripMinZ: 12,
                stripDepth: 4,
                facing: PlacementRotation.North,
                availableWidths: new List<int> { 3, 4 },
                room: room);

            var subroom = Assert.Single(subrooms);
            // Expanded to fill: 3 + 1 (waste expansion) = 4
            Assert.Equal(4, subroom.Width);
        }

        [Fact]
        public void FitSubrooms_6CellRegion_FitsOneExpandedSubroom()
        {
            // 6 cells with non-overlapping: can only fit 1 subroom
            // 2 subrooms would need 3 + 1 (wall) + 3 = 7 cells > 6
            // Single subroom expanded from 3 to 4, leaving 2 cells waste
            var region = new Region { MinX = 3, MaxX = 8, IsExclusionZone = false };
            var room = new SimpleRect { MinX = 0, MinZ = 0, Width = 20, Height = 17 };

            var subrooms = FitSubrooms(
                region,
                stripMinZ: 1,
                stripDepth: 4,
                facing: PlacementRotation.South,
                availableWidths: new List<int> { 3, 4 },
                room: room);

            var subroom = Assert.Single(subrooms);
            // Expanded to max width 4
            Assert.Equal(4, subroom.Width);
        }

        [Fact]
        public void FitSubrooms_10CellRegion_FitsTwoSubrooms()
        {
            // 10 cells with non-overlapping: 2 subrooms + 1 wall
            // 2 subrooms: 3 + 1 (wall) + 3 = 7, waste 3
            // Expand both to max: 4 + 1 (wall) + 4 = 9, waste 1
            var region = new Region { MinX = 9, MaxX = 18, IsExclusionZone = false };
            var room = new SimpleRect { MinX = 0, MinZ = 0, Width = 20, Height = 17 };

            var subrooms = FitSubrooms(
                region,
                stripMinZ: 12,
                stripDepth: 4,
                facing: PlacementRotation.North,
                availableWidths: new List<int> { 3, 4 },
                room: room);

            Assert.Equal(2, subrooms.Count);
            // Both should be expanded to width 4
            Assert.All(subrooms, s => Assert.Equal(4, s.Width));
        }

        #endregion

        #region Helper Methods

        private static SubroomPackingInput CreateTestInput()
        {
            return new SubroomPackingInput
            {
                Room = new SimpleRect { MinX = 0, MinZ = 0, Width = 20, Height = 17 },
                Doors = new List<DoorPosition>
                {
                    new DoorPosition { X = 0, Z = 2 },   // West wall
                    new DoorPosition { X = 10, Z = 0 },  // South wall
                    new DoorPosition { X = 7, Z = 16 },  // North wall
                    new DoorPosition { X = 19, Z = 8 }   // East wall
                },
                AvailableWidths = new List<int> { 3, 4 },
                AvailableDepths = new List<int> { 4, 5 }
            };
        }

        #endregion

        #region Non-Square Prefab Tests

        [Fact]
        public void GetPrefabSpawnBoundsNonSquare_NorthRotation_KeepsDimensions()
        {
            // 3x4 prefab at (5, 10) facing North
            var bounds = PlacementCalculator.GetPrefabSpawnBoundsNonSquare(
                minX: 5, minZ: 10, width: 3, height: 4, rotation: PlacementRotation.North);

            Assert.Equal(5, bounds.MinX);
            Assert.Equal(10, bounds.MinZ);
            Assert.Equal(3, bounds.Width);
            Assert.Equal(4, bounds.Height);
        }

        [Fact]
        public void GetPrefabSpawnBoundsNonSquare_EastRotation_SwapsDimensions()
        {
            // 3x4 prefab at (5, 10) facing East - dimensions swap
            var bounds = PlacementCalculator.GetPrefabSpawnBoundsNonSquare(
                minX: 5, minZ: 10, width: 3, height: 4, rotation: PlacementRotation.East);

            Assert.Equal(5, bounds.MinX);
            Assert.Equal(10, bounds.MinZ);
            Assert.Equal(4, bounds.Width);   // height becomes width
            Assert.Equal(3, bounds.Height);  // width becomes height
        }

        [Fact]
        public void GetPrefabSpawnBoundsNonSquare_SouthRotation_KeepsDimensions()
        {
            // 3x4 prefab at (5, 10) facing South
            var bounds = PlacementCalculator.GetPrefabSpawnBoundsNonSquare(
                minX: 5, minZ: 10, width: 3, height: 4, rotation: PlacementRotation.South);

            Assert.Equal(5, bounds.MinX);
            Assert.Equal(10, bounds.MinZ);
            Assert.Equal(3, bounds.Width);
            Assert.Equal(4, bounds.Height);
        }

        [Fact]
        public void GetPrefabSpawnBoundsNonSquare_WestRotation_SwapsDimensions()
        {
            // 3x4 prefab at (5, 10) facing West - dimensions swap
            var bounds = PlacementCalculator.GetPrefabSpawnBoundsNonSquare(
                minX: 5, minZ: 10, width: 3, height: 4, rotation: PlacementRotation.West);

            Assert.Equal(5, bounds.MinX);
            Assert.Equal(10, bounds.MinZ);
            Assert.Equal(4, bounds.Width);   // height becomes width
            Assert.Equal(3, bounds.Height);  // width becomes height
        }

        [Fact]
        public void GetCenterFromCorner_3x4Prefab_CalculatesCorrectly()
        {
            // 3x4 prefab at corner (0, 0) facing North
            // World bounds: 0-2 in X (3 wide), 0-3 in Z (4 tall)
            // Center should be at (1, 2) - midpoints rounded down
            var (centerX, centerZ) = PlacementCalculator.GetCenterFromCorner(
                minX: 0, minZ: 0, width: 3, height: 4, rotation: PlacementRotation.North);

            Assert.Equal(1, centerX);  // 0 + 3/2 = 1
            Assert.Equal(2, centerZ);  // 0 + 4/2 = 2
        }

        [Fact]
        public void GetCornerFromCenter_3x4Prefab_CalculatesCorrectly()
        {
            // Inverse of GetCenterFromCorner test
            var (minX, minZ) = PlacementCalculator.GetCornerFromCenter(
                centerX: 1, centerZ: 2, width: 3, height: 4, rotation: PlacementRotation.North);

            Assert.Equal(0, minX);  // 1 - 3/2 = 0
            Assert.Equal(0, minZ);  // 2 - 4/2 = 0
        }

        [Fact]
        public void GetRotatedDimensions_NorthSouth_NoSwap()
        {
            var (w1, h1) = PlacementCalculator.GetRotatedDimensions(3, 4, PlacementRotation.North);
            var (w2, h2) = PlacementCalculator.GetRotatedDimensions(3, 4, PlacementRotation.South);

            Assert.Equal(3, w1);
            Assert.Equal(4, h1);
            Assert.Equal(3, w2);
            Assert.Equal(4, h2);
        }

        [Fact]
        public void GetRotatedDimensions_EastWest_Swaps()
        {
            var (w1, h1) = PlacementCalculator.GetRotatedDimensions(3, 4, PlacementRotation.East);
            var (w2, h2) = PlacementCalculator.GetRotatedDimensions(3, 4, PlacementRotation.West);

            Assert.Equal(4, w1);  // swapped
            Assert.Equal(3, h1);
            Assert.Equal(4, w2);
            Assert.Equal(3, h2);
        }

        #endregion
    }
}
