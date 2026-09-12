using System;
using Xunit;
using BetterTradersGuild.Helpers.MapGeneration;

namespace BetterTradersGuild.Tests.Helpers
{
    /// <summary>
    /// Tests for the pure core of StationHatchPlacer.FindBestCenter: a 3x3 hatch with a
    /// one-cell clear ring (a 5x5 clear window), nearest a preferred cell.
    /// Grids are drawn as strings, one row per z with z increasing upward (last line is
    /// z=0), '.' = clear floor, '#' = blocked.
    /// </summary>
    public class StationHatchPlacerTests
    {
        private static bool[,] Grid(params string[] rowsTopDown)
        {
            int height = rowsTopDown.Length;
            int width = rowsTopDown[0].Length;
            var clear = new bool[width, height];
            for (int row = 0; row < height; row++)
            {
                int z = height - 1 - row;
                for (int x = 0; x < width; x++)
                    clear[x, z] = rowsTopDown[row][x] == '.';
            }
            return clear;
        }

        [Fact]
        public void OpenGrid_PicksPreferredCell()
        {
            var grid = Grid(
                ".........",
                ".........",
                ".........",
                ".........",
                ".........",
                ".........",
                ".........",
                ".........",
                ".........");

            var result = StationHatchPlacer.FindBestCenter(grid, size: 3, clearance: 1, preferX: 4, preferZ: 4);

            Assert.Equal((4, 4), result);
        }

        [Fact]
        public void PreferredCellOutsideValidRange_ClampsToNearestValidWindow()
        {
            // The window needs 2 cells of margin, so the nearest valid centre to (0,0) is (2,2).
            var grid = Grid(
                ".......",
                ".......",
                ".......",
                ".......",
                ".......",
                ".......",
                ".......");

            var result = StationHatchPlacer.FindBestCenter(grid, size: 3, clearance: 1, preferX: 0, preferZ: 0);

            Assert.Equal((2, 2), result);
        }

        [Fact]
        public void BlockedCellInRing_RejectsWindow()
        {
            // Preferred (3,3) has a wall at (1,3), inside its ring -> shifts one right to (4,3).
            var grid = Grid(
                "........",
                "........",
                "........",
                "........",
                ".#......",
                "........",
                "........",
                "........");

            var result = StationHatchPlacer.FindBestCenter(grid, size: 3, clearance: 1, preferX: 3, preferZ: 3);

            Assert.Equal((4, 3), result);
        }

        [Fact]
        public void CorridorNarrowerThanWindow_NoPlacement()
        {
            // A 4-wide corridor: a 3x3 hatch would fit but its ring would not, and placing
            // it would leave passages of one cell; the rule refuses.
            var grid = Grid(
                "##########",
                "##########",
                "##########",
                "..........",
                "..........",
                "..........",
                "..........",
                "##########",
                "##########",
                "##########");

            var result = StationHatchPlacer.FindBestCenter(grid, size: 3, clearance: 1, preferX: 5, preferZ: 5);

            Assert.Null(result);
        }

        [Fact]
        public void OnlyOneClearRoom_FoundWhereverItIs()
        {
            var grid = Grid(
                "############",
                "############",
                "############",
                "############",
                "############",
                "############",
                "#######.....",
                "#######.....",
                "#######.....",
                "#######.....",
                "#######.....",
                "############");

            var result = StationHatchPlacer.FindBestCenter(grid, size: 3, clearance: 1, preferX: 0, preferZ: 11);

            Assert.Equal((9, 3), result);
        }

        [Fact]
        public void ZeroClearance_FootprintOnlyMustBeClear()
        {
            // 3x3 clear island exactly; with a ring required it fails, without it succeeds.
            var grid = Grid(
                "#####",
                "#...#",
                "#...#",
                "#...#",
                "#####");

            Assert.Null(StationHatchPlacer.FindBestCenter(grid, size: 3, clearance: 1, preferX: 2, preferZ: 2));
            Assert.Equal((2, 2), StationHatchPlacer.FindBestCenter(grid, size: 3, clearance: 0, preferX: 2, preferZ: 2));
        }

        [Fact]
        public void EvenSize_Throws()
        {
            var grid = Grid("....", "....", "....", "....");

            Assert.Throws<ArgumentException>(() => StationHatchPlacer.FindBestCenter(grid, size: 2, clearance: 1, preferX: 1, preferZ: 1));
        }
    }
}
