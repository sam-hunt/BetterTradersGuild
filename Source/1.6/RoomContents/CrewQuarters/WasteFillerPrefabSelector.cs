using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Selects appropriate waste filler prefabs based on size requirements.
    ///
    /// Design goals:
    /// 1. Support multiple variants per size (for visual variety)
    /// 2. Automatic DLC checking (RimWorld's [MayRequire] on XML defs handles this)
    /// 3. Dynamic discovery (new prefabs added via XML are automatically found)
    ///
    /// Prefab naming convention: BTG_CrewQuarters*{width}x{depth}*
    /// Examples:
    /// - BTG_CrewQuartersIndustrialShelves1x4
    /// - BTG_CrewQuartersLockers1x5
    /// - BTG_CrewQuartersPasteDiner2x5 (VNutrientE)
    ///
    /// The selector scans DefDatabase<PrefabDef> on first use and caches results.
    /// DLC-dependent prefabs that use [MayRequire] in XML will simply not be in
    /// DefDatabase if the required DLC isn't loaded.
    /// </summary>
    public static class WasteFillerPrefabSelector
    {
        /// <summary>
        /// Prefix for waste filler prefab defNames.
        /// Prefabs matching this prefix will be considered for selection.
        /// </summary>
        private const string PREFAB_PREFIX = "BTG_CrewQuarters";

        /// <summary>
        /// Cache of available prefabs grouped by (width, depth).
        /// Key format: "WxD" (e.g., "1x4", "2x5")
        /// </summary>
        private static Dictionary<string, List<PrefabDef>> prefabsBySize;

        /// <summary>
        /// Selects a random waste filler prefab of the specified size.
        /// </summary>
        /// <param name="width">Width of the waste area (1 or 2).</param>
        /// <param name="depth">Depth of the waste area (4 or 5).</param>
        /// <returns>A PrefabDef to spawn, or null if no matching prefab is available.</returns>
        public static PrefabDef SelectPrefab(int width, int depth)
        {
            EnsureInitialized();

            string sizeKey = GetSizeKey(width, depth);

            if (!prefabsBySize.TryGetValue(sizeKey, out var candidates) || candidates.Count == 0)
            {
                return null;
            }

            // Random selection using RimWorld's seeded random
            return candidates.RandomElement();
        }

        /// <summary>
        /// Gets all available prefabs for the specified size.
        /// Useful for debugging or for callers that want to implement their own selection logic.
        /// </summary>
        /// <param name="width">Width of the waste area.</param>
        /// <param name="depth">Depth of the waste area.</param>
        /// <returns>List of available prefabs, or empty list if none available.</returns>
        public static List<PrefabDef> GetAvailablePrefabs(int width, int depth)
        {
            EnsureInitialized();

            string sizeKey = GetSizeKey(width, depth);

            if (prefabsBySize.TryGetValue(sizeKey, out var candidates))
            {
                return new List<PrefabDef>(candidates);
            }

            return new List<PrefabDef>();
        }

        /// <summary>
        /// Checks if any waste filler prefabs are available for the specified size.
        /// </summary>
        /// <param name="width">Width of the waste area.</param>
        /// <param name="depth">Depth of the waste area.</param>
        /// <returns>True if at least one prefab is available.</returns>
        public static bool HasPrefabsForSize(int width, int depth)
        {
            EnsureInitialized();

            string sizeKey = GetSizeKey(width, depth);
            return prefabsBySize.TryGetValue(sizeKey, out var candidates) && candidates.Count > 0;
        }

        /// <summary>
        /// Forces re-scanning of available prefabs.
        /// Call this if prefabs are dynamically added/removed after initial load.
        /// </summary>
        public static void Refresh()
        {
            prefabsBySize = null;
            EnsureInitialized();
        }

        /// <summary>
        /// Ensures the prefab cache is initialized by scanning DefDatabase.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (prefabsBySize != null)
                return;

            prefabsBySize = new Dictionary<string, List<PrefabDef>>();

            // Scan all PrefabDefs that match our naming convention
            foreach (var prefab in DefDatabase<PrefabDef>.AllDefsListForReading)
            {
                if (prefab.defName == null || !prefab.defName.StartsWith(PREFAB_PREFIX))
                    continue;

                // Skip the subroom prefabs (they have different naming)
                if (prefab.defName.Contains("Subroom"))
                    continue;

                // Get size from the prefab's size field
                // PrefabDef.size is IntVec2 where x=width, z=depth
                int width = prefab.size.x;
                int depth = prefab.size.z;

                // Only consider waste-filler-sized prefabs (1-2 width, 4-5 depth)
                if (width < 1 || width > 2 || depth < 4 || depth > 5)
                    continue;

                string sizeKey = GetSizeKey(width, depth);

                if (!prefabsBySize.TryGetValue(sizeKey, out var list))
                {
                    list = new List<PrefabDef>();
                    prefabsBySize[sizeKey] = list;
                }

                list.Add(prefab);
            }
        }

        /// <summary>
        /// Creates a cache key from width and depth.
        /// </summary>
        private static string GetSizeKey(int width, int depth)
        {
            return $"{width}x{depth}";
        }
    }
}
