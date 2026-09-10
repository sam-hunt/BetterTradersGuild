using System;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    // Chooses where a square hatch (the cargo vault hatch) goes on a station whose rooms
    // BTG did not lay out, so no room worker can hand it a spot.
    //
    // RULE: the hatch footprint plus a ring of `clearance` cells around it must all be
    // clear floor. The ring guarantees the hatch never blocks a passage or splits a room:
    // whatever could reach one side of the hatch can walk around it on the ring, so
    // connectivity is preserved by construction without any flood fill. Among valid
    // spots the one nearest `preferNear` (the station centre, so the vault reads as the
    // station's heart) wins; ties go to scan order, which is deterministic.
    //
    // The core is pure over a bool grid (unit-tested); the Verse overload samples the map
    // once per cell and delegates.
    public static class StationHatchPlacer
    {
        // Finds the centre cell for a `size`x`size` hatch inside searchRect, or
        // IntVec3.Invalid when no spot satisfies the clearance rule.
        // isClearFloor: true for a cell the hatch or its ring may occupy.
        public static IntVec3 FindBestCenter(CellRect searchRect, IntVec3 preferNear, int size, int clearance, Func<IntVec3, bool> isClearFloor)
        {
            if (searchRect.Width <= 0 || searchRect.Height <= 0)
                return IntVec3.Invalid;

            bool[,] clear = new bool[searchRect.Width, searchRect.Height];
            for (int x = 0; x < searchRect.Width; x++)
            {
                for (int z = 0; z < searchRect.Height; z++)
                    clear[x, z] = isClearFloor(new IntVec3(searchRect.minX + x, 0, searchRect.minZ + z));
            }

            (int x, int z)? local = FindBestCenter(clear, size, clearance,
                preferNear.x - searchRect.minX, preferNear.z - searchRect.minZ);

            return local.HasValue
                ? new IntVec3(searchRect.minX + local.Value.x, 0, searchRect.minZ + local.Value.z)
                : IntVec3.Invalid;
        }

        // Pure core. `clear[x, z]` is the grid; returns the local centre of the best
        // `size`x`size` window whose footprint plus `clearance` ring are all clear, nearest
        // (squared Euclidean) to (preferX, preferZ); null if none. `size` must be odd so
        // the centre is a cell.
        public static (int x, int z)? FindBestCenter(bool[,] clear, int size, int clearance, int preferX, int preferZ)
        {
            if (clear == null || size <= 0 || (size & 1) == 0 || clearance < 0)
                throw new ArgumentException("size must be a positive odd number and clearance non-negative");

            int width = clear.GetLength(0);
            int height = clear.GetLength(1);
            int half = size / 2;
            int reach = half + clearance; // window extends this far from the centre each way

            (int x, int z)? best = null;
            long bestDist = long.MaxValue;

            for (int cx = reach; cx < width - reach; cx++)
            {
                for (int cz = reach; cz < height - reach; cz++)
                {
                    long dx = cx - preferX;
                    long dz = cz - preferZ;
                    long dist = dx * dx + dz * dz;
                    if (dist >= bestDist)
                        continue; // can't beat the current best, skip the window check

                    if (WindowClear(clear, cx - reach, cz - reach, 2 * reach + 1))
                    {
                        best = (cx, cz);
                        bestDist = dist;
                    }
                }
            }

            return best;
        }

        private static bool WindowClear(bool[,] clear, int minX, int minZ, int span)
        {
            for (int x = minX; x < minX + span; x++)
            {
                for (int z = minZ; z < minZ + span; z++)
                {
                    if (!clear[x, z])
                        return false;
                }
            }
            return true;
        }
    }
}
