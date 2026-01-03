using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    /// <summary>
    /// Detects landing pads by finding and clustering beacon things.
    ///
    /// PURPOSE:
    /// Provides landing pad detection for multiple use cases:
    /// - External pads (outside structure) for pipe network extension
    /// - Internal pads (inside rooms) for selective roofing
    ///
    /// TECHNICAL APPROACH:
    /// - Finds AncientShipBeacon OR ShipLandingBeacon (Royalty) things on the map
    /// - Only clusters homogenous beacon types (all corners same type)
    /// - Clusters beacons by grid-aligned direct paths (handles large pads >50 cells)
    /// - Returns bounding rect and beacon positions for each detected pad
    /// </summary>
    public static class LandingPadDetector
    {
        /// <summary>
        /// Beacon def names to search for landing pad detection.
        /// AncientShipBeacon is vanilla, ShipLandingBeacon is from Royalty DLC.
        /// </summary>
        private static readonly string[] BeaconDefNames = new[]
        {
            "AncientShipBeacon",
            "ShipLandingBeacon"
        };
        /// <summary>
        /// Data structure representing a detected landing pad.
        /// </summary>
        public struct LandingPadInfo
        {
            public CellRect BoundingRect;
            public IntVec3 Centroid;
            public List<IntVec3> BeaconPositions;
        }

        /// <summary>
        /// Detects landing pads OUTSIDE a bounding rect (external pads).
        /// Used by LandingPadPipeExtender for pipe extension to external landing areas.
        /// </summary>
        /// <param name="map">The map to search</param>
        /// <param name="excludeRect">Rect to exclude (typically structure bounds)</param>
        /// <returns>List of landing pads found outside the exclude rect</returns>
        public static List<LandingPadInfo> DetectOutsideRect(Map map, CellRect excludeRect)
        {
            List<Thing> allBeacons = FindBeacons(map);
            if (allBeacons.Count == 0)
                return new List<LandingPadInfo>();

            // Filter to beacons OUTSIDE exclude rect
            List<Thing> filteredBeacons = allBeacons
                .Where(b => !excludeRect.Contains(b.Position))
                .ToList();

            if (filteredBeacons.Count == 0)
                return new List<LandingPadInfo>();

            return ClusterAndBuildPadInfos(map, filteredBeacons);
        }

        /// <summary>
        /// Detects landing pads INSIDE a bounding rect (internal pads).
        /// Used by RoomContents_TransportRoom for selective roofing.
        /// Returns the beacon bounding rect for the first pad found, or null.
        /// </summary>
        /// <param name="map">The map to search</param>
        /// <param name="searchRect">Rect to search within (typically room bounds)</param>
        /// <returns>Bounding rect of first landing pad found, or null if none</returns>
        public static CellRect? DetectInsideRect(Map map, CellRect searchRect)
        {
            List<Thing> allBeacons = FindBeacons(map);
            if (allBeacons.Count == 0)
                return null;

            // Filter to beacons INSIDE search rect
            List<Thing> filteredBeacons = allBeacons
                .Where(b => searchRect.Contains(b.Position))
                .ToList();

            if (filteredBeacons.Count == 0)
                return null;

            List<LandingPadInfo> pads = ClusterAndBuildPadInfos(map, filteredBeacons);
            if (pads.Count == 0)
                return null;

            // Return the bounding rect of the first (typically only) pad
            return pads[0].BoundingRect;
        }

        /// <summary>
        /// Finds all beacon things on the map (AncientShipBeacon or ShipLandingBeacon).
        /// </summary>
        private static List<Thing> FindBeacons(Map map)
        {
            List<Thing> allBeacons = new List<Thing>();

            foreach (string defName in BeaconDefNames)
            {
                ThingDef beaconDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (beaconDef != null)
                {
                    allBeacons.AddRange(map.listerThings.ThingsOfDef(beaconDef));
                }
            }

            return allBeacons;
        }

        /// <summary>
        /// Clusters beacons and builds LandingPadInfo for each cluster.
        /// Groups beacons by ThingDef first to ensure homogenous pads
        /// (all corners must be the same beacon type).
        /// </summary>
        private static List<LandingPadInfo> ClusterAndBuildPadInfos(Map map, List<Thing> beacons)
        {
            TerrainDef orbitalPlatformTerrain = DefDatabase<TerrainDef>.GetNamedSilentFail("OrbitalPlatform");
            if (orbitalPlatformTerrain == null)
                return new List<LandingPadInfo>();

            // Group beacons by their ThingDef to ensure homogenous clustering
            // (AncientShipBeacon pads stay separate from ShipLandingBeacon pads)
            Dictionary<ThingDef, List<Thing>> beaconsByDef = beacons
                .GroupBy(b => b.def)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<LandingPadInfo> landingPads = new List<LandingPadInfo>();

            // Cluster each beacon type separately
            foreach (List<Thing> sameTypeBeacons in beaconsByDef.Values)
            {
                List<List<Thing>> clusters = ClusterBeaconsByGridAlignment(map, sameTypeBeacons, orbitalPlatformTerrain);

                // Convert clusters to LandingPadInfo
                foreach (List<Thing> cluster in clusters)
                {
                    LandingPadInfo? padInfo = BuildLandingPadInfo(cluster);
                    if (padInfo.HasValue)
                    {
                        landingPads.Add(padInfo.Value);
                    }
                }
            }

            return landingPads;
        }

        /// <summary>
        /// Builds a LandingPadInfo from a cluster of beacons.
        /// </summary>
        private static LandingPadInfo? BuildLandingPadInfo(List<Thing> cluster)
        {
            if (cluster.Count == 0)
                return null;

            // Calculate bounding rect from beacon positions
            int minX = cluster.Min(b => b.Position.x);
            int maxX = cluster.Max(b => b.Position.x);
            int minZ = cluster.Min(b => b.Position.z);
            int maxZ = cluster.Max(b => b.Position.z);

            CellRect boundingRect = new CellRect(minX, minZ, maxX - minX + 1, maxZ - minZ + 1);
            IntVec3 centroid = boundingRect.CenterCell;

            return new LandingPadInfo
            {
                BoundingRect = boundingRect,
                Centroid = centroid,
                BeaconPositions = cluster.Select(b => b.Position).ToList()
            };
        }

        /// <summary>
        /// Clusters beacons that form grid-aligned rectangles with direct terrain paths.
        /// Two beacons belong to same pad if:
        /// 1. They share the same X or Z coordinate (grid-aligned on same row/column)
        /// 2. The straight-line path between them is all OrbitalPlatform terrain
        ///
        /// This correctly separates pads connected by walkways because:
        /// - Same pad: beacons at corners share X/Z coords with direct pad-edge paths
        /// - Different pads: beacons don't share coordinates (or path isn't continuous)
        ///
        /// Handles large landing pads (DoLargePlatforms) where beacons can be >50 cells apart.
        /// </summary>
        private static List<List<Thing>> ClusterBeaconsByGridAlignment(
            Map map, List<Thing> beacons, TerrainDef terrainDef)
        {
            int n = beacons.Count;
            if (n == 0)
                return new List<List<Thing>>();

            // Union-Find parent array (each beacon starts as its own cluster)
            int[] parent = new int[n];
            for (int i = 0; i < n; i++)
                parent[i] = i;

            // Find with path compression
            int Find(int x)
            {
                if (parent[x] != x)
                    parent[x] = Find(parent[x]);
                return parent[x];
            }

            // Union two clusters
            void Union(int x, int y)
            {
                int px = Find(x);
                int py = Find(y);
                if (px != py)
                    parent[px] = py;
            }

            // Check all pairs for grid-aligned direct connections
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    IntVec3 posI = beacons[i].Position;
                    IntVec3 posJ = beacons[j].Position;

                    // Only consider grid-aligned pairs (same X or same Z)
                    if (posI.x != posJ.x && posI.z != posJ.z)
                        continue;

                    // Check if direct straight-line path exists on terrain
                    if (HasDirectTerrainPath(map, posI, posJ, terrainDef))
                    {
                        Union(i, j);
                    }
                }
            }

            // Build clusters from union-find results
            Dictionary<int, List<Thing>> clusterMap = new Dictionary<int, List<Thing>>();
            for (int i = 0; i < n; i++)
            {
                int root = Find(i);
                if (!clusterMap.ContainsKey(root))
                    clusterMap[root] = new List<Thing>();
                clusterMap[root].Add(beacons[i]);
            }

            return clusterMap.Values.ToList();
        }

        /// <summary>
        /// Checks if a straight horizontal or vertical path between two cells
        /// consists entirely of the specified terrain type.
        /// Returns false if cells are not grid-aligned (neither same X nor same Z).
        /// </summary>
        private static bool HasDirectTerrainPath(Map map, IntVec3 a, IntVec3 b, TerrainDef terrainDef)
        {
            // Must be grid-aligned (same X or same Z)
            bool sameX = a.x == b.x;
            bool sameZ = a.z == b.z;

            if (!sameX && !sameZ)
                return false;

            if (sameX && sameZ)
                return true; // Same cell

            if (sameX)
            {
                // Vertical path
                int minZ = System.Math.Min(a.z, b.z);
                int maxZ = System.Math.Max(a.z, b.z);
                for (int z = minZ; z <= maxZ; z++)
                {
                    IntVec3 cell = new IntVec3(a.x, 0, z);
                    if (!cell.InBounds(map) || map.terrainGrid.TerrainAt(cell) != terrainDef)
                        return false;
                }
            }
            else
            {
                // Horizontal path
                int minX = System.Math.Min(a.x, b.x);
                int maxX = System.Math.Max(a.x, b.x);
                for (int x = minX; x <= maxX; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, a.z);
                    if (!cell.InBounds(map) || map.terrainGrid.TerrainAt(cell) != terrainDef)
                        return false;
                }
            }

            return true;
        }
    }
}
