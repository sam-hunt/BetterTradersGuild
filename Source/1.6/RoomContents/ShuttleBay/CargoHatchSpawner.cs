using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using Verse;

namespace BetterTradersGuild.RoomContents.ShuttleBay
{
    /// <summary>
    /// Handles spawning of the cargo vault hatch in the ShuttleBay room.
    ///
    /// The hatch is a 3x3 hackable portal that leads to a secure cargo vault.
    /// It should be placed in the center of the largest free area remaining
    /// after the shuttle landing pad has been spawned.
    /// </summary>
    public static class CargoVaultHatchSpawner
    {
        private const int HATCH_SIZE = 3;

        /// <summary>
        /// Finds the best position for the cargo hatch and spawns it.
        /// Returns the CellRect of the spawned hatch for blocking purposes, or default if spawn failed.
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="roomRect">The room's bounding rectangle.</param>
        /// <param name="excludedRect">Area to exclude (e.g., landing pad). Pass default/empty to exclude nothing.</param>
        /// <returns>The CellRect occupied by the hatch, or default if spawning failed.</returns>
        public static CellRect SpawnHatch(Map map, CellRect roomRect, CellRect excludedRect)
        {
            if (Things.BTG_CargoVaultHatch == null)
                return default;

            IntVec3 position = FindBestPosition(map, roomRect, excludedRect);
            if (!position.IsValid)
            {
                Log.Warning("[Better Traders Guild] Could not find valid position for cargo hatch in ShuttleBay.");
                return default;
            }

            Thing hatch = ThingMaker.MakeThing(Things.BTG_CargoVaultHatch);
            GenSpawn.Spawn(hatch, position, map, WipeMode.VanishOrMoveAside);

            return GetBlockingRectFromCenter(position);
        }

        /// <summary>
        /// Calculates the blocking rect for the cargo hatch without spawning.
        /// Use this to reserve space before base.FillRoom() is called.
        /// </summary>
        public static CellRect CalculateBlockingRect(Map map, CellRect roomRect, CellRect excludedRect)
        {
            IntVec3 position = FindBestPosition(map, roomRect, excludedRect);
            if (!position.IsValid)
                return default;

            return GetBlockingRectFromCenter(position);
        }

        /// <summary>
        /// Calculates the blocking rect from a center position.
        /// Multi-cell things spawn from their center, so we expand outward.
        /// </summary>
        private static CellRect GetBlockingRectFromCenter(IntVec3 center)
        {
            int halfSize = HATCH_SIZE / 2;
            return new CellRect(center.x - halfSize, center.z - halfSize, HATCH_SIZE, HATCH_SIZE);
        }

        /// <summary>
        /// Checks if inner rect is fully contained within outer bounds.
        /// </summary>
        private static bool IsRectWithinBounds(CellRect inner, CellRect outer)
        {
            return inner.minX >= outer.minX && inner.maxX <= outer.maxX &&
                   inner.minZ >= outer.minZ && inner.maxZ <= outer.maxZ;
        }

