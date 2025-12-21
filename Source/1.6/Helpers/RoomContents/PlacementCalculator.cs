using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Pure placement calculation logic for prefabs (e.g., bedroom subrooms).
    /// Contains no RimWorld dependencies - all methods work with primitives for easy unit testing.
    ///
    /// Supports both even-sized (6×6) and odd-sized (5×5) square prefabs, as well as
    /// non-square prefabs (4×5, 5×4) via GetPrefabSpawnBoundsNonSquare.
    /// Even-sized dimensions require a center offset adjustment due to RimWorld's
    /// center-based spawning asymmetry.
    /// </summary>
    public static class PlacementCalculator
    {
        /// <summary>
        /// Offset to account for RimWorld's center-based prefab spawning with even-sized prefabs.
        /// Even-sized prefabs (6×6) cannot center symmetrically, so RimWorld's spawn API rounds
        /// the position. This offset ensures the prefab aligns flush with room walls/corners.
        /// Odd-sized prefabs (5×5) center symmetrically and don't need this offset.
        /// </summary>
        private const int EVEN_SIZE_CENTER_OFFSET = 1;

        /// <summary>
        /// Returns the center offset for a given prefab size.
        /// Even-sized prefabs need an offset of 1; odd-sized prefabs need no offset.
        /// </summary>
        private static int GetCenterOffset(int prefabSize) => (prefabSize % 2 == 0) ? EVEN_SIZE_CENTER_OFFSET : 0;

        /// <summary>
        /// Type of placement for the prefab within the room.
        /// </summary>
        public enum PlacementType
        {
            Invalid,   // No valid placement found
            Corner,    // Placed in room corner (uses 2 room walls)
            Edge,      // Placed along room edge (uses 1 room wall)
            Center   // Placed in room center (uses 0 room walls)
        }

        /// <summary>
        /// Rotation direction for prefab placement.
        /// Values match RimWorld's Rot4.AsInt: 0=North, 1=East, 2=South, 3=West.
        /// The rotation indicates which direction the prefab "faces" (where its door is).
        /// </summary>
        public enum PlacementRotation
        {
            North = 0,  // Door faces South ↓
            East = 1,   // Door faces West ←
            South = 2,  // Door faces North ↑
            West = 3    // Door faces East →
        }

        /// <summary>
        /// Result of a placement calculation containing position, rotation, placement type, and required walls.
        /// </summary>
        public struct PlacementResult
        {
            public int CenterX;
            public int CenterZ;
            public PlacementRotation Rotation;
            public PlacementType Type;
            public List<WallSegment> RequiredWalls;  // Walls that must be spawned for non-corner placements
        }

        /// <summary>
        /// Simple rectangle representation for testing without RimWorld dependencies.
        /// </summary>
        public struct SimpleRect
        {
            public int MinX;
            public int MinZ;
            public int Width;
            public int Height;

            public int MaxX => MinX + Width - 1;
            public int MaxZ => MinZ + Height - 1;
        }

        /// <summary>
        /// Represents a door position in a room.
        /// </summary>
        public struct DoorPosition
        {
            public int X;
            public int Z;
        }

        /// <summary>
        /// Represents a wall segment that needs to be spawned for non-corner placements.
        /// Walls are described by start and end coordinates in absolute room space.
        /// For vertical walls: StartX == EndX, iterate Z from StartZ to EndZ.
        /// For horizontal walls: StartZ == EndZ, iterate X from StartX to EndX.
        /// </summary>
        public struct WallSegment
        {
            public int StartX;
            public int StartZ;
            public int EndX;
            public int EndZ;
        }

        /// <summary>
        /// Calculates the best placement for a prefab within a room, accounting for door positions.
        /// Uses priority-based fallback: corners (all 4) → edges (North only) → invalid.
        /// </summary>
        /// <param name="room">Room dimensions</param>
        /// <param name="prefabSize">Size of the prefab (assumed square)</param>
        /// <param name="doors">List of door positions in the room</param>
        /// <returns>Placement result with position, rotation, and type</returns>
        public static PlacementResult CalculateBestPlacement(
            SimpleRect room,
            int prefabSize,
            List<DoorPosition> doors)
        {
            // Phase 1: Try all 4 corners (preferred - uses 2 room walls)
            PlacementResult[] corners = new PlacementResult[]
            {
                CalculateNWCornerPlacement(room, prefabSize),
                CalculateNECornerPlacement(room, prefabSize),
                CalculateSECornerPlacement(room, prefabSize),
                CalculateSWCornerPlacement(room, prefabSize)
            };

            foreach (var corner in corners)
            {
                if (!HasDoorConflict(corner, room, doors, prefabSize))
                {
                    return corner;
                }
            }


            // Phase 2: Try all 4 edge placements (North, East, South, West)
            for (int wallDirection = 0; wallDirection < 4; wallDirection++)
            {
                PlacementResult edge = TryEdgePlacement(room, wallDirection, prefabSize, doors);
                if (edge.Type != PlacementType.Invalid)
                {
                    return edge;
                }
            }

            // Phase 3: Try center (floating) placement as final fallback
            PlacementResult center = CalculateCenterPlacement(room, prefabSize);
            if (center.Type != PlacementType.Invalid)
            {
                return center;
            }

            // Phase 4: No valid placement found
            return new PlacementResult
            {
                Type = PlacementType.Invalid,
                RequiredWalls = new List<WallSegment>()
            };
        }

        /// <summary>
        /// Checks if a placement would conflict with doors in the room's walls.
        /// For corner placements, checks if doors exist on the two walls the prefab uses.
        /// For edge placements, checks if doors exist within the prefab's wall segment.
        /// </summary>
        private static bool HasDoorConflict(PlacementResult placement, SimpleRect room, List<DoorPosition> doors, int prefabSize = 6)
        {
            if (placement.Type == PlacementType.Corner)
            {
                // Corner placements: check the two walls within the prefab's footprint
                return CheckCornerDoorConflict(placement.Rotation, room, doors, prefabSize);
            }
            else if (placement.Type == PlacementType.Edge)
            {
                // Edge placements: check if doors exist within the prefab's segment
                // (This is already handled during edge placement calculation)
                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks if corner placement has doors on walls adjacent to the prefab's footprint.
        /// Calculates actual prefab bounds, then checks for doors adjacent to those bounds.
        /// </summary>
        private static bool CheckCornerDoorConflict(PlacementRotation rotation, SimpleRect room, List<DoorPosition> doors, int prefabSize)
        {
            // Calculate the actual placement position for this corner
            PlacementResult placement;
            switch ((int)rotation)
            {
                case 0: placement = CalculateNWCornerPlacement(room, prefabSize); break;
                case 1: placement = CalculateNECornerPlacement(room, prefabSize); break;
                case 2: placement = CalculateSECornerPlacement(room, prefabSize); break;
                case 3: placement = CalculateSWCornerPlacement(room, prefabSize); break;
                default: return true;
            }

            // Get the actual prefab bounds
            var prefabBounds = GetPrefabSpawnBounds(placement.CenterX, placement.CenterZ, rotation, prefabSize);

            // Check doors on the two walls that align with this corner
            switch ((int)rotation)
            {
                case 0:  // North (NW corner) - check North + West walls
                         // North wall: check x range where prefab's north edge is
                         // West wall: check z range where prefab's west edge is
                    return HasDoorsOnWallSegment(doors, room, 0, prefabBounds.MinX, prefabBounds.MaxX) ||
                           HasDoorsOnWallSegment(doors, room, 3, prefabBounds.MinZ, prefabBounds.MaxZ);

                case 1:  // East (NE corner) - check North + East walls
                    return HasDoorsOnWallSegment(doors, room, 0, prefabBounds.MinX, prefabBounds.MaxX) ||
                           HasDoorsOnWallSegment(doors, room, 1, prefabBounds.MinZ, prefabBounds.MaxZ);

                case 2:  // South (SE corner) - check South + East walls
                    return HasDoorsOnWallSegment(doors, room, 2, prefabBounds.MinX, prefabBounds.MaxX) ||
                           HasDoorsOnWallSegment(doors, room, 1, prefabBounds.MinZ, prefabBounds.MaxZ);

                case 3:  // West (SW corner) - check South + West walls
                    return HasDoorsOnWallSegment(doors, room, 2, prefabBounds.MinX, prefabBounds.MaxX) ||
                           HasDoorsOnWallSegment(doors, room, 3, prefabBounds.MinZ, prefabBounds.MaxZ);

                default:
                    return true;
            }
        }

        /// <summary>
        /// Checks if any door exists on a specific wall segment.
        /// This is more precise than checking the entire wall - only checks within a range.
        /// </summary>
        /// <param name="doors">List of door positions</param>
        /// <param name="room">Room rectangle</param>
        /// <param name="wallDirection">0=North, 1=East, 2=South, 3=West</param>
        /// <param name="rangeStart">Start of range to check (X for horizontal walls, Z for vertical walls)</param>
        /// <param name="rangeEnd">End of range to check (inclusive)</param>
        private static bool HasDoorsOnWallSegment(List<DoorPosition> doors, SimpleRect room, int wallDirection, int rangeStart, int rangeEnd)
        {
            foreach (var door in doors)
            {
                switch (wallDirection)
                {
                    case 0:  // North wall (maxZ) - check X range
                        if (door.Z == room.MaxZ && door.X >= rangeStart && door.X <= rangeEnd)
                            return true;
                        break;
                    case 1:  // East wall (maxX) - check Z range
                        if (door.X == room.MaxX && door.Z >= rangeStart && door.Z <= rangeEnd)
                            return true;
                        break;
                    case 2:  // South wall (minZ) - check X range
                        if (door.Z == room.MinZ && door.X >= rangeStart && door.X <= rangeEnd)
                            return true;
                        break;
                    case 3:  // West wall (minX) - check Z range
                        if (door.X == room.MinX && door.Z >= rangeStart && door.Z <= rangeEnd)
                            return true;
                        break;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to find a valid edge placement along a specific wall.
        /// Searches for a contiguous segment without doors.
        /// </summary>
        private static PlacementResult TryEdgePlacement(SimpleRect room, int wallDirection, int prefabSize, List<DoorPosition> doors)
        {
            // Calculate required segment length (prefab size + 1 for safety margin)
            int segmentLength = prefabSize + 1;
            int cornerOffset = 2;  // Avoid corner overlap

            // Get wall cells
            var wallCells = GetWallCells(room, wallDirection);

            if (wallCells.Count < segmentLength)
                return new PlacementResult
                {
                    Type = PlacementType.Invalid,
                    RequiredWalls = new List<WallSegment>()
                };

            // Find contiguous segment without doors
            for (int startIdx = cornerOffset; startIdx <= wallCells.Count - segmentLength - cornerOffset; startIdx++)
            {
                bool hasDoorsInSegment = false;

                for (int i = 0; i < segmentLength; i++)
                {
                    var cell = wallCells[startIdx + i];
                    if (doors.Any(door => door.X == cell.x && door.Z == cell.z))
                    {
                        hasDoorsInSegment = true;
                        break;
                    }
                }

                if (!hasDoorsInSegment)
                {
                    // Found valid segment - calculate placement
                    // Center the prefab within the segment
                    // For segmentLength=7: center at index 3 (middle of 0-6 range)
                    int centerIdx = startIdx + (segmentLength - 1) / 2;
                    var wallCell = wallCells[centerIdx];
                    return CalculateEdgePlacement(wallCell.x, wallCell.z, wallDirection, prefabSize);
                }
            }

            return new PlacementResult
            {
                Type = PlacementType.Invalid,
                RequiredWalls = new List<WallSegment>()
            };
        }

        /// <summary>
        /// Gets all cells along a specific wall.
        /// </summary>
        private static List<(int x, int z)> GetWallCells(SimpleRect room, int wallDirection)
        {
            var cells = new List<(int x, int z)>();

            switch (wallDirection)
            {
                case 0:  // North wall (maxZ)
                    for (int x = room.MinX; x <= room.MaxX; x++)
                        cells.Add((x, room.MaxZ));
                    break;
                case 1:  // East wall (maxX)
                    for (int z = room.MinZ; z <= room.MaxZ; z++)
                        cells.Add((room.MaxX, z));
                    break;
                case 2:  // South wall (minZ)
                    for (int x = room.MinX; x <= room.MaxX; x++)
                        cells.Add((x, room.MinZ));
                    break;
                case 3:  // West wall (minX)
                    for (int z = room.MinZ; z <= room.MaxZ; z++)
                        cells.Add((room.MinX, z));
                    break;
            }

            return cells;
        }

        /// <summary>
        /// Calculates bedroom placement for NW (top-left) corner.
        /// Door faces South, missing walls on North+West (align with room corner).
        /// Uses center-based positioning.
        /// </summary>
        private static PlacementResult CalculateNWCornerPlacement(SimpleRect room, int prefabSize = 6)
        {
            int offset = GetCenterOffset(prefabSize);
            int halfSize = prefabSize / 2;
            // Position prefab one cell inside both North and West walls
            int centerX = room.MinX + 1 + halfSize - offset;
            int centerZ = room.MaxZ - offset - (prefabSize - halfSize);

            return new PlacementResult
            {
                CenterX = centerX,
                CenterZ = centerZ,
                Rotation = PlacementRotation.North,
                Type = PlacementType.Corner,
                RequiredWalls = new List<WallSegment>()  // Corners use room walls, no spawning needed
            };
        }

        /// <summary>
        /// Calculates bedroom placement for NE (top-right) corner.
        /// Door faces West, missing walls on North+East (align with room corner).
        /// </summary>
        private static PlacementResult CalculateNECornerPlacement(SimpleRect room, int prefabSize = 6)
        {
            int offset = GetCenterOffset(prefabSize);
            int halfSize = prefabSize / 2;
            // Position prefab one cell inside both North and East walls
            int centerX = room.MaxX - offset - (prefabSize - halfSize);
            int centerZ = room.MaxZ - prefabSize + halfSize;

            return new PlacementResult
            {
                CenterX = centerX,
                CenterZ = centerZ,
                Rotation = PlacementRotation.East,
                Type = PlacementType.Corner,
                RequiredWalls = new List<WallSegment>()  // Corners use room walls, no spawning needed
            };
        }

        /// <summary>
        /// Calculates bedroom placement for SE (bottom-right) corner.
        /// Door faces North, missing walls on South+East (align with room corner).
        /// </summary>
        private static PlacementResult CalculateSECornerPlacement(SimpleRect room, int prefabSize = 6)
        {
            int halfSize = prefabSize / 2;
            // Position prefab one cell inside both South and East walls
            int centerX = room.MaxX - prefabSize + halfSize;
            int centerZ = room.MinZ + 1 + halfSize;

            return new PlacementResult
            {
                CenterX = centerX,
                CenterZ = centerZ,
                Rotation = PlacementRotation.South,
                Type = PlacementType.Corner,
                RequiredWalls = new List<WallSegment>()  // Corners use room walls, no spawning needed
            };
        }

        /// <summary>
        /// Calculates bedroom placement for SW (bottom-left) corner.
        /// Door faces East, missing walls on South+West (align with room corner).
        /// </summary>
        private static PlacementResult CalculateSWCornerPlacement(SimpleRect room, int prefabSize = 6)
        {
            int offset = GetCenterOffset(prefabSize);
            int halfSize = prefabSize / 2;
            // Position prefab one cell inside both South and West walls
            int centerX = room.MinX + 1 + halfSize;
            int centerZ = room.MinZ + 1 + halfSize - offset;

            return new PlacementResult
            {
                CenterX = centerX,
                CenterZ = centerZ,
                Rotation = PlacementRotation.West,
                Type = PlacementType.Corner,
                RequiredWalls = new List<WallSegment>()  // Corners use room walls, no spawning needed
            };
        }

        /// <summary>
        /// Calculates center (floating) placement in the middle of the room.
        /// Used as final fallback when all corners and edges are blocked.
        /// The prefab floats in the center with spawned walls creating an enclosed subroom.
        /// </summary>
        private static PlacementResult CalculateCenterPlacement(SimpleRect room, int prefabSize = 6)
        {
            // Calculate center position, slightly biased for even-sized rooms
            int centerX = room.MinX + (room.Width / 2) - 1;
            int centerZ = room.MinZ + (room.Height / 2) - 1;

            return new PlacementResult
            {
                CenterX = centerX,
                CenterZ = centerZ,
                Rotation = PlacementRotation.North,  // Default to North for center placement
                Type = PlacementType.Center,
                RequiredWalls = CalculateCenterWalls(centerX, centerZ, PlacementRotation.North, prefabSize)
            };
        }

        /// <summary>
        /// Calculates the wall segments that must be spawned for a center placement.
        /// Center placements have no room walls, so both back wall and left side wall must be spawned.
        /// Returns a list of two wall segments: back wall and left wall.
        /// </summary>
        /// <param name="centerX">X coordinate of prefab center</param>
        /// <param name="centerZ">Z coordinate of prefab center</param>
        /// <param name="rotation">Rotation value (0=North, 1=East, 2=South, 3=West)</param>
        /// <param name="prefabSize">Size of the prefab (default 6)</param>
        /// <returns>List of two wall segments in absolute room coordinates</returns>
        private static List<WallSegment> CalculateCenterWalls(int centerX, int centerZ, PlacementRotation rotation, int prefabSize = 6)
        {
            var bounds = GetPrefabSpawnBounds(centerX, centerZ, rotation, prefabSize);

            // Both walls are outside the prefab footprint, forming an L-shape enclosure
            // Left wall is vertical (west side), back wall is horizontal (north side)
            return new List<WallSegment>
            {
                // Left wall (vertical, at MinX-1, spans full height of prefab)
                new WallSegment
                {
                    StartX = bounds.MinX - 1,
                    StartZ = bounds.MinZ,
                    EndX = bounds.MinX - 1,
                    EndZ = bounds.MaxZ
                },
                // Back wall (horizontal, at MaxZ+1, connects with left wall)
                new WallSegment
                {
                    StartX = bounds.MinX - 1,  // Start at left wall position to connect
                    StartZ = bounds.MaxZ + 1,  // One cell above prefab
                    EndX = bounds.MaxX,
                    EndZ = bounds.MaxZ + 1
                }
            };
        }

        /// <summary>
        /// Calculates the wall segment that must be spawned for an edge placement.
        /// Edge placements have one wall provided by the room edge (back wall), and need
        /// one spawned wall (left side wall in prefab's local coordinate system).
        /// </summary>
        /// <param name="centerX">X coordinate of prefab center</param>
        /// <param name="centerZ">Z coordinate of prefab center</param>
        /// <param name="rotation">Rotation value (0=North, 1=East, 2=South, 3=West)</param>
        /// <param name="prefabSize">Size of the prefab (default 6)</param>
        /// <returns>Wall segment in absolute room coordinates</returns>
        private static WallSegment CalculateEdgeWall(int centerX, int centerZ, PlacementRotation rotation, int prefabSize = 6)
        {
            var bounds = GetPrefabSpawnBounds(centerX, centerZ, rotation, prefabSize);

            switch ((int)rotation)
            {
                case 0:  // North edge (against North wall) - spawn West wall (left side)
                    return new WallSegment
                    {
                        StartX = bounds.MinX - 1,
                        StartZ = bounds.MinZ,
                        EndX = bounds.MinX - 1,
                        EndZ = bounds.MaxZ
                    };

                case 1:  // East edge (against East wall) - spawn North wall (left side)
                    return new WallSegment
                    {
                        StartX = bounds.MinX,
                        StartZ = bounds.MaxZ + 1,
                        EndX = bounds.MaxX,
                        EndZ = bounds.MaxZ + 1
                    };

                case 2:  // South edge (against South wall) - spawn East wall (left side)
                    return new WallSegment
                    {
                        StartX = bounds.MaxX + 1,
                        StartZ = bounds.MinZ,
                        EndX = bounds.MaxX + 1,
                        EndZ = bounds.MaxZ
                    };

                case 3:  // West edge (against West wall) - spawn South wall (left side)
                    return new WallSegment
                    {
                        StartX = bounds.MinX,
                        StartZ = bounds.MinZ - 1,
                        EndX = bounds.MaxX,
                        EndZ = bounds.MinZ - 1
                    };

                default:
                    // Invalid rotation - return empty segment
                    return new WallSegment { StartX = 0, StartZ = 0, EndX = 0, EndZ = 0 };
            }
        }

        /// <summary>
        /// Calculates bedroom placement along an edge, centered at the specified wall cell.
        /// The bedroom's back wall aligns with the room's edge wall.
        /// </summary>
        /// <param name="wallCenterX">X coordinate of the wall center cell</param>
        /// <param name="wallCenterZ">Z coordinate of the wall center cell</param>
        /// <param name="wallDirection">0=North, 1=East, 2=South, 3=West</param>
        /// <param name="prefabSize">Size of the prefab (default 6)</param>
        private static PlacementResult CalculateEdgePlacement(
            int wallCenterX,
            int wallCenterZ,
            int wallDirection,
            int prefabSize = 6)
        {
            PlacementRotation bedroomRotation;
            int centerX = wallCenterX;
            int centerZ = wallCenterZ;
            int offset = GetCenterOffset(prefabSize);

            switch (wallDirection)
            {
                case 0:  // North wall (maxZ) - bedroom faces South into room
                    bedroomRotation = PlacementRotation.North;
                    centerZ -= (prefabSize / 2 + offset);  // Offset inward from wall
                    break;

                case 1:  // East wall (maxX) - bedroom faces West into room
                    bedroomRotation = PlacementRotation.East;
                    centerX -= (prefabSize / 2 + offset);
                    break;

                case 2:  // South wall (minZ) - bedroom faces North into room
                    bedroomRotation = PlacementRotation.South;
                    centerZ += (prefabSize / 2 + offset);
                    break;

                case 3:  // West wall (minX) - bedroom faces East into room
                    bedroomRotation = PlacementRotation.West;
                    centerX += (prefabSize / 2 + offset);
                    break;

                default:
                    return new PlacementResult
                    {
                        Type = PlacementType.Invalid,
                        RequiredWalls = new List<WallSegment>()
                    };
            }

            return new PlacementResult
            {
                CenterX = centerX,
                CenterZ = centerZ,
                Rotation = bedroomRotation,
                Type = PlacementType.Edge,
                RequiredWalls = new List<WallSegment>
                {
                    CalculateEdgeWall(centerX, centerZ, bedroomRotation, prefabSize)
                }
            };
        }

        /// <summary>
        /// Calculates the actual prefab spawn bounds from center position.
        /// Returns the exact cell area that RimWorld will spawn the prefab into, accounting for
        /// EVEN_SIZE_CENTER_OFFSET asymmetry. This is used for diagram generation and validation.
        ///
        /// IMPORTANT: RimWorld's spawning behavior for even-sized prefabs has inherent asymmetry
        /// that does NOT rotate with the prefab. The prefab always extends [-3, +2] from center
        /// in both axes (for 6×6), regardless of rotation. The placement calculator functions
        /// pre-offset the center position to achieve desired wall alignment, but additional
        /// per-rotation corrections are needed here to match RimWorld's actual spawn behavior.
        ///
        /// Why rotation matrices don't work: RimWorld doesn't geometrically rotate the bounds.
        /// The asymmetric offset is always applied in the same direction (relative to world axes,
        /// not prefab axes), so each rotation requires a different Min coordinate adjustment.
        /// </summary>
        /// <param name="centerX">X coordinate of prefab center</param>
        /// <param name="centerZ">Z coordinate of prefab center</param>
        /// <param name="rotation">Rotation value</param>
        /// <param name="prefabSize">Size of the prefab (default 6)</param>
        /// <returns>Rectangle representing the actual prefab spawn bounds</returns>
        public static SimpleRect GetPrefabSpawnBounds(
            int centerX,
            int centerZ,
            PlacementRotation rotation,
            int prefabSize = 6)
        {
            int halfSize = prefabSize / 2;  // For 6×6: halfSize = 3, for 5×5: halfSize = 2
            int offset = GetCenterOffset(prefabSize);

            // Base calculation differs by parity:
            // - Even-sized (6×6): extends [center-halfSize+1, center+halfSize] → asymmetric
            // - Odd-sized (5×5): extends [center-halfSize, center+halfSize] → symmetric
            int evenCorrection = (prefabSize % 2 == 0) ? 1 : 0;
            int baseMinX = centerX - halfSize + evenCorrection;
            int baseMinZ = centerZ - halfSize + evenCorrection;

            // Apply rotation-specific corrections to match RimWorld's spawn behavior
            // These corrections compensate for the interaction between:
            // 1. The placement calculator's center pre-offsetting (only for even-sized prefabs)
            // 2. RimWorld's fixed asymmetric spawn behavior (which doesn't rotate)
            // For odd-sized prefabs, offset is 0 so these corrections have no effect
            switch ((int)rotation)
            {
                case 0:  // North (door faces south)
                         // No correction needed - base calculation is correct
                    return new SimpleRect
                    {
                        MinX = baseMinX,
                        MinZ = baseMinZ,
                        Width = prefabSize,
                        Height = prefabSize
                    };

                case 1:  // East (door faces west)
                         // X axis needs correction due to placement calculator's pre-offset
                    return new SimpleRect
                    {
                        MinX = baseMinX,
                        MinZ = baseMinZ - offset,
                        Width = prefabSize,
                        Height = prefabSize
                    };

                case 2:  // South (door faces north)
                         // Z axis needs correction
                    return new SimpleRect
                    {
                        MinX = baseMinX - offset,
                        MinZ = baseMinZ - offset,
                        Width = prefabSize,
                        Height = prefabSize
                    };

                case 3:  // West (door faces east)
                         // X axis needs correction (same as East rotation)
                    return new SimpleRect
                    {
                        MinX = baseMinX - offset,
                        MinZ = baseMinZ,
                        Width = prefabSize,
                        Height = prefabSize
                    };

                default:
                    // Fallback for invalid rotation
                    return new SimpleRect
                    {
                        MinX = baseMinX,
                        MinZ = baseMinZ,
                        Width = prefabSize,
                        Height = prefabSize
                    };
            }
        }

        /// <summary>
        /// Checks if a cell is contained within a rectangle.
        /// </summary>
        public static bool ContainsCell(SimpleRect rect, int x, int z)
        {
            return x >= rect.MinX && x <= rect.MaxX && z >= rect.MinZ && z <= rect.MaxZ;
        }

        #region Non-Square Prefab Support

        /// <summary>
        /// Calculates the actual prefab spawn bounds for non-square prefabs.
        /// Handles prefabs where width ≠ height (e.g., 3×4, 4×5).
        ///
        /// IMPORTANT: Width and Height refer to the prefab's LOCAL dimensions:
        /// - Width: X dimension in prefab's local space (perpendicular to door direction)
        /// - Height: Z dimension in prefab's local space (parallel to door direction, depth)
        ///
        /// When rotated, these local dimensions map to world axes:
        /// - North/South rotation: localWidth → worldX, localHeight → worldZ
        /// - East/West rotation: localWidth → worldZ, localHeight → worldX
        /// </summary>
        /// <param name="minX">X coordinate of the prefab's minimum corner (not center)</param>
        /// <param name="minZ">Z coordinate of the prefab's minimum corner (not center)</param>
        /// <param name="width">Width of the prefab in local X (perpendicular to door)</param>
        /// <param name="height">Height/depth of the prefab in local Z (parallel to door)</param>
        /// <param name="rotation">Rotation of the prefab</param>
        /// <returns>Rectangle representing the actual prefab spawn bounds in world space</returns>
        public static SimpleRect GetPrefabSpawnBoundsNonSquare(
            int minX,
            int minZ,
            int width,
            int height,
            PlacementRotation rotation)
        {
            // For North/South rotations: prefab dimensions stay as width × height
            // For East/West rotations: prefab dimensions become height × width (swapped)
            int worldWidth, worldHeight;

            switch ((int)rotation)
            {
                case 0:  // North (door faces south)
                case 2:  // South (door faces north)
                    worldWidth = width;
                    worldHeight = height;
                    break;

                case 1:  // East (door faces west)
                case 3:  // West (door faces east)
                    // Dimensions swap when rotated 90°
                    worldWidth = height;
                    worldHeight = width;
                    break;

                default:
                    worldWidth = width;
                    worldHeight = height;
                    break;
            }

            return new SimpleRect
            {
                MinX = minX,
                MinZ = minZ,
                Width = worldWidth,
                Height = worldHeight
            };
        }

        /// <summary>
        /// Calculates the center position for a non-square prefab given its corner position.
        /// Used when converting from corner-based placement to center-based spawning.
        /// </summary>
        /// <param name="minX">X coordinate of the prefab's minimum corner</param>
        /// <param name="minZ">Z coordinate of the prefab's minimum corner</param>
        /// <param name="width">Width of the prefab in local X</param>
        /// <param name="height">Height/depth of the prefab in local Z</param>
        /// <param name="rotation">Rotation of the prefab</param>
        /// <returns>Tuple of (centerX, centerZ) for spawning</returns>
        public static (int centerX, int centerZ) GetCenterFromCorner(
            int minX,
            int minZ,
            int width,
            int height,
            PlacementRotation rotation)
        {
            var bounds = GetPrefabSpawnBoundsNonSquare(minX, minZ, width, height, rotation);

            // Center is calculated from the world-space bounds
            // For even dimensions, center rounds down (RimWorld convention)
            int centerX = bounds.MinX + bounds.Width / 2;
            int centerZ = bounds.MinZ + bounds.Height / 2;

            return (centerX, centerZ);
        }

        /// <summary>
        /// Calculates the corner position from center for a non-square prefab.
        /// Used when converting from center-based spawning to corner-based calculations.
        /// </summary>
        /// <param name="centerX">X coordinate of the prefab center</param>
        /// <param name="centerZ">Z coordinate of the prefab center</param>
        /// <param name="width">Width of the prefab in local X</param>
        /// <param name="height">Height/depth of the prefab in local Z</param>
        /// <param name="rotation">Rotation of the prefab</param>
        /// <returns>Tuple of (minX, minZ) corner position</returns>
        public static (int minX, int minZ) GetCornerFromCenter(
            int centerX,
            int centerZ,
            int width,
            int height,
            PlacementRotation rotation)
        {
            // Determine world-space dimensions based on rotation
            int worldWidth, worldHeight;

            switch ((int)rotation)
            {
                case 0:  // North
                case 2:  // South
                    worldWidth = width;
                    worldHeight = height;
                    break;

                case 1:  // East
                case 3:  // West
                    worldWidth = height;
                    worldHeight = width;
                    break;

                default:
                    worldWidth = width;
                    worldHeight = height;
                    break;
            }

            // Calculate corner from center (inverse of center calculation)
            // For even dimensions, this accounts for RimWorld's rounding
            int minX = centerX - worldWidth / 2;
            int minZ = centerZ - worldHeight / 2;

            return (minX, minZ);
        }

        /// <summary>
        /// Gets the world-space dimensions of a prefab after rotation.
        /// </summary>
        /// <param name="width">Local width (X dimension)</param>
        /// <param name="height">Local height (Z dimension)</param>
        /// <param name="rotation">Rotation to apply</param>
        /// <returns>Tuple of (worldWidth, worldHeight)</returns>
        public static (int worldWidth, int worldHeight) GetRotatedDimensions(
            int width,
            int height,
            PlacementRotation rotation)
        {
            switch ((int)rotation)
            {
                case 0:  // North
                case 2:  // South
                    return (width, height);

                case 1:  // East
                case 3:  // West
                    return (height, width);

                default:
                    return (width, height);
            }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for PlacementRotation enum.
    /// </summary>
    public static class PlacementRotationExtensions
    {
        /// <summary>
        /// Converts PlacementRotation to RimWorld's Rot4 for interop with game API.
        /// </summary>
        public static Verse.Rot4 AsRot4(this PlacementCalculator.PlacementRotation rotation)
        {
            return new Verse.Rot4((int)rotation);
        }
    }
}
