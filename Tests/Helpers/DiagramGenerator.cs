using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BetterTradersGuild.RoomContents;

namespace BetterTradersGuild.Tests.Helpers
{
	/// <summary>
	/// Generates accurate ASCII diagrams of room layouts for test documentation.
	/// Ensures diagrams match test data precisely.
	/// Shows the actual 6×6 prefab placement (not the 7×7 semantic blocking area).
	/// </summary>
	public static class DiagramGenerator
	{
		/// <summary>
		/// Calculates the prefab rectangle from center position using the actual placement logic.
		/// This uses PlacementCalculator.GetPrefabSpawnBounds to ensure diagrams match reality.
		/// </summary>
		public static PlacementCalculator.SimpleRect CalculatePrefabRect(
			int centerX,
			int centerZ,
			int prefabSize = 6,
			PlacementCalculator.PlacementRotation rotation = PlacementCalculator.PlacementRotation.North)
		{
			// Use the actual placement calculator to get accurate bounds
			return PlacementCalculator.GetPrefabSpawnBounds(centerX, centerZ, rotation, prefabSize);
		}

		/// <summary>
		/// Generates a room diagram showing prefab placement with directional arrows.
		/// Includes z-axis coordinates, horizontal spacing, and marks center with 'C'.
		/// </summary>
		/// <param name="roomWidth">Room width (number of cells)</param>
		/// <param name="roomHeight">Room height (number of cells)</param>
		/// <param name="prefabCenterX">X coordinate of prefab center (null for no prefab)</param>
		/// <param name="prefabCenterZ">Z coordinate of prefab center (null for no prefab)</param>
		/// <param name="rotation">Prefab rotation</param>
		/// <param name="doorPositions">List of door positions (null for no doors)</param>
		/// <param name="requiredWalls">List of wall segments that must be spawned (null for no walls)</param>
		/// <param name="showCoordinates">Show coordinate labels</param>
		/// <returns>Multi-line ASCII diagram</returns>
		public static string GenerateRoomDiagram(
			int roomWidth,
			int roomHeight,
			int? prefabCenterX = null,
			int? prefabCenterZ = null,
			PlacementCalculator.PlacementRotation rotation = PlacementCalculator.PlacementRotation.North,
			List<(int x, int z)> doorPositions = null,
			List<PlacementCalculator.WallSegment> requiredWalls = null,
			bool showCoordinates = false)
		{
			var sb = new StringBuilder();
			doorPositions = doorPositions ?? new List<(int x, int z)>();
			requiredWalls = requiredWalls ?? new List<PlacementCalculator.WallSegment>();

			// Calculate 6×6 prefab rect if center is provided
			PlacementCalculator.SimpleRect? prefabRect = null;
			if (prefabCenterX.HasValue && prefabCenterZ.HasValue)
			{
				prefabRect = CalculatePrefabRect(prefabCenterX.Value, prefabCenterZ.Value, 6, rotation);
			}

			// Calculate padding for z-axis labels (e.g., "z=99" needs 4 chars: "z=99")
			int maxZ = roomHeight - 1;
			int zLabelWidth = $"z={maxZ}".Length;
			string zPadding = new string(' ', zLabelWidth);

			// Room rows (from top to bottom, so iterate Z from max to min)
			for (int z = roomHeight - 1; z >= 0; z--)
			{
				// Z-axis label
				sb.Append($"z={z}".PadLeft(zLabelWidth));
				sb.Append("  ");

				for (int x = 0; x < roomWidth; x++)
				{
					char cell = GetCellChar(x, z, roomWidth, roomHeight, prefabRect, rotation, doorPositions, requiredWalls, prefabCenterX, prefabCenterZ);
					sb.Append(cell);
					sb.Append(' ');  // Space after cell
				}

				// Optional coordinate label
				if (showCoordinates && z == roomHeight - 1)
					sb.Append(" ← North wall");
				else if (showCoordinates && z == 0)
					sb.Append(" ← South wall");
				else if (showCoordinates && prefabCenterZ.HasValue && z == prefabCenterZ.Value)
					sb.Append(" ← Prefab center");

				sb.AppendLine();
			}

			// X-axis coordinate labels
			if (showCoordinates)
			{
				// Format: "x=" right-padded to (zLabelWidth + 2) so "0" aligns with first cell
				// For zLabelWidth=4: "    x=" (2 spaces + x=) + "0" → total 6 chars to "0"
				// This matches z-label format: "z=10" (4 chars) + "  " (2 spaces) = 6 chars to first cell
				sb.Append("x=".PadLeft(zLabelWidth + 2));

				for (int x = 0; x < roomWidth; x++)
				{
					if (x >= 0 && x <= 9)
					{
						// Single digit: show digit and space (2 chars per cell)
						sb.Append(x);
						sb.Append(' ');
					}
					else if (x >= 10)
					{
						// Double digits: only show even numbers (2 chars per cell)
						if (x % 2 == 0)
						{
							sb.Append(x.ToString().PadRight(2));
						}
						else
						{
							sb.Append("  ");  // Two spaces for odd numbers
						}
					}
				}

				sb.AppendLine();
			}

			// Generation timestamp (with blank line separator)
			sb.AppendLine();
			sb.Append("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

			return sb.ToString();
		}

		/// <summary>
		/// Generates argument description text for test comments.
		/// Shows room dimensions and prefab placement details.
		/// </summary>
		public static string GenerateArgumentDescription(
			int roomWidth,
			int roomHeight,
			int centerX,
			int centerZ,
			PlacementCalculator.PlacementRotation rotation,
			int prefabSize = 6)
		{
			int roomMinX = 0;
			int roomMinZ = 0;
			int roomMaxX = roomWidth - 1;
			int roomMaxZ = roomHeight - 1;

			string rotationName = rotation switch
			{
				PlacementCalculator.PlacementRotation.North => "North - door faces south ↓",
				PlacementCalculator.PlacementRotation.East => "East - door faces west ←",
				PlacementCalculator.PlacementRotation.South => "South - door faces north ↑",
				PlacementCalculator.PlacementRotation.West => "West - door faces east →",
				_ => "Unknown"
			};

			var sb = new StringBuilder();
			sb.AppendLine($"Visual: {roomWidth}x{roomHeight} room ({roomMinX},{roomMinZ} to {roomMaxX},{roomMaxZ})");
			sb.Append($"Prefab: {prefabSize}×{prefabSize} at center ({centerX},{centerZ}), rotation {(int)rotation} ({rotationName})");

			return sb.ToString();
		}

		/// <summary>
		/// Generates a wall diagram showing door positions.
		/// </summary>
		public static string GenerateWallDiagram(
			int wallLength,
			List<(int x, int z)> doorPositions,
			int wallZ,
			string wallName = "North")
		{
			var sb = new StringBuilder();
			sb.AppendLine($"{wallName} wall (z={wallZ}): {wallLength} cells");

			for (int x = 0; x < wallLength; x++)
			{
				bool hasDoor = doorPositions.Any(d => d.x == x && d.z == wallZ);
				sb.Append(hasDoor ? 'D' : '#');
			}

			sb.AppendLine();

			// Add position markers every 5 cells
			for (int x = 0; x < wallLength; x++)
			{
				if (x % 5 == 0)
					sb.Append("|");
				else
					sb.Append(" ");
			}
			sb.AppendLine();

			for (int x = 0; x < wallLength; x++)
			{
				if (x % 5 == 0)
					sb.Append(x.ToString()[0]);
				else
					sb.Append(" ");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Gets directional arrow character based on rotation.
		/// Arrow shows which direction the bedroom door faces (entrance into room).
		/// </summary>
		private static char GetArrowChar(PlacementCalculator.PlacementRotation rotation)
		{
			switch (rotation)
			{
				case PlacementCalculator.PlacementRotation.North: return '↓';
				case PlacementCalculator.PlacementRotation.East: return '←';
				case PlacementCalculator.PlacementRotation.South: return '↑';
				case PlacementCalculator.PlacementRotation.West: return '→';
				default: return 'X';
			}
		}

		private static char GetCellChar(
			int x, int z,
			int roomWidth,
			int roomHeight,
			PlacementCalculator.SimpleRect? prefabRect,
			PlacementCalculator.PlacementRotation rotation,
			List<(int x, int z)> doorPositions,
			List<PlacementCalculator.WallSegment> requiredWalls,
			int? centerX = null,
			int? centerZ = null)
		{
			// Check if this is a wall cell (room boundary)
			bool isWall = (x == 0 || x == roomWidth - 1 || z == 0 || z == roomHeight - 1);

			// Check for door first (highest priority, overrides everything)
			if (doorPositions.Any(d => d.x == x && d.z == z))
				return 'D';

			// Check if this cell is part of a required wall segment (second priority)
			if (IsInRequiredWalls(x, z, requiredWalls))
				return 'W';

			// Check if this is the prefab center (high priority)
			if (centerX.HasValue && centerZ.HasValue && x == centerX.Value && z == centerZ.Value)
				return 'C';

			// Check if in prefab area - prefabs can overlap walls
			if (prefabRect.HasValue && PlacementCalculator.ContainsCell(prefabRect.Value, x, z))
				return GetArrowChar(rotation);

			// Mark walls with black square (after checking prefab)
			if (isWall)
				return '■';

			// Empty interior space
			return '.';
		}

		/// <summary>
		/// Checks if a cell is part of any required wall segment.
		/// Handles both vertical (StartX == EndX) and horizontal (StartZ == EndZ) wall segments.
		/// </summary>
		private static bool IsInRequiredWalls(int x, int z, List<PlacementCalculator.WallSegment> requiredWalls)
		{
			foreach (var wall in requiredWalls)
			{
				if (wall.StartX == wall.EndX)  // Vertical wall
				{
					if (x == wall.StartX && z >= Math.Min(wall.StartZ, wall.EndZ) && z <= Math.Max(wall.StartZ, wall.EndZ))
						return true;
				}
				else  // Horizontal wall
				{
					if (z == wall.StartZ && x >= Math.Min(wall.StartX, wall.EndX) && x <= Math.Max(wall.StartX, wall.EndX))
						return true;
				}
			}
			return false;
		}
	}
}