        /// <summary>
        /// Finds the best position for a 3x3 hatch by centering it in the largest
        /// free rectangular region of the room.
        /// </summary>
        private static IntVec3 FindBestPosition(Map map, CellRect roomRect, CellRect excludedRect)
        {
            // Find the largest free region by checking candidate areas
            // The room is divided into potential regions based on the excluded rect position
            var freeRegions = FindFreeRegions(roomRect, excludedRect);

            // Sort by area descending to try largest regions first
            freeRegions.Sort((a, b) => b.Area.CompareTo(a.Area));

            foreach (var region in freeRegions)
            {
                IntVec3 position = FindCenteredPosition(map, region);
                if (position.IsValid)
                    return position;
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Finds rectangular free regions in the room by subtracting the excluded area.
        /// Returns up to 4 regions (above, below, left, right of excluded rect).
        /// </summary>
        private static List<CellRect> FindFreeRegions(CellRect roomRect, CellRect excludedRect)
        {
            var regions = new List<CellRect>();

            // Interior rect (1 cell margin from walls for hatch clearance)
            CellRect interior = roomRect.ContractedBy(1);

            if (excludedRect.Width == 0 || excludedRect.Height == 0)
            {
                // No exclusion - entire interior is free
                regions.Add(interior);
                return regions;
            }

            // Region to the LEFT of excluded area
            if (excludedRect.minX > interior.minX)
            {
                var leftRegion = new CellRect(
                    interior.minX,
                    interior.minZ,
                    excludedRect.minX - interior.minX,
                    interior.Height
                );
                if (leftRegion.Width >= HATCH_SIZE && leftRegion.Height >= HATCH_SIZE)
                    regions.Add(leftRegion);
            }

            // Region to the RIGHT of excluded area
            if (excludedRect.maxX < interior.maxX)
            {
                var rightRegion = new CellRect(
                    excludedRect.maxX + 1,
                    interior.minZ,
                    interior.maxX - excludedRect.maxX,
                    interior.Height
                );
                if (rightRegion.Width >= HATCH_SIZE && rightRegion.Height >= HATCH_SIZE)
                    regions.Add(rightRegion);
            }

            // Region BELOW excluded area (between left and right edges of excluded)
            if (excludedRect.minZ > interior.minZ)
            {
                var belowRegion = new CellRect(
                    excludedRect.minX,
                    interior.minZ,
                    excludedRect.Width,
                    excludedRect.minZ - interior.minZ
                );
                if (belowRegion.Width >= HATCH_SIZE && belowRegion.Height >= HATCH_SIZE)
                    regions.Add(belowRegion);
            }

            // Region ABOVE excluded area (between left and right edges of excluded)
            if (excludedRect.maxZ < interior.maxZ)
            {
                var aboveRegion = new CellRect(
                    excludedRect.minX,
                    excludedRect.maxZ + 1,
                    excludedRect.Width,
                    interior.maxZ - excludedRect.maxZ
                );
                if (aboveRegion.Width >= HATCH_SIZE && aboveRegion.Height >= HATCH_SIZE)
                    regions.Add(aboveRegion);
            }

            return regions;
        }

        /// <summary>
        /// Finds a valid centered position for the hatch within the given region.
        /// Centers on the short axis, but adds random offset along the long axis
        /// to leave contiguous space for other prefabs.
        /// </summary>
        private static IntVec3 FindCenteredPosition(Map map, CellRect region)
        {
            // Start with region center, then offset along longer axis
            IntVec3 position = GetOffsetPosition(region);
            CellRect hatchRect = GetBlockingRectFromCenter(position);

            // Validate all cells are clear
            if (IsValidPlacement(map, hatchRect))
                return position;

            // Try nearby positions in a spiral pattern if preferred position is blocked
            foreach (var offset in GetSpiralOffsets(3))
            {
                IntVec3 candidate = new IntVec3(position.x + offset.x, 0, position.z + offset.z);
                CellRect candidateRect = GetBlockingRectFromCenter(candidate);

                // Ensure candidate rect is within region bounds
                if (!IsRectWithinBounds(candidateRect, region))
                    continue;

                if (IsValidPlacement(map, candidateRect))
                    return candidate;
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Calculates hatch position: centered on short axis, random offset on long axis.
        /// This leaves more contiguous space for other prefabs to spawn.
        /// </summary>
        private static IntVec3 GetOffsetPosition(CellRect region)
        {
            IntVec3 center = region.CenterCell;

            int widthSlack = region.Width - HATCH_SIZE;
            int heightSlack = region.Height - HATCH_SIZE;

            // Only offset if one axis has significantly more slack (3+ cells difference)
            if (heightSlack > widthSlack + 2)
            {
                // Height is longer - offset along z-axis
                int maxOffset = heightSlack / 2;
                int offset = Rand.RangeInclusive(-maxOffset, maxOffset);
                return new IntVec3(center.x, 0, center.z + offset);
            }
            else if (widthSlack > heightSlack + 2)
            {
                // Width is longer - offset along x-axis
                int maxOffset = widthSlack / 2;
                int offset = Rand.RangeInclusive(-maxOffset, maxOffset);
                return new IntVec3(center.x + offset, 0, center.z);
            }

            // Axes are similar length - stay centered
            return center;
        }

        /// <summary>
        /// Checks if all cells in the rect are valid for hatch placement.
        /// Cells must be in bounds, not contain walls/doors/impassables.
        /// </summary>
        private static bool IsValidPlacement(Map map, CellRect rect)
        {
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                    return false;

                // Check for walls, doors, or other impassable buildings
                Building building = cell.GetEdifice(map);
                if (building != null)
                    return false;

                // Check terrain is standable
                if (!cell.Standable(map))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Generates offset positions in a spiral pattern from center.
        /// Used to find alternative positions if center is blocked.
        /// </summary>
        private static IEnumerable<IntVec3> GetSpiralOffsets(int maxRadius)
        {
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                // Top edge (left to right)
                for (int x = -radius; x <= radius; x++)
                    yield return new IntVec3(x, 0, radius);

                // Right edge (top to bottom, excluding corner)
                for (int z = radius - 1; z >= -radius; z--)
                    yield return new IntVec3(radius, 0, z);

                // Bottom edge (right to left, excluding corner)
                for (int x = radius - 1; x >= -radius; x--)
                    yield return new IntVec3(x, 0, -radius);

                // Left edge (bottom to top, excluding corners)
                for (int z = -radius + 1; z < radius; z++)
                    yield return new IntVec3(-radius, 0, z);
            }
        }
    }
}
