using System;
using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.Helpers;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Pure placement calculation logic for packing multiple subrooms into a larger room.
    /// Contains no RimWorld dependencies - all methods work with primitives for easy unit testing.
    ///
    /// Algorithm overview:
    /// 1. Divide room into horizontal strips based on prefab depths
    /// 2. Cut strips into regions based on door exclusion zones
    /// 3. Fit subrooms into regions, maximizing count then filling waste
    /// 4. Calculate wall segments for shared walls and enclosures
    /// </summary>
    public static class SubroomPackingCalculator
    {
        #region Data Structures

        /// <summary>
        /// Input parameters for subroom packing calculation.
        /// </summary>
        public struct SubroomPackingInput
        {
            /// <summary>Room bounds including walls (e.g., 0,0 to 19,16 for 20x17 room).</summary>
            public SimpleRect Room;

            /// <summary>Door positions on the room perimeter.</summary>
            public List<DoorPosition> Doors;

            /// <summary>Available prefab widths (e.g., [3, 4]).</summary>
            public List<int> AvailableWidths;

            /// <summary>Available prefab depths (e.g., [4, 5]).</summary>
            public List<int> AvailableDepths;
        }

        /// <summary>
        /// Result of subroom packing calculation.
        /// </summary>
        public struct SubroomPackingResult
        {
            /// <summary>The strip divisions (subroom strips and corridors).</summary>
            public List<Strip> Strips;

            /// <summary>Final subroom placements with positions, sizes, and rotations.</summary>
            public List<SubroomPlacement> Subrooms;

            /// <summary>Wall segments to spawn (shared walls and enclosing walls).</summary>
            public List<WallSegment> Walls;

            /// <summary>Waste filler areas that can be filled with decorative prefabs.</summary>
            public List<WasteFillerPlacement> WasteFillers;
        }

        /// <summary>
        /// A waste filler area between subrooms and exclusion zones.
        /// These are 1-2 cell wide strips that can't fit another subroom but can hold decorative prefabs.
        /// </summary>
        public struct WasteFillerPlacement
        {
            /// <summary>X coordinate of the waste filler's leftmost cell.</summary>
            public int MinX;

            /// <summary>Z coordinate of the waste filler's bottom cell.</summary>
            public int MinZ;

            /// <summary>Width of the waste filler area (1 or 2 cells).</summary>
            public int Width;

            /// <summary>Depth of the waste filler area (matches strip depth, 4 or 5 cells).</summary>
            public int Depth;

            /// <summary>
            /// Rotation for the waste filler prefab.
            /// Prefabs are designed facing East (content toward right).
            /// Use North (no rotation) when exclusion is on right, South (180°) when on left.
            /// </summary>
            public PlacementRotation Rotation;

            /// <summary>
            /// X coordinate for spawning. Delegates to SpawnPositionHelper.CalculateSpawnPosition
            /// for rotation-aware center calculation.
            /// </summary>
            public int CenterX => SpawnPositionHelper.CalculateSpawnPosition(
                MinX, MinZ, Width, Depth, (int)Rotation).centerX;

            /// <summary>
            /// Z coordinate for spawning. Delegates to SpawnPositionHelper.CalculateSpawnPosition
            /// for rotation-aware center calculation.
            /// </summary>
            public int CenterZ => SpawnPositionHelper.CalculateSpawnPosition(
                MinX, MinZ, Width, Depth, (int)Rotation).centerZ;
        }

        /// <summary>
        /// Type of strip in the room layout.
        /// </summary>
        public enum StripType
        {
            /// <summary>Strip containing subrooms.</summary>
            Subroom,

            /// <summary>Corridor strip between subroom strips.</summary>
            Corridor
        }

        /// <summary>
        /// A horizontal strip spanning the room width.
        /// </summary>
        public struct Strip
        {
            /// <summary>Minimum Z coordinate of the strip (inclusive).</summary>
            public int MinZ;

            /// <summary>Maximum Z coordinate of the strip (inclusive).</summary>
            public int MaxZ;

            /// <summary>Depth of the strip (MaxZ - MinZ + 1).</summary>
            public int Depth => MaxZ - MinZ + 1;

            /// <summary>Type of strip (Subroom or Corridor).</summary>
            public StripType Type;

            /// <summary>Direction subrooms face (only valid for Subroom type).</summary>
            public PlacementRotation Facing;

            /// <summary>Regions within the strip (usable areas and exclusion zones).</summary>
            public List<Region> Regions;
        }

        /// <summary>
        /// A region within a strip (either usable for subrooms or an exclusion zone).
        /// </summary>
        public struct Region
        {
            /// <summary>Minimum X coordinate of the region (inclusive).</summary>
            public int MinX;

            /// <summary>Maximum X coordinate of the region (inclusive).</summary>
            public int MaxX;

            /// <summary>Width of the region (MaxX - MinX + 1).</summary>
            public int Width => MaxX - MinX + 1;

            /// <summary>True if this is an exclusion zone (no subrooms can be placed).</summary>
            public bool IsExclusionZone;
        }

        /// <summary>
        /// A placed subroom with position, size, and rotation.
        /// Use CenterX/CenterZ properties to get the spawn position for PrefabUtility.SpawnPrefab.
        /// </summary>
        public struct SubroomPlacement
        {
            /// <summary>X coordinate of the subroom's leftmost cell.</summary>
            public int MinX;

            /// <summary>Z coordinate of the subroom's bottom cell.</summary>
            public int MinZ;

            /// <summary>Width of the subroom in cells (local X dimension before rotation).</summary>
            public int Width;

            /// <summary>Depth of the subroom in cells (local Z dimension before rotation).</summary>
            public int Depth;

            /// <summary>Rotation/facing direction of the subroom.</summary>
            public PlacementRotation Rotation;

            /// <summary>DefName of the prefab to spawn (e.g., "BTG_CrewBedSubroom3x4").</summary>
            public string PrefabDefName;

            /// <summary>
            /// X coordinate for spawning. Uses rotation-dependent formula to account for
            /// RimWorld's center adjustment on even-sized dimensions.
            /// North: MinX + (Width - 1) / 2
            /// South: MinX + Width / 2
            /// </summary>
            public int CenterX => Rotation == PlacementRotation.South
                ? MinX + Width / 2
                : MinX + (Width - 1) / 2;

            /// <summary>
            /// Z coordinate for spawning. Uses rotation-dependent formula to account for
            /// RimWorld's center adjustment on even-sized dimensions.
            /// North: MinZ + (Depth - 1) / 2
            /// South: MinZ + Depth / 2
            /// </summary>
            public int CenterZ => Rotation == PlacementRotation.South
                ? MinZ + Depth / 2
                : MinZ + (Depth - 1) / 2;
        }

        #endregion

        #region Constants

        /// <summary>
        /// Exclusion zone width for north/south wall doors.
        /// Door cell + 1 cell on each side = 3 cells total.
        /// </summary>
        private const int NS_DOOR_EXCLUSION_WIDTH = 3;

        /// <summary>
        /// Exclusion zone width for east/west wall doors.
        /// 1 cell for corridor access + 1 cell for subroom wall = 2 cells total.
        /// </summary>
        private const int EW_DOOR_EXCLUSION_WIDTH = 2;

        /// <summary>
        /// Minimum corridor width when adjacent strips face each other (door ↔ door).
        /// </summary>
        private const int MIN_CORRIDOR_FACING = 1;

        /// <summary>
        /// Minimum corridor width when one strip's door faces another's back (door ↔ back).
        /// </summary>
        private const int MIN_CORRIDOR_BACK = 2;

        #endregion

        #region Main Entry Point

        /// <summary>
        /// Calculates optimal subroom packing for a room.
        /// </summary>
        /// <param name="input">Room bounds, doors, and available prefab sizes.</param>
        /// <returns>Strip layout, subroom placements, and wall segments.</returns>
        public static SubroomPackingResult Calculate(SubroomPackingInput input)
        {
            // Step 1: Calculate optimal strip layout
            var strips = CalculateStrips(input);

            // Step 2: Calculate regions within each strip based on doors
            // Use index-based iteration to allow modification
            for (int i = 0; i < strips.Count; i++)
            {
                var strip = strips[i];
                if (strip.Type == StripType.Subroom)
                {
                    strip.Regions = CalculateRegions(strip, input.Room, input.Doors);
                    strips[i] = strip;
                }
            }

            // Step 3: Fit subrooms into each region and track waste areas
            var subrooms = new List<SubroomPlacement>();
            var wasteFillers = new List<WasteFillerPlacement>();

            foreach (var strip in strips)
            {
                if (strip.Type == StripType.Subroom && strip.Regions != null)
                {
                    for (int regionIndex = 0; regionIndex < strip.Regions.Count; regionIndex++)
                    {
                        var region = strip.Regions[regionIndex];
                        if (!region.IsExclusionZone)
                        {
                            var fitResult = FitSubroomsWithWaste(
                                region,
                                strip.MinZ,
                                strip.Depth,
                                strip.Facing,
                                input.AvailableWidths,
                                input.Room);

                            subrooms.AddRange(fitResult.Subrooms);

                            // Check if waste area borders an exclusion zone
                            if (fitResult.HasWaste && fitResult.WasteWidth > 0)
                            {
                                bool bordersExclusion = false;
                                // Prefabs are designed facing East (content on right side).
                                // Use North (no rotation) when exclusion is on right, South (180°) when on left.
                                PlacementRotation wasteFacing = PlacementRotation.North;

                                if (fitResult.WasteOnLeft)
                                {
                                    // Waste on left - check if previous region is exclusion zone
                                    if (regionIndex > 0 && strip.Regions[regionIndex - 1].IsExclusionZone)
                                    {
                                        bordersExclusion = true;
                                        // Exclusion is to the left - rotate 180° to face West
                                        wasteFacing = PlacementRotation.South;
                                    }
                                }
                                else
                                {
                                    // Waste on right - check if next region is exclusion zone
                                    if (regionIndex < strip.Regions.Count - 1 && strip.Regions[regionIndex + 1].IsExclusionZone)
                                    {
                                        bordersExclusion = true;
                                        // Exclusion is to the right - no rotation needed (default East-facing)
                                        wasteFacing = PlacementRotation.North;
                                    }
                                }

                                if (bordersExclusion)
                                {
                                    wasteFillers.Add(new WasteFillerPlacement
                                    {
                                        MinX = fitResult.WasteMinX,
                                        MinZ = strip.MinZ,
                                        Width = fitResult.WasteWidth,
                                        Depth = strip.Depth,
                                        Rotation = wasteFacing
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // Step 4: Calculate wall segments
            var walls = CalculateWalls(subrooms, strips, input.Room);

            return new SubroomPackingResult
            {
                Strips = strips,
                Subrooms = subrooms,
                Walls = walls,
                WasteFillers = wasteFillers
            };
        }

        #endregion

        #region Step 1: Strip Calculation

        /// <summary>
        /// Calculates the optimal strip layout for a room.
        /// Maximizes number of strips while respecting corridor minimums.
        /// </summary>
        public static List<Strip> CalculateStrips(SubroomPackingInput input)
        {
            var strips = new List<Strip>();

            // Interior bounds (excluding walls)
            int interiorMinZ = input.Room.MinZ + 1;
            int interiorMaxZ = input.Room.MaxZ - 1;
            int interiorHeight = interiorMaxZ - interiorMinZ + 1;

            // Sort depths descending to prefer larger prefabs first
            var depths = input.AvailableDepths.OrderByDescending(d => d).ToList();
            int minDepth = depths.Min();
            int maxDepth = depths.Max();

            // Try to fit maximum number of strips
            // Pattern: strips alternate facing (S, S, N, S, N, ...) with corridors between
            // Odd strips: first faces S, last faces N, middle alternates
            var bestLayout = FindBestStripLayout(interiorHeight, minDepth, maxDepth);

            if (bestLayout == null || bestLayout.Count == 0)
            {
                return strips;
            }

            // Position strips with BOTH ends anchored:
            // - Top strip flush with north wall (interiorMaxZ)
            // - Bottom strip flush with south wall (interiorMinZ)
            // - Any leftover space distributed to middle corridors
            //
            // The layout is in bottom-to-top order: [bottom_strip, corridor, middle_strip, corridor, top_strip]

            // Calculate total layout height and leftover
            int totalLayoutHeight = bestLayout.Sum(item => item.Depth);
            int leftover = interiorHeight - totalLayoutHeight;

            // Find corridor indices to distribute leftover to middle corridors
            var corridorIndices = new List<int>();
            for (int i = 0; i < bestLayout.Count; i++)
            {
                if (!bestLayout[i].IsSubroom)
                {
                    corridorIndices.Add(i);
                }
            }

            // Distribute leftover evenly to corridors (prefer middle corridors)
            if (leftover > 0 && corridorIndices.Count > 0)
            {
                int perCorridor = leftover / corridorIndices.Count;
                int extraForFirst = leftover % corridorIndices.Count;

                for (int i = 0; i < corridorIndices.Count; i++)
                {
                    int idx = corridorIndices[i];
                    var item = bestLayout[idx];
                    item.Depth += perCorridor + (i < extraForFirst ? 1 : 0);
                    bestLayout[idx] = item;
                }
            }

            // Now position from both ends:
            // 1. Bottom strip starts at interiorMinZ
            // 2. Top strip ends at interiorMaxZ
            // 3. Build from bottom up to find where top strip should start

            int currentZ = interiorMinZ;
            for (int i = 0; i < bestLayout.Count; i++)
            {
                var item = bestLayout[i];

                strips.Add(new Strip
                {
                    MinZ = currentZ,
                    MaxZ = currentZ + item.Depth - 1,
                    Type = item.IsSubroom ? StripType.Subroom : StripType.Corridor,
                    Facing = item.Facing,
                    Regions = new List<Region>()
                });

                currentZ += item.Depth;
            }

            // Reverse to get top-to-bottom order for consistent iteration
            strips.Reverse();

            return strips;
        }

        /// <summary>
        /// Layout item for strip planning.
        /// </summary>
        private struct LayoutItem
        {
            public bool IsSubroom;
            public int Depth;
            public PlacementRotation Facing;
        }

        /// <summary>
        /// Finds the best strip layout that maximizes subroom strips.
        /// </summary>
        private static List<LayoutItem> FindBestStripLayout(int availableHeight, int minDepth, int maxDepth)
        {
            // Try different numbers of strips, from max possible down to 1
            int maxStrips = availableHeight / minDepth;

            for (int numStrips = maxStrips; numStrips >= 1; numStrips--)
            {
                var layout = TryBuildLayout(numStrips, availableHeight, minDepth, maxDepth);
                if (layout != null)
                {
                    return layout;
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to build a layout with the specified number of subroom strips.
        /// </summary>
        private static List<LayoutItem> TryBuildLayout(int numSubroomStrips, int availableHeight, int minDepth, int maxDepth)
        {
            // Calculate minimum space needed for corridors
            // Pattern depends on number of strips:
            // 1 strip: no corridors needed
            // 2 strips: 1 corridor (facing each other = 1 cell)
            // 3 strips: 2 corridors (S faces back of S = 2 cells, S faces N = 1 cell)
            // General pattern: strips from north face S, strips from south face N

            if (numSubroomStrips == 1)
            {
                // Single strip, just use max depth that fits
                int depth = Math.Min(maxDepth, availableHeight);
                if (depth < minDepth) return null;

                return new List<LayoutItem>
                {
                    new LayoutItem { IsSubroom = true, Depth = depth, Facing = PlacementRotation.South }
                };
            }

            // Calculate facing directions for each strip
            // With odd number: S, S, N (top faces S, middle faces S, bottom faces N)
            // With 4: S, N, S, N (alternating pairs)
            var facings = CalculateFacings(numSubroomStrips);

            // Calculate corridor widths needed
            // Each corridor is between two strips. In our layout (bottom-to-top):
            // - facings[i] is the strip BELOW the corridor (lower Z)
            // - facings[i+1] is the strip ABOVE the corridor (higher Z)
            //
            // Remember: PlacementRotation.South means doors face NORTH (toward higher Z)
            //           PlacementRotation.North means doors face SOUTH (toward lower Z)
            var corridorWidths = new List<int>();
            for (int i = 0; i < numSubroomStrips - 1; i++)
            {
                var belowFacing = facings[i];     // Strip below corridor (lower Z)
                var aboveFacing = facings[i + 1]; // Strip above corridor (higher Z)

                // Doors face each other when:
                // - Below strip doors face UP (north) = PlacementRotation.South
                // - Above strip doors face DOWN (south) = PlacementRotation.North
                // This gives 1 cell corridor (doors face each other)
                // Otherwise = door faces back = 2 cells
                if (belowFacing == PlacementRotation.South && aboveFacing == PlacementRotation.North)
                {
                    corridorWidths.Add(MIN_CORRIDOR_FACING);
                }
                else
                {
                    corridorWidths.Add(MIN_CORRIDOR_BACK);
                }
            }

            int totalCorridorWidth = corridorWidths.Sum();
            int spaceForStrips = availableHeight - totalCorridorWidth;

            if (spaceForStrips < numSubroomStrips * minDepth)
            {
                return null; // Not enough space
            }

            // Distribute depth among strips, preferring uniform depth
            int baseDepth = spaceForStrips / numSubroomStrips;
            if (baseDepth > maxDepth) baseDepth = maxDepth;
            if (baseDepth < minDepth) return null;

            // Build the layout
            var layout = new List<LayoutItem>();
            int usedHeight = 0;

            for (int i = 0; i < numSubroomStrips; i++)
            {
                // Add subroom strip
                layout.Add(new LayoutItem
                {
                    IsSubroom = true,
                    Depth = baseDepth,
                    Facing = facings[i]
                });
                usedHeight += baseDepth;

                // Add corridor after each strip except the last
                if (i < numSubroomStrips - 1)
                {
                    layout.Add(new LayoutItem
                    {
                        IsSubroom = false,
                        Depth = corridorWidths[i],
                        Facing = PlacementRotation.North // N/A
                    });
                    usedHeight += corridorWidths[i];
                }
            }

            // Distribute remaining space: first expand strips, then corridors
            int remaining = availableHeight - usedHeight;

            // First, try to expand strips up to maxDepth
            // This gives subrooms more space before expanding corridors
            // Corridors stay at minimum width (respecting door access requirements)
            if (remaining > 0)
            {
                // Expand strips one cell at a time, cycling through all strips
                // until no more remaining space or all strips at maxDepth
                bool expanded = true;
                while (remaining > 0 && expanded)
                {
                    expanded = false;
                    for (int i = 0; i < layout.Count && remaining > 0; i++)
                    {
                        if (layout[i].IsSubroom && layout[i].Depth < maxDepth)
                        {
                            var item = layout[i];
                            item.Depth += 1;
                            layout[i] = item;
                            remaining--;
                            expanded = true;
                        }
                    }
                }
            }

            // Then, distribute any final remaining space to corridors
            if (remaining > 0 && corridorWidths.Count > 0)
            {
                for (int i = 0; i < layout.Count && remaining > 0; i++)
                {
                    if (!layout[i].IsSubroom)
                    {
                        var item = layout[i];
                        item.Depth += 1;
                        layout[i] = item;
                        remaining--;
                    }
                }
            }

            return layout;
        }

        /// <summary>
        /// Calculates facing directions for subroom strips.
        /// Pattern: top strips have doors facing south (toward corridor below),
        ///          bottom strip has doors facing north (toward corridor above).
        ///
        /// IMPORTANT: PlacementRotation values map to Rot4 for spawning:
        /// - PlacementRotation.North (0) = Rot4.North = prefab NOT rotated = door at z=0 = doors face SOUTH
        /// - PlacementRotation.South (2) = Rot4.South = prefab rotated 180° = door at z=max = doors face NORTH
        ///
        /// Layout is built bottom-to-top, so:
        /// - Strip 0 (bottom): doors face north → PlacementRotation.South
        /// - Other strips: doors face south → PlacementRotation.North
        ///
        /// With 3 strips built bottom-to-top: [South, North, North]
        /// After reversal (top-to-bottom): [North, North, South]
        /// </summary>
        private static List<PlacementRotation> CalculateFacings(int numStrips)
        {
            var facings = new List<PlacementRotation>();

            for (int i = 0; i < numStrips; i++)
            {
                if (i == 0)
                {
                    // Bottom strip: doors face NORTH (toward corridor above)
                    // Use PlacementRotation.South → Rot4.South → prefab rotated 180° → door faces north
                    facings.Add(PlacementRotation.South);
                }
                else
                {
                    // Other strips: doors face SOUTH (toward corridor below)
                    // Use PlacementRotation.North → Rot4.North → prefab not rotated → door faces south
                    facings.Add(PlacementRotation.North);
                }
            }

            return facings;
        }

        #endregion

        #region Step 2: Region Calculation

        /// <summary>
        /// Calculates regions within a strip based on door positions.
        /// </summary>
        public static List<Region> CalculateRegions(Strip strip, SimpleRect room, List<DoorPosition> doors)
        {
            var regions = new List<Region>();

            int interiorMinX = room.MinX + 1;
            int interiorMaxX = room.MaxX - 1;

            // Find all exclusion zones in this strip
            var exclusions = new List<(int minX, int maxX)>();

            foreach (var door in doors)
            {
                // Check if door affects this strip
                bool affectsStrip = false;
                int exclusionWidth = 0;
                int exclusionCenterX = door.X;

                // North wall door (z = room.MaxZ)
                if (door.Z == room.MaxZ && strip.MaxZ == room.MaxZ - 1)
                {
                    // Door on north wall affects topmost strip
                    affectsStrip = true;
                    exclusionWidth = NS_DOOR_EXCLUSION_WIDTH;
                }
                // South wall door (z = room.MinZ)
                else if (door.Z == room.MinZ && strip.MinZ == room.MinZ + 1)
                {
                    // Door on south wall affects bottommost strip
                    affectsStrip = true;
                    exclusionWidth = NS_DOOR_EXCLUSION_WIDTH;
                }
                // West wall door (x = room.MinX)
                // Check if door is within strip bounds OR aligned with the back wall of a middle strip.
                // Middle strips have back walls at strip.MaxZ+1 (North-facing) or strip.MinZ-1 (South-facing).
                else if (door.X == room.MinX &&
                    (door.Z >= strip.MinZ && door.Z <= strip.MaxZ ||
                     (strip.Facing == PlacementRotation.North && strip.MaxZ < room.MaxZ - 1 && door.Z == strip.MaxZ + 1) ||
                     (strip.Facing == PlacementRotation.South && strip.MinZ > room.MinZ + 1 && door.Z == strip.MinZ - 1)))
                {
                    // Door on west wall affects strips at that Z level or at back wall position
                    affectsStrip = true;
                    exclusionWidth = EW_DOOR_EXCLUSION_WIDTH;
                    exclusionCenterX = interiorMinX; // Start from interior edge
                }
                // East wall door (x = room.MaxX)
                else if (door.X == room.MaxX &&
                    (door.Z >= strip.MinZ && door.Z <= strip.MaxZ ||
                     (strip.Facing == PlacementRotation.North && strip.MaxZ < room.MaxZ - 1 && door.Z == strip.MaxZ + 1) ||
                     (strip.Facing == PlacementRotation.South && strip.MinZ > room.MinZ + 1 && door.Z == strip.MinZ - 1)))
                {
                    // Door on east wall affects strips at that Z level or at back wall position
                    affectsStrip = true;
                    exclusionWidth = EW_DOOR_EXCLUSION_WIDTH;
                    exclusionCenterX = interiorMaxX; // End at interior edge
                }

                if (affectsStrip)
                {
                    int halfWidth = exclusionWidth / 2;
                    int minX, maxX;

                    if (door.X == room.MinX)
                    {
                        // West door: exclusion starts at interior edge
                        minX = interiorMinX;
                        maxX = interiorMinX + exclusionWidth - 1;
                    }
                    else if (door.X == room.MaxX)
                    {
                        // East door: exclusion ends at interior edge
                        minX = interiorMaxX - exclusionWidth + 1;
                        maxX = interiorMaxX;
                    }
                    else
                    {
                        // North/South door: centered on door position
                        minX = door.X - halfWidth;
                        maxX = door.X + halfWidth;
                    }

                    // Clamp to interior bounds
                    minX = Math.Max(minX, interiorMinX);
                    maxX = Math.Min(maxX, interiorMaxX);

                    exclusions.Add((minX, maxX));
                }
            }

            // Sort exclusions by minX
            exclusions = exclusions.OrderBy(e => e.minX).ToList();

            // Merge overlapping exclusions
            var mergedExclusions = MergeOverlappingRanges(exclusions);

            // Build regions from exclusions
            int currentX = interiorMinX;
            foreach (var (exMinX, exMaxX) in mergedExclusions)
            {
                // Usable region before exclusion
                if (currentX < exMinX)
                {
                    regions.Add(new Region
                    {
                        MinX = currentX,
                        MaxX = exMinX - 1,
                        IsExclusionZone = false
                    });
                }

                // Exclusion zone
                regions.Add(new Region
                {
                    MinX = exMinX,
                    MaxX = exMaxX,
                    IsExclusionZone = true
                });

                currentX = exMaxX + 1;
            }

            // Usable region after last exclusion
            if (currentX <= interiorMaxX)
            {
                regions.Add(new Region
                {
                    MinX = currentX,
                    MaxX = interiorMaxX,
                    IsExclusionZone = false
                });
            }

            // Ensure middle strips have at least one exclusion zone for corridor connectivity.
            // A middle strip is one not adjacent to either the north or south room wall.
            // Without an exclusion zone, corridors above and below the strip would be disconnected.
            bool isMiddleStrip = strip.MinZ > room.MinZ + 1 && strip.MaxZ < room.MaxZ - 1;
            bool hasExclusionZone = regions.Any(r => r.IsExclusionZone);

            if (isMiddleStrip && !hasExclusionZone)
            {
                // Force-add exclusion zone at the start of the strip
                int exclusionMaxX = interiorMinX + EW_DOOR_EXCLUSION_WIDTH - 1;

                var newRegions = new List<Region>();
                newRegions.Add(new Region
                {
                    MinX = interiorMinX,
                    MaxX = exclusionMaxX,
                    IsExclusionZone = true
                });

                if (exclusionMaxX < interiorMaxX)
                {
                    newRegions.Add(new Region
                    {
                        MinX = exclusionMaxX + 1,
                        MaxX = interiorMaxX,
                        IsExclusionZone = false
                    });
                }

                regions = newRegions;
            }

            return regions;
        }

        /// <summary>
        /// Merges overlapping ranges into non-overlapping ranges.
        /// </summary>
        private static List<(int minX, int maxX)> MergeOverlappingRanges(List<(int minX, int maxX)> ranges)
        {
            if (ranges.Count == 0) return ranges;

            var merged = new List<(int minX, int maxX)>();
            var current = ranges[0];

            for (int i = 1; i < ranges.Count; i++)
            {
                var next = ranges[i];
                if (next.minX <= current.maxX + 1)
                {
                    // Overlapping or adjacent, merge
                    current = (current.minX, Math.Max(current.maxX, next.maxX));
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }
            merged.Add(current);

            return merged;
        }

        #endregion

        #region Step 3: Subroom Fitting

        /// <summary>
        /// Internal result from FitSubrooms including waste area info.
        /// </summary>
        private struct FitSubroomsResult
        {
            public List<SubroomPlacement> Subrooms;

            /// <summary>True if there's leftover space after fitting subrooms.</summary>
            public bool HasWaste;

            /// <summary>X coordinate where waste area starts (MinX for left waste, or end of last subroom for right waste).</summary>
            public int WasteMinX;

            /// <summary>Width of the waste area in cells.</summary>
            public int WasteWidth;

            /// <summary>True if waste is on the left (MinX) side of the region, false if on right (MaxX) side.</summary>
            public bool WasteOnLeft;
        }

        /// <summary>
        /// Fits subrooms into a region, maximizing count then filling waste.
        ///
        /// IMPORTANT: Subrooms do NOT overlap. Walls between adjacent subrooms are spawned
        /// separately and occupy 1 cell each. This prevents prefab conflicts.
        ///
        /// Formula: N subrooms of width W + (N-1) walls = regionWidth
        ///          N*W + N - 1 = regionWidth
        ///          N*(W+1) = regionWidth + 1
        ///          N = (regionWidth + 1) / (W + 1)
        /// </summary>
        public static List<SubroomPlacement> FitSubrooms(
            Region region,
            int stripMinZ,
            int stripDepth,
            PlacementRotation facing,
            List<int> availableWidths,
            SimpleRect room)
        {
            var subrooms = new List<SubroomPlacement>();

            int regionWidth = region.Width;
            int minWidth = availableWidths.Min();
            int maxWidth = availableWidths.Max();

            // Can't fit any subrooms if region is too small
            if (regionWidth < minWidth)
            {
                return subrooms;
            }

            // Calculate how many subrooms we can fit (non-overlapping, with walls between)
            // N subrooms + (N-1) walls = regionWidth
            // N*W + (N-1) = regionWidth
            // N*(W+1) - 1 = regionWidth
            // N = (regionWidth + 1) / (W + 1)
            int maxCount = (regionWidth + 1) / (minWidth + 1);
            if (maxCount < 1) maxCount = 1;

            // Verify it fits: maxCount * minWidth + (maxCount - 1) walls <= regionWidth
            int cellsUsed = maxCount * minWidth + (maxCount - 1);
            while (cellsUsed > regionWidth && maxCount > 0)
            {
                maxCount--;
                cellsUsed = maxCount * minWidth + (maxCount - 1);
            }

            if (maxCount == 0 || cellsUsed > regionWidth)
            {
                return subrooms;
            }

            // Determine widths for each subroom
            // Start with all minimum width, then expand some to fill waste
            var widths = new int[maxCount];
            for (int i = 0; i < maxCount; i++)
            {
                widths[i] = minWidth;
            }

            int waste = regionWidth - cellsUsed;

            // Determine placement direction: start from the end nearest to the room edge
            // This prioritizes corner placements and puts any leftover space toward the middle
            int interiorMinX = room.MinX + 1;
            int interiorMaxX = room.MaxX - 1;
            int distToWest = region.MinX - interiorMinX;
            int distToEast = interiorMaxX - region.MaxX;
            bool placeLeftToRight = distToWest <= distToEast;

            // Expand subrooms to fill waste (1 cell at a time, respecting maxWidth)
            // ALWAYS expand from index 0 first - this is the subroom closest to the room edge
            // (index 0 = leftmost when placing left-to-right, rightmost when placing right-to-left)
            int widthIncrease = maxWidth - minWidth;
            for (int i = 0; i < maxCount && waste > 0; i++)
            {
                int expand = Math.Min(widthIncrease, waste);
                widths[i] += expand;
                waste -= expand;
            }

            // Place subrooms starting from the end nearest to the room edge
            if (placeLeftToRight)
            {
                // Place left-to-right (standard)
                int currentX = region.MinX;
                for (int i = 0; i < maxCount; i++)
                {
                    int subroomWidth = widths[i];

                    subrooms.Add(new SubroomPlacement
                    {
                        MinX = currentX,
                        MinZ = stripMinZ,
                        Width = subroomWidth,
                        Depth = stripDepth,
                        Rotation = facing,
                        PrefabDefName = GetPrefabDefName(subroomWidth, stripDepth)
                    });

                    currentX += subroomWidth + 1;
                }
            }
            else
            {
                // Place right-to-left (from east edge)
                int currentX = region.MaxX;
                for (int i = 0; i < maxCount; i++)
                {
                    int subroomWidth = widths[i];

                    subrooms.Add(new SubroomPlacement
                    {
                        MinX = currentX - subroomWidth + 1,
                        MinZ = stripMinZ,
                        Width = subroomWidth,
                        Depth = stripDepth,
                        Rotation = facing,
                        PrefabDefName = GetPrefabDefName(subroomWidth, stripDepth)
                    });

                    currentX -= subroomWidth + 1;
                }
            }

            return subrooms;
        }

        /// <summary>
        /// Gets the prefab def name for a given width and depth.
        /// </summary>
        private static string GetPrefabDefName(int width, int depth)
        {
            return $"BTG_CrewBedSubroom{width}x{depth}";
        }

        /// <summary>
        /// Internal version of FitSubrooms that also returns waste area info.
        /// </summary>
        private static FitSubroomsResult FitSubroomsWithWaste(
            Region region,
            int stripMinZ,
            int stripDepth,
            PlacementRotation facing,
            List<int> availableWidths,
            SimpleRect room)
        {
            var result = new FitSubroomsResult
            {
                Subrooms = new List<SubroomPlacement>(),
                HasWaste = false
            };

            int regionWidth = region.Width;
            int minWidth = availableWidths.Min();
            int maxWidth = availableWidths.Max();

            // Can't fit any subrooms if region is too small
            if (regionWidth < minWidth)
            {
                // Entire region is waste
                result.HasWaste = true;
                result.WasteMinX = region.MinX;
                result.WasteWidth = regionWidth;
                result.WasteOnLeft = true; // Convention: whole-region waste is "left"
                return result;
            }

            // Calculate how many subrooms we can fit (non-overlapping, with walls between)
            int maxCount = (regionWidth + 1) / (minWidth + 1);
            if (maxCount < 1) maxCount = 1;

            // Verify it fits
            int cellsUsed = maxCount * minWidth + (maxCount - 1);
            while (cellsUsed > regionWidth && maxCount > 0)
            {
                maxCount--;
                cellsUsed = maxCount * minWidth + (maxCount - 1);
            }

            if (maxCount == 0 || cellsUsed > regionWidth)
            {
                // Entire region is waste
                result.HasWaste = true;
                result.WasteMinX = region.MinX;
                result.WasteWidth = regionWidth;
                result.WasteOnLeft = true;
                return result;
            }

            // Determine widths for each subroom
            var widths = new int[maxCount];
            for (int i = 0; i < maxCount; i++)
            {
                widths[i] = minWidth;
            }

            int waste = regionWidth - cellsUsed;

            // Determine placement direction
            int interiorMinX = room.MinX + 1;
            int interiorMaxX = room.MaxX - 1;
            int distToWest = region.MinX - interiorMinX;
            int distToEast = interiorMaxX - region.MaxX;
            bool placeLeftToRight = distToWest <= distToEast;

            // Expand subrooms to fill waste (up to maxWidth)
            int widthIncrease = maxWidth - minWidth;
            for (int i = 0; i < maxCount && waste > 0; i++)
            {
                int expand = Math.Min(widthIncrease, waste);
                widths[i] += expand;
                waste -= expand;
            }

            // Track final waste position (after expansion)
            if (waste > 0)
            {
                result.HasWaste = true;
                result.WasteWidth = waste;
                result.WasteOnLeft = !placeLeftToRight; // Waste is opposite to placement direction
            }

            // Place subrooms
            if (placeLeftToRight)
            {
                int currentX = region.MinX;
                for (int i = 0; i < maxCount; i++)
                {
                    int subroomWidth = widths[i];

                    result.Subrooms.Add(new SubroomPlacement
                    {
                        MinX = currentX,
                        MinZ = stripMinZ,
                        Width = subroomWidth,
                        Depth = stripDepth,
                        Rotation = facing,
                        PrefabDefName = GetPrefabDefName(subroomWidth, stripDepth)
                    });

                    currentX += subroomWidth + 1;
                }

                // Waste is at the right end (after last subroom + wall gap)
                if (result.HasWaste)
                {
                    result.WasteMinX = currentX;
                }
            }
            else
            {
                int currentX = region.MaxX;
                for (int i = 0; i < maxCount; i++)
                {
                    int subroomWidth = widths[i];

                    result.Subrooms.Add(new SubroomPlacement
                    {
                        MinX = currentX - subroomWidth + 1,
                        MinZ = stripMinZ,
                        Width = subroomWidth,
                        Depth = stripDepth,
                        Rotation = facing,
                        PrefabDefName = GetPrefabDefName(subroomWidth, stripDepth)
                    });

                    currentX -= subroomWidth + 1;
                }

                // Waste is at the left end (before first subroom)
                if (result.HasWaste)
                {
                    result.WasteMinX = region.MinX;

                    // Account for enclosing wall that will be placed between waste and leftmost subroom.
                    // The wall is added at subroom.MinX - 1 when subroom.MinX > room.MinX + 1.
                    // This wall eats into the waste area, so reduce waste width accordingly.
                    var leftmostSubroom = result.Subrooms.OrderBy(s => s.MinX).First();
                    if (leftmostSubroom.MinX > room.MinX + 1)
                    {
                        result.WasteWidth -= 1;
                        if (result.WasteWidth <= 0)
                        {
                            result.HasWaste = false;
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Step 4: Wall Calculation

        /// <summary>
        /// Calculates wall segments for subroom enclosures.
        ///
        /// Wall types:
        /// 1. Walls between adjacent subrooms (in the 1-cell gap)
        /// 2. Enclosing walls at region edges (separating subrooms from exclusion zones)
        /// 3. Enclosing walls at room edges (only if subroom not against room wall)
        ///
        /// NOTE: Exclusion zones do NOT get corridor-side walls - they need to remain
        /// open for door access from the corridor.
        /// </summary>
        public static List<WallSegment> CalculateWalls(
            List<SubroomPlacement> subrooms,
            List<Strip> strips,
            SimpleRect room)
        {
            var walls = new List<WallSegment>();

            foreach (var strip in strips)
            {
                if (strip.Type != StripType.Subroom) continue;

                // Get subrooms in this strip, grouped by region
                var stripSubrooms = subrooms
                    .Where(s => s.MinZ == strip.MinZ)
                    .OrderBy(s => s.MinX)
                    .ToList();

                if (stripSubrooms.Count == 0) continue;

                // Process each non-exclusion region separately
                foreach (var region in strip.Regions.Where(r => !r.IsExclusionZone))
                {
                    // Get subrooms within this region
                    var regionSubrooms = stripSubrooms
                        .Where(s => s.MinX >= region.MinX && s.MinX + s.Width - 1 <= region.MaxX)
                        .OrderBy(s => s.MinX)
                        .ToList();

                    if (regionSubrooms.Count == 0) continue;

                    for (int i = 0; i < regionSubrooms.Count; i++)
                    {
                        var subroom = regionSubrooms[i];
                        bool isFirstInRegion = i == 0;
                        bool isLastInRegion = i == regionSubrooms.Count - 1;

                        // Left enclosing wall (at region start, not against room wall)
                        if (isFirstInRegion && subroom.MinX > room.MinX + 1)
                        {
                            walls.Add(new WallSegment
                            {
                                StartX = subroom.MinX - 1,
                                StartZ = strip.MinZ,
                                EndX = subroom.MinX - 1,
                                EndZ = strip.MaxZ
                            });
                        }

                        // Wall between this subroom and next (in the 1-cell gap)
                        if (!isLastInRegion)
                        {
                            int wallX = subroom.MinX + subroom.Width;
                            walls.Add(new WallSegment
                            {
                                StartX = wallX,
                                StartZ = strip.MinZ,
                                EndX = wallX,
                                EndZ = strip.MaxZ
                            });
                        }

                        // Right enclosing wall (at region end, not against room wall)
                        if (isLastInRegion && subroom.MinX + subroom.Width - 1 < room.MaxX - 1)
                        {
                            int rightX = subroom.MinX + subroom.Width;
                            walls.Add(new WallSegment
                            {
                                StartX = rightX,
                                StartZ = strip.MinZ,
                                EndX = rightX,
                                EndZ = strip.MaxZ
                            });
                        }
                    }
                }

                // NO corridor-side walls for exclusion zones!
                // Exclusion zones are meant to provide open access from corridors to doors.

                // Add horizontal back wall for strips not against room wall.
                // The prefab includes walls at z=0 (door side), but NOT at z=max (back side).
                // For strips in the middle of the room, we need to close off the back.
                //
                // For facing North: doors at z=min, back at z=max
                // For facing South: doors at z=max, back at z=min (prefab rotated 180°)
                bool needsBackWall = false;
                int backWallZ = 0;

                if (strip.Facing == PlacementRotation.North)
                {
                    // Back is at z=max; need wall if not against north room wall
                    if (strip.MaxZ < room.MaxZ - 1)
                    {
                        needsBackWall = true;
                        backWallZ = strip.MaxZ + 1; // Wall in corridor, closing off subroom back
                    }
                }
                else if (strip.Facing == PlacementRotation.South)
                {
                    // Back is at z=min; need wall if not against south room wall
                    if (strip.MinZ > room.MinZ + 1)
                    {
                        needsBackWall = true;
                        backWallZ = strip.MinZ - 1; // Wall in corridor, closing off subroom back
                    }
                }

                if (needsBackWall)
                {
                    // Add back wall for each non-exclusion region, using actual subroom bounds
                    // instead of region bounds (regions may have waste space not filled by subrooms).
                    // The back wall must also extend to cover the back of any enclosing side walls.
                    foreach (var region in strip.Regions.Where(r => !r.IsExclusionZone))
                    {
                        var regionSubroomsForWall = stripSubrooms
                            .Where(s => s.MinX >= region.MinX && s.MinX + s.Width - 1 <= region.MaxX)
                            .OrderBy(s => s.MinX)
                            .ToList();

                        if (regionSubroomsForWall.Count > 0)
                        {
                            var firstSubroom = regionSubroomsForWall.First();
                            var lastSubroom = regionSubroomsForWall.Last();

                            // Check if there are enclosing side walls that need their backs covered.
                            // Left enclosing wall exists if first subroom is not against room's west wall.
                            // Right enclosing wall exists if last subroom is not against room's east wall.
                            bool hasLeftEnclosingWall = firstSubroom.MinX > room.MinX + 1;
                            bool hasRightEnclosingWall = lastSubroom.MinX + lastSubroom.Width - 1 < room.MaxX - 1;

                            int wallStartX = hasLeftEnclosingWall
                                ? firstSubroom.MinX - 1
                                : firstSubroom.MinX;
                            int wallEndX = hasRightEnclosingWall
                                ? lastSubroom.MinX + lastSubroom.Width
                                : lastSubroom.MinX + lastSubroom.Width - 1;

                            walls.Add(new WallSegment
                            {
                                StartX = wallStartX,
                                StartZ = backWallZ,
                                EndX = wallEndX,
                                EndZ = backWallZ
                            });
                        }
                    }
                }
            }

            return walls;
        }

        #endregion
    }
}
