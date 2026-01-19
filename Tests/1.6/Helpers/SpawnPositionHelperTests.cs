using Xunit;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.Tests.Helpers
{
    /// <summary>
    /// Tests for SpawnPositionHelper.CalculateSpawnPosition pure function.
    /// Tests the actual implementation without RimWorld assembly dependencies.
    /// Test assertions derived from observed in-game behavior.
    /// </summary>
    public class SpawnPositionHelperTests
    {
        /// <summary>
        /// Helper that calls the actual SpawnPositionHelper.CalculateSpawnPosition,
        /// handling rotation-based size swapping first.
        /// </summary>
        private static (int x, int z) GetSpawnPositionForRect(
            int rectMinX, int rectMinZ, int rectWidth, int rectHeight,
            int localSizeX, int localSizeZ,
            int rotation) // 0=North, 1=East, 2=South, 3=West
        {
            // Get world-space size after rotation
            var worldSize = SpawnPositionHelper.GetRotatedSize(localSizeX, localSizeZ, rotation);

            // Call the actual implementation
            return SpawnPositionHelper.CalculateSpawnPosition(
                rectMinX, rectMinZ,
                worldSize.x, worldSize.z,
                rotation);
        }

        #region 3x3 Prefab Tests

        /*
         *  3x3 prefab placing into rect (0,0) to (2,2)
         *  Expected spawn position: (1, 1) for all rotations
         *  (Square prefabs have same world size regardless of rotation)
         *
         *  z=2  ■ ■ ■
         *  z=1  ■ C ■  ← Center at (1,1)
         *  z=0  ■ ■ ■
         *     x=0 1 2
         */

        [Fact]
        public void GetSpawnPositionForRect_3x3Prefab_NorthRotation_ReturnsCenterAt1_1()
        {
            var result = GetSpawnPositionForRect(
                rectMinX: 0, rectMinZ: 0, rectWidth: 3, rectHeight: 3,
                localSizeX: 3, localSizeZ: 3,
                rotation: 0); // North

            Assert.Equal(1, result.x);
            Assert.Equal(1, result.z);
        }

        [Fact]
        public void GetSpawnPositionForRect_3x3Prefab_EastRotation_ReturnsCenterAt1_1()
        {
            var result = GetSpawnPositionForRect(
                rectMinX: 0, rectMinZ: 0, rectWidth: 3, rectHeight: 3,
                localSizeX: 3, localSizeZ: 3,
                rotation: 1); // East

            Assert.Equal(1, result.x);
            Assert.Equal(1, result.z);
        }

        [Fact]
        public void GetSpawnPositionForRect_3x3Prefab_SouthRotation_ReturnsCenterAt1_1()
        {
            var result = GetSpawnPositionForRect(
                rectMinX: 0, rectMinZ: 0, rectWidth: 3, rectHeight: 3,
                localSizeX: 3, localSizeZ: 3,
                rotation: 2); // South

            Assert.Equal(1, result.x);
            Assert.Equal(1, result.z);
        }

        [Fact]
        public void GetSpawnPositionForRect_3x3Prefab_WestRotation_ReturnsCenterAt1_1()
        {
            var result = GetSpawnPositionForRect(
                rectMinX: 0, rectMinZ: 0, rectWidth: 3, rectHeight: 3,
                localSizeX: 3, localSizeZ: 3,
                rotation: 3); // West

            Assert.Equal(1, result.x);
            Assert.Equal(1, result.z);
        }

        #endregion

        #region 2x4 Prefab Tests

        /*
         *  2x4 prefab placing into rect (0,0) to (1,3)
         *  North rotation: localSize (2,4) → worldSize (2,4)
         *  Expected spawn position: (0, 1)
         *
         *  z=3  ■ ■
         *  z=2  ■ ■
         *  z=1  C ■  ← Center at (0,1)
         *  z=0  ■ ■
         *     x=0 1
         */

        [Fact]
        public void GetSpawnPositionForRect_2x4Prefab_NorthRotation_ReturnsCenterAt0_1()
        {
            var result = GetSpawnPositionForRect(
                rectMinX: 0, rectMinZ: 0, rectWidth: 2, rectHeight: 4,
                localSizeX: 2, localSizeZ: 4,
                rotation: 0); // North

            Assert.Equal(0, result.x);
            Assert.Equal(1, result.z);
        }

        /*
         *  2x4 prefab placing into rect (0,0) to (1,3)
         *  South rotation: localSize (2,4) → worldSize (2,4)
         *  Expected spawn position: (1, 0, 2)
         *
         *  z=3  ■ ■
         *  z=2  ■ C  ← Center at (1,2)
         *  z=1  ■ ■
         *  z=0  ■ ■
         *     x=0 1
         */

        [Fact]
        public void GetSpawnPositionForRect_2x4Prefab_SouthRotation_ReturnsCenterAt1_3()
        {
            var result = GetSpawnPositionForRect(
                rectMinX: 0, rectMinZ: 0, rectWidth: 2, rectHeight: 4,
                localSizeX: 2, localSizeZ: 4,
                rotation: 2); // South

            Assert.Equal(1, result.x);
            Assert.Equal(2, result.z);
        }

        #endregion
    }
}
