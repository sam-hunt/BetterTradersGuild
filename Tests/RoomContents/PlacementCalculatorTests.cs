using System.Collections.Generic;
using Xunit;
using BetterTradersGuild.RoomContents;
using static BetterTradersGuild.RoomContents.PlacementCalculator;
using PlacementRotation = BetterTradersGuild.RoomContents.PlacementCalculator.PlacementRotation;

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
		 *  z=10  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← North wall
		 *   z=9  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=8  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=7  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=6  ■ ↓ ↓ C ↓ ↓ ↓ . . . . . ■  ← Prefab center
		 *   z=5  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . D
		 *   z=4  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=3  ■ . . . . . . . . . . . ■
		 *   z=2  ■ . . . . . . . . . . . ■
		 *   z=1  ■ . . . . . . . . . . . ■
		 *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
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
					new DoorPosition { X = 12, Z = 5 },  // NE, SE corner blocked
                    new DoorPosition { X = 6, Z = 0 },  // SW, SE corners blocked
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
		 *  z=10  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← North wall
		 *   z=9  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=8  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=7  ■ . . . . . ← ← C ← ← ← ■  ← Prefab center
		 *   z=6  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=5  D . . . . . ← ← ← ← ← ← ■
		 *   z=4  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=3  ■ . . . . . . . . . . . ■
		 *   z=2  ■ . . . . . . . . . . . ■
		 *   z=1  ■ . . . . . . . . . . . ■
		 *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
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
					new DoorPosition { X = 0, Z = 5 },   // NW, SW corners blocked
					new DoorPosition { X = 6, Z = 0 }    // SW, SE corners blocked
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
		 *  Generated: 2025-11-22 16:09:33
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
		 *  Generated: 2025-11-22 16:09:33
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
		 *  z=10  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← North wall
		 *   z=9  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=8  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=7  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=6  ■ . W ↓ ↓ C ↓ ↓ ↓ . . . ■  ← Prefab center
		 *   z=5  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=4  ■ . W ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=3  ■ . . . . . . . . . . . ■
		 *   z=2  ■ . . . . . . . . . . . ■
		 *   z=1  ■ . . . . . . . . . . . ■
		 *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
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
					new DoorPosition { X = 1, Z = 10 },   // NW corner blocked
					new DoorPosition { X = 11, Z = 10 },  // NE corner blocked
					new DoorPosition { X = 6, Z = 0 }  // SW, SE corners blocked
				}
			);

			Assert.Equal(PlacementType.Edge, result.Type);
			Assert.Equal(PlacementRotation.North, result.Rotation);
			Assert.Equal(5, result.CenterX);
			Assert.Equal(6, result.CenterZ);

			// Verify required wall (West wall at x=2, from z=4 to z=9)
			Assert.Single(result.RequiredWalls);
			AssertWallSegment(result.RequiredWalls[0], startX: 2, startZ: 4, endX: 2, endZ: 9);
		}

		#endregion

		#region NW Edge Placement

		/*  <!-- DIAGRAM_START -->
		 *  Visual: 13x11 room (0,0 to 12,10)
		 *  Prefab: 6×6 at center (7,6), rotation 0 (North - door faces south ↓)
		 *
		 *  z=10  ■ ■ ■ D ■ ■ ■ ■ ■ ■ ■ D ■  ← North wall
		 *   z=9  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
		 *   z=8  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
		 *   z=7  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
		 *   z=6  ■ . . . W ↓ ↓ C ↓ ↓ ↓ . ■  ← Prefab center
		 *   z=5  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
		 *   z=4  ■ . . . W ↓ ↓ ↓ ↓ ↓ ↓ . ■
		 *   z=3  ■ . . . . . . . . . . . ■
		 *   z=2  ■ . . . . . . . . . . . ■
		 *   z=1  ■ . . . . . . . . . . . ■
		 *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
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
					new DoorPosition { X = 11, Z = 10 },  // NE corner blocked
					new DoorPosition { X = 6, Z = 0 }  // SW, SE corners blocked
				}
			);
			Assert.Equal(PlacementType.Edge, result.Type);
			Assert.Equal(PlacementRotation.North, result.Rotation);
			Assert.Equal(7, result.CenterX);
			Assert.Equal(6, result.CenterZ);

			// Verify required wall (West wall at x=4, from z=4 to z=9)
			Assert.Single(result.RequiredWalls);
			AssertWallSegment(result.RequiredWalls[0], startX: 4, startZ: 4, endX: 4, endZ: 9);
		}

		#endregion

		#region E Edge Placement

		/*  <!-- DIAGRAM_START -->
		 *  Visual: 13x11 room (0,0 to 12,10)
		 *  Prefab: 6×6 at center (8,5), rotation 1 (East - door faces west ←)
		 *
		 *  z=10  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← North wall
		 *   z=9  ■ . . . . . . . . . . . D
		 *   z=8  ■ . . . . . . . . . . . ■
		 *   z=7  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=6  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=5  D . . . . . ← ← C ← ← ← ■  ← Prefab center
		 *   z=4  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=3  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=2  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=1  ■ . . . . . . . . . . . D
		 *   z=0  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact(Skip = "TODO: Implement East edge handling in TryEdgePlacement")]
		public void E_Edge_PlacesCorrectly()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 12, Z = 9 },  // NE corner blocked
					new DoorPosition { X = 12, Z = 1 },  // SE corner blocked
					new DoorPosition { X = 0, Z = 5 }  // NW, SW corners blocked
				}
			);
			Assert.Equal(PlacementType.Edge, result.Type);
			Assert.Equal(PlacementRotation.East, result.Rotation);
			Assert.Equal(8, result.CenterX);
			Assert.Equal(5, result.CenterZ);
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
		 *   z=6  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ . . ■
		 *   z=5  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ . . ■
		 *   z=4  ■ . . . ↑ ↑ ↑ C ↑ ↑ . . ■  ← Prefab center
		 *   z=3  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ . . ■
		 *   z=2  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ . . ■
		 *   z=1  ■ . . . ↑ ↑ ↑ ↑ ↑ ↑ . . ■
		 *   z=0  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact(Skip = "TODO: Implement handling")]
		public void SE_Edge_PlacesCorrectly()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 6, Z = 10 },   // NW, NE corners blocked
					new DoorPosition { X = 11, Z = 0 },  // SE corner blocked
					new DoorPosition { X = 1, Z = 0 }    // SW corner blocked
				}
			);
			Assert.Equal(PlacementType.Edge, result.Type);
			Assert.Equal(PlacementRotation.South, result.Rotation);
			Assert.Equal(7, result.CenterX);
			Assert.Equal(4, result.CenterZ);
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
		 *   z=6  ■ . ↑ ↑ ↑ ↑ ↑ ↑ . . . . ■
		 *   z=5  ■ . ↑ ↑ ↑ ↑ ↑ ↑ . . . . ■
		 *   z=4  ■ . ↑ ↑ ↑ C ↑ ↑ . . . . ■  ← Prefab center
		 *   z=3  ■ . ↑ ↑ ↑ ↑ ↑ ↑ . . . . ■
		 *   z=2  ■ . ↑ ↑ ↑ ↑ ↑ ↑ . . . . ■
		 *   z=1  ■ . ↑ ↑ ↑ ↑ ↑ ↑ . . . . ■
		 *   z=0  ■ D ■ ■ ■ ■ ■ ■ ■ D ■ ■ ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact(Skip = "TODO: Implement handling")]
		public void SW_Edge_PlacesCorrectly()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 6, Z = 10 },   // NW, NE corners blocked
					new DoorPosition { X = 9, Z = 0 },  // SE corner blocked
					new DoorPosition { X = 1, Z = 0 }    // SW corner blocked
				}
			);
			Assert.Equal(PlacementType.Edge, result.Type);
			Assert.Equal(PlacementRotation.South, result.Rotation);
			Assert.Equal(5, result.CenterX);
			Assert.Equal(4, result.CenterZ);
		}

		#endregion

		#region W Edge Placement

		/*  <!-- DIAGRAM_START -->
		 *  Visual: 13x11 room (0,0 to 12,10)
		 *  Prefab: 6×6 at center (4,5), rotation 3 (West - door faces east →)
		 *
		 *  z=10  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← North wall
		 *   z=9  D . . . . . . . . . . . ■
		 *   z=8  ■ → → → → → → . . . . . ■
		 *   z=7  ■ → → → → → → . . . . . ■
		 *   z=6  ■ → → → → → → . . . . . ■
		 *   z=5  ■ → → → C → → . . . . . D  ← Prefab center
		 *   z=4  ■ → → → → → → . . . . . ■
		 *   z=3  ■ → → → → → → . . . . . ■
		 *   z=2  ■ . . . . . . . . . . . ■
		 *   z=1  D . . . . . . . . . . . ■
		 *   z=0  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact(Skip = "TODO: Implement handling")]
		public void W_Edge_PlacesCorrectly()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 0, Z = 9 },  // NW corner blocked
					new DoorPosition { X = 0, Z = 1 },  // SW corner blocked
					new DoorPosition { X = 12, Z = 5 }  // NE, SE corners blocked
				}
			);
			Assert.Equal(PlacementType.Edge, result.Type);
			Assert.Equal(PlacementRotation.West, result.Rotation);
			Assert.Equal(4, result.CenterX);
			Assert.Equal(5, result.CenterZ);
		}

		#endregion

		#region Center Placement

		/*  <!-- DIAGRAM_START -->
		 *  Visual: 13x11 room (0,0 to 12,10)
		 *  Prefab: 6×6 at center (5,4), rotation 0 (North - door faces south ↓)
		 *
		 *  z=10  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← North wall
		 *   z=9  ■ . . . . . . . . . . . ■
		 *   z=8  ■ . . . . . . . . . . . ■
		 *   z=7  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=6  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=5  D . . ↓ ↓ ↓ ↓ ↓ ↓ . . . D
		 *   z=4  ■ . . ↓ ↓ C ↓ ↓ ↓ . . . ■  ← Prefab center
		 *   z=3  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=2  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=1  ■ . . . . . . . . . . . ■
		 *   z=0  ■ ■ ■ ■ ■ ■ D ■ ■ ■ ■ ■ ■  ← South wall
		 *      x=0 1 2 3 4 5 6 7 8 9 10  12
		 *
		 *  Generated: 2025-11-22 16:09:33
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact(Skip = "TODO: Implement handling")]
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
			Assert.Equal(PlacementType.Edge, result.Type);
			Assert.Equal(PlacementRotation.North, result.Rotation);
			Assert.Equal(5, result.CenterX);
			Assert.Equal(4, result.CenterZ);
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
