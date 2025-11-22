using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using BetterTradersGuild.RoomContents;

namespace BetterTradersGuild.Tests.Helpers
{
    /// <summary>
    /// Tests for DiagramGenerator - also serves as usage examples.
    /// Run these tests to see generated diagrams in the output!
    /// </summary>
    public class DiagramGeneratorTests
    {
        private readonly ITestOutputHelper output;

        public DiagramGeneratorTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void GenerateDiagram_StandardRoom_ShowsCorrectLayout()
        {
            // Arrange: 12x10 room with NW corner bedroom
            // NW uses North+West walls, so block other corners with doors on East+South walls
            var placement = PlacementCalculator.CalculateBestPlacement(
                new PlacementCalculator.SimpleRect { MinX = 0, MinZ = 0, Width = 12, Height = 10 },
                prefabSize: 6,
                doors: new List<PlacementCalculator.DoorPosition>
                {
                    new PlacementCalculator.DoorPosition { X = 11, Z = 5 },  // East wall - blocks NE & SE
                    new PlacementCalculator.DoorPosition { X = 6, Z = 0 }    // South wall - blocks SE & SW
                });

            // Act
            string diagram = DiagramGenerator.GenerateRoomDiagram(
                roomWidth: 12,
                roomHeight: 10,
                prefabCenterX: placement.CenterX,
                prefabCenterZ: placement.CenterZ,
                rotation: placement.Rotation,
                showCoordinates: true);

            // Output for viewing
            output.WriteLine("NW Corner Prefab (12x10 room):");
            output.WriteLine(diagram);
            output.WriteLine("");
            output.WriteLine($"Prefab center: ({placement.CenterX},{placement.CenterZ})");
            output.WriteLine($"Rotation: {placement.Rotation} (North - door faces south)");

            // Verify it generated something
            Assert.Contains("■", diagram);  // Contains walls
            Assert.Contains("↓", diagram);  // North rotation uses down arrow
            Assert.Contains("C", diagram);  // Contains center marker
        }

        [Fact]
        public void GenerateDiagram_WithDoors_ShowsDoorsCorrectly()
        {
            // Arrange: Room with SW corner bedroom
            // SW uses South+West walls, so block other corners with doors on North+East walls
            var doors = new List<(int x, int z)>
            {
                (6, 9),   // North wall - blocks NW & NE
                (11, 5)   // East wall - blocks NE & SE
            };

            var placement = PlacementCalculator.CalculateBestPlacement(
                new PlacementCalculator.SimpleRect { MinX = 0, MinZ = 0, Width = 12, Height = 10 },
                prefabSize: 6,
                doors: new List<PlacementCalculator.DoorPosition>
                {
                    new PlacementCalculator.DoorPosition { X = 6, Z = 9 },   // North wall - blocks NW & NE
                    new PlacementCalculator.DoorPosition { X = 11, Z = 5 }   // East wall - blocks NE & SE
                });

            // Act
            string diagram = DiagramGenerator.GenerateRoomDiagram(
                roomWidth: 12,
                roomHeight: 10,
                prefabCenterX: placement.CenterX,
                prefabCenterZ: placement.CenterZ,
                rotation: placement.Rotation,
                doorPositions: doors,
                showCoordinates: true);

            // Output
            output.WriteLine("SW Corner Prefab (→ arrows) with doors on North and East walls:");
            output.WriteLine(diagram);

            // Verify doors and arrows are shown
            Assert.Contains("D", diagram);
            Assert.Contains("→", diagram);  // West rotation uses right arrow
        }

        [Fact]
        public void GenerateWallDiagram_ShowsDoorPositions()
        {
            // Useful for testing wall-specific features
            var doors = new List<(int x, int z)>
            {
                (3, 9),
                (6, 9),
                (9, 9)
            };

            string wallDiagram = DiagramGenerator.GenerateWallDiagram(
                wallLength: 12,
                doorPositions: doors,
                wallZ: 9,
                wallName: "North");

            output.WriteLine("North wall with 3 doors:");
            output.WriteLine(wallDiagram);

            Assert.Contains("D", wallDiagram);
            Assert.Contains("#", wallDiagram);
        }

