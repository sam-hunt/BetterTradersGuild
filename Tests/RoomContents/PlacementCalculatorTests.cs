using System.Collections.Generic;
using Xunit;
using BetterTradersGuild.RoomContents;
using static BetterTradersGuild.RoomContents.PlacementCalculator;

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
		 *  z=10  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← North wall
		 *   z=9  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=8  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=7  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=6  ■ ↓ ↓ C ↓ ↓ ↓ . . . . . ■  ← Prefab center
		 *   z=5  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=4  ■ ↓ ↓ ↓ ↓ ↓ ↓ . . . . . ■
		 *   z=3  ■ . . . . . . . . . . . ■
		 *   z=2  ■ . . . . . . . . . . . ■
		 *   z=1  ■ . . . . . . . . . . . ■
		 *   z=0  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← South wall
		 *          West (x=0), East (x=12)
		 *
		 *  Generated: 2025-11-17 21:51:51
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact]
		public void NWCorner_PlacesCorrectly()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 11, Z = 10 },  // NE corner
                    new DoorPosition { X = 11, Z = 0 },  // SE corner
                    new DoorPosition { X = 1, Z = 0 }    // SW corner
                }
			);
			Assert.Equal(PlacementType.Corner, result.Type);
			Assert.Equal(0, result.Rotation);  // North (door faces south ↓)
			Assert.Equal(3, result.CenterX);   // minX + prefabSize/2 = 0+3
			Assert.Equal(6, result.CenterZ);   // maxZ - (prefabSize/2+1) = 10-4
		}

		#endregion

		#region NE Corner Placement

		/*  <!-- DIAGRAM_START -->
		 *  Visual: 13x11 room (0,0 to 12,10)
		 *  Prefab: 6×6 at center (8,7), rotation 1 (East - door faces west ←)
		 *
		 *  z=10  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← North wall
		 *   z=9  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=8  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=7  ■ . . . . . ← ← C ← ← ← ■  ← Prefab center
		 *   z=6  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=5  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=4  ■ . . . . . ← ← ← ← ← ← ■
		 *   z=3  ■ . . . . . . . . . . . ■
		 *   z=2  ■ . . . . . . . . . . . ■
		 *   z=1  ■ . . . . . . . . . . . ■
		 *   z=0  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← South wall
		 *          West (x=0), East (x=12)
		 *
		 *  Generated: 2025-11-17 21:51:51
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact]
		public void NECorner_PlacesCorrectly()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 1, Z = 10 },   // NW corner
					new DoorPosition { X = 11, Z = 0 },  // SE corner
					new DoorPosition { X = 1, Z = 0 }    // SW corner
				}
			);
			Assert.Equal(PlacementType.Corner, result.Type);
			Assert.Equal(1, result.Rotation);  // East (door faces west ←)
			Assert.Equal(8, result.CenterX);   // maxX - (prefabSize/2+1) = 12-4 = 8
			Assert.Equal(7, result.CenterZ);   // maxZ - prefabSize/2 = 10-3 = 7
		}

		#endregion

		#region SE Corner Placement

		/*  <!-- DIAGRAM_START -->
		 *  Visual: 13x11 room (0,0 to 12,10)
		 *  Prefab: 6×6 at center (9,4), rotation 2 (South - door faces north ↑)
		 *
		 *  z=10  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← North wall
		 *   z=9  ■ . . . . . . . . . . . ■
		 *   z=8  ■ . . . . . . . . . . . ■
		 *   z=7  ■ . . . . . . . . . . . ■
		 *   z=6  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
		 *   z=5  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
		 *   z=4  ■ . . . . . ↑ ↑ ↑ C ↑ ↑ ■  ← Prefab center
		 *   z=3  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
		 *   z=2  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
		 *   z=1  ■ . . . . . ↑ ↑ ↑ ↑ ↑ ↑ ■
		 *   z=0  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■  ← South wall
		 *          West (x=0), East (x=12)
		 *
		 *  Generated: 2025-11-17 21:51:51
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact]
		public void SECorner_PlacesCorrectly()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 1, Z = 10 },   // NW corner
					new DoorPosition { X = 11, Z = 10 },  // NE corner
					new DoorPosition { X = 1, Z = 0 }    // SW corner
				}
			);
			Assert.Equal(PlacementType.Corner, result.Type);
			Assert.Equal(2, result.Rotation);  // South (door faces north ↑)
			Assert.Equal(9, result.CenterX);
			Assert.Equal(4, result.CenterZ);
		}

		#endregion

		#region SW Corner Placement

		/*  <!-- DIAGRAM_START -->
		 *  Visual: 13x11 room (0,0 to 12,10)
		 *  Prefab: 6×6 at center (4,3), rotation 3 (West - door faces east →)
		 *
		 *  z=10  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← North wall
		 *   z=9  ■ . . . . . . . . . . . ■
		 *   z=8  ■ . . . . . . . . . . . ■
		 *   z=7  ■ . . . . . . . . . . . ■
		 *   z=6  ■ → → → → → → . . . . . ■
		 *   z=5  ■ → → → → → → . . . . . ■
		 *   z=4  ■ → → → → → → . . . . . ■
		 *   z=3  ■ → → → C → → . . . . . ■  ← Prefab center
		 *   z=2  ■ → → → → → → . . . . . ■
		 *   z=1  ■ → → → → → → . . . . . ■
		 *   z=0  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← South wall
		 *          West (x=0), East (x=12)
		 *
		 *  Generated: 2025-11-17 21:51:51
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact]
		public void SWCorner_PlacesCorrectly()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 1, Z = 10 },   // NW corner
                    new DoorPosition { X = 11, Z = 10 },  // NE corner
                    new DoorPosition { X = 11, Z = 0 },  // SE corner
				}
			);
			Assert.Equal(PlacementType.Corner, result.Type);
			Assert.Equal(3, result.Rotation);  // West (door faces east →)
			Assert.Equal(4, result.CenterX);
			Assert.Equal(3, result.CenterZ);
		}

		#endregion

		#region North Edge Placement

		/*  <!-- DIAGRAM_START -->
		 *  Visual: 13x11 room (0,0 to 12,10)
		 *  Prefab: 6×6 at center (5,6), rotation 0 (North - door faces south ↓)
		 *
		 *  z=10  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← North wall
		 *   z=9  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=8  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=7  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=6  ■ . . ↓ ↓ C ↓ ↓ ↓ . . . ■  ← Prefab center
		 *   z=5  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=4  ■ . . ↓ ↓ ↓ ↓ ↓ ↓ . . . ■
		 *   z=3  ■ . . . . . . . . . . . ■
		 *   z=2  ■ . . . . . . . . . . . ■
		 *   z=1  ■ . . . . . . . . . . . ■
		 *   z=0  ■ D ■ ■ ■ ■ ■ ■ ■ ■ ■ D ■  ← South wall
		 *          West (x=0), East (x=12)
		 *
		 *  Generated: 2025-11-17 21:51:51
		 *  <!-- DIAGRAM_END -->
		*/
		[Fact]
		public void NorthEdge_PlacesFirstPossibleLeft()
		{
			var result = CalculateBestPlacement(
				new SimpleRect { MinX = 0, MinZ = 0, Width = 13, Height = 11 },
				prefabSize: 6,
				doors: new List<DoorPosition>
				{
					new DoorPosition { X = 1, Z = 10 },   // NW corner
					new DoorPosition { X = 11, Z = 10 },  // NE corner
					new DoorPosition { X = 11, Z = 0 },  // SE corner
					new DoorPosition { X = 1, Z = 0 }    // SW corner
				}
			);
			Assert.Equal(PlacementType.Edge, result.Type);  // Edge fallback
			Assert.Equal(0, result.Rotation);  // North (door faces south ↓)
			Assert.Equal(5, result.CenterX);   // Left-most viable position
			Assert.Equal(6, result.CenterZ);   // Offset inward from wall (9 - 4 = 5)
		}

		#endregion
	}
}
