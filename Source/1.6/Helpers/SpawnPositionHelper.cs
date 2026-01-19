using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Helper for calculating spawn positions for multi-cell things and prefabs.
    ///
    /// RimWorld uses center-based positioning where the spawn position represents
    /// the center of the thing's occupied rect. The formula used by GenAdj.OccupiedRect is:
    ///   minX = center.x - (size.x - 1) / 2
    ///   minZ = center.z - (size.z - 1) / 2
    ///
    /// GenAdj.AdjustForRotation modifies the center position for even-sized dimensions
    /// based on rotation. For rotations other than North, the center is shifted:
    ///   - East:  (0, -1) adjustment for even dimensions
    ///   - South: (-1, -1) adjustment for even dimensions
    ///   - West:  (-1, 0) adjustment for even dimensions
    ///
    /// This helper provides the inverse calculation: given a target rect and rotation,
    /// compute the spawn position that will cause the thing to occupy that rect.
    /// </summary>
    public static class SpawnPositionHelper
    {
        /// <summary>
        /// Calculates the spawn position that will cause a prefab to occupy the given rect.
        /// This is the core algorithm - all other methods should delegate to this.
        ///
        /// Uses center = min + (size-1)/2, then applies rotation-based adjustments for even dimensions.
        /// </summary>
        /// <param name="rectMinX">Target rect minimum X coordinate</param>
        /// <param name="rectMinZ">Target rect minimum Z coordinate</param>
        /// <param name="worldSizeX">World-space width (after rotation applied)</param>
        /// <param name="worldSizeZ">World-space depth (after rotation applied)</param>
        /// <param name="rotation">Rotation as int: 0=North, 1=East, 2=South, 3=West</param>
        /// <returns>Tuple of (centerX, centerZ) for spawn position</returns>
        public static (int centerX, int centerZ) CalculateSpawnPosition(
            int rectMinX, int rectMinZ,
            int worldSizeX, int worldSizeZ,
            int rotation)
        {
            // Base formula matches GenAdj.OccupiedRect: center = min + (size-1)/2
            int centerX = rectMinX + (worldSizeX - 1) / 2;
            int centerZ = rectMinZ + (worldSizeZ - 1) / 2;

            // Apply rotation-based adjustments for even dimensions.
            // GenAdj.AdjustForRotation shifts the center for non-North rotations,
            // so we compensate by adding the inverse adjustment.
            if (rotation == 1) // East: (0, +1) for even z
            {
                if (worldSizeZ % 2 == 0) centerZ += 1;
            }
            else if (rotation == 2) // South: (+1, +1) for even x/z
            {
                if (worldSizeX % 2 == 0) centerX += 1;
                if (worldSizeZ % 2 == 0) centerZ += 1;
            }
            else if (rotation == 3) // West: (+1, 0) for even x
            {
                if (worldSizeX % 2 == 0) centerX += 1;
            }

            return (centerX, centerZ);
        }

        /// <summary>
        /// Gets the world-space size after applying rotation.
        /// For North/South (0, 2), size is unchanged.
        /// For East/West (1, 3), x and z are swapped.
        /// </summary>
        public static (int x, int z) GetRotatedSize(int localSizeX, int localSizeZ, int rotation)
        {
            bool isHorizontal = rotation == 1 || rotation == 3;
            return isHorizontal ? (localSizeZ, localSizeX) : (localSizeX, localSizeZ);
        }

        /// <summary>
        /// Calculates the spawn position for a prefab to be centered within a room rect.
        /// Convenience method for RimWorld callers - delegates to CalculateSpawnPosition.
        /// </summary>
        public static IntVec3 GetCenteredSpawnPosition(CellRect roomRect, IntVec2 prefabSize, Rot4 rotation)
        {
            var worldSize = GetRotatedSize(prefabSize.x, prefabSize.z, rotation.AsInt);

            // Calculate the target rect centered in the room
            int targetMinX = roomRect.minX + (roomRect.Width - worldSize.x) / 2;
            int targetMinZ = roomRect.minZ + (roomRect.Height - worldSize.z) / 2;

            var (centerX, centerZ) = CalculateSpawnPosition(
                targetMinX, targetMinZ,
                worldSize.x, worldSize.z,
                rotation.AsInt);

            return new IntVec3(centerX, 0, centerZ);
        }

        /// <summary>
        /// Wrapper around GenAdj.OccupiedRect for convenience.
        /// </summary>
        public static CellRect GetOccupiedRect(IntVec3 spawnPosition, IntVec2 size, Rot4 rotation)
        {
            return GenAdj.OccupiedRect(spawnPosition, rotation, size);
        }
    }
}