        [Fact]
        public void HowToUse_GenerateDiagramsForTestComments()
        {
            /*
             *  This test shows how to generate diagrams to paste into test comments.
             *
             *  USAGE:
             *  1. Run: ./run-tests.sh (or run this specific test)
             *  2. Look at the test output below
             *  3. Copy the diagram into your test comment
             *  4. The diagram will be mathematically correct!
             */

            output.WriteLine("=== HOW TO USE DiagramGenerator ===");
            output.WriteLine("");
            output.WriteLine("Step 1: Generate a diagram for your test scenario");
            output.WriteLine("");

            // Example: Generate diagram for NE corner placement
            // NE uses North+East walls, so block other corners with doors on West+South walls
            var placement = PlacementCalculator.CalculateBestPlacement(
                new PlacementCalculator.SimpleRect { MinX = 0, MinZ = 0, Width = 16, Height = 14 },
                prefabSize: 6,
                doors: new List<PlacementCalculator.DoorPosition>
                {
                    new PlacementCalculator.DoorPosition { X = 0, Z = 7 },   // West wall - blocks NW & SW
                    new PlacementCalculator.DoorPosition { X = 8, Z = 0 }    // South wall - blocks SW & SE
                });

            string diagram = DiagramGenerator.GenerateRoomDiagram(
                roomWidth: 16,
                roomHeight: 14,
                prefabCenterX: placement.CenterX,
                prefabCenterZ: placement.CenterZ,
                rotation: placement.Rotation,
                showCoordinates: true);

            output.WriteLine("Your generated diagram (← arrows show door faces west):");
            output.WriteLine(diagram);
            output.WriteLine("");

            output.WriteLine("Step 2: Copy and paste into your test comment:");
            output.WriteLine("");
            output.WriteLine("/*  Visual: 16x14 room - NE corner prefab");
            output.WriteLine(" *");
            foreach (var line in diagram.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                output.WriteLine(" *  " + line);
            }
            output.WriteLine(" */");
            output.WriteLine("");

            output.WriteLine("Step 3: Your test now has an accurate diagram with directional arrows!");
        }

        [Fact]
        public void EdgeCase_TinyRoom_StillGeneratesCorrectly()
        {
            // Even tiny rooms should generate without crashing
            string diagram = DiagramGenerator.GenerateRoomDiagram(
                roomWidth: 5,
                roomHeight: 5,
                showCoordinates: true);

            output.WriteLine("Tiny 5x5 room (no prefab):");
            output.WriteLine(diagram);

            Assert.Contains("■", diagram);  // Contains walls
            Assert.Contains("z=0", diagram);  // Contains z-axis labels
            Assert.Contains("z=4", diagram);  // Contains top row
        }

        [Fact]
        public void EdgeCase_LargeRoom_ShowsFullBounds()
        {
            // Large rooms should also work - NW corner placement
            // NW uses North+West walls, so block other corners with doors on East+South walls
            var placement = PlacementCalculator.CalculateBestPlacement(
                new PlacementCalculator.SimpleRect { MinX = 0, MinZ = 0, Width = 25, Height = 25 },
                prefabSize: 6,
                doors: new List<PlacementCalculator.DoorPosition>
                {
                    new PlacementCalculator.DoorPosition { X = 24, Z = 12 },  // East wall - blocks NE & SE
                    new PlacementCalculator.DoorPosition { X = 12, Z = 0 }    // South wall - blocks SE & SW
                });

            string diagram = DiagramGenerator.GenerateRoomDiagram(
                roomWidth: 25,
                roomHeight: 25,
                prefabCenterX: placement.CenterX,
                prefabCenterZ: placement.CenterZ,
                rotation: placement.Rotation,
                showCoordinates: true);

            output.WriteLine("Large 25x25 room with NW corner prefab:");
            output.WriteLine(diagram);

            // Should have prefab arrows in top-left
            Assert.Contains("↓", diagram);
        }
    }
}
