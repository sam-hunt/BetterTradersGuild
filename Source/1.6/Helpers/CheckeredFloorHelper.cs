using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Helper class for applying checkered floor patterns during room generation.
    ///
    /// Creates a diagonal stripe pattern by cycling through terrain types based on
    /// (row + column) % terrainCount. With 3 terrain types, this produces visually
    /// interesting diagonal bands rather than a simple 2-color checkerboard.
    ///
    /// USAGE: Call ApplyCheckeredFloor() at the start of RoomContentsWorker.FillRoom()
    /// BEFORE calling base.FillRoom(), as base class may apply a single floor type.
    /// </summary>
    public static class CheckeredFloorHelper
    {
        /// <summary>
        /// Applies a checkered floor pattern to the specified rect using the provided terrain types.
        ///
        /// Pattern algorithm: terrain[(row + col) % terrainCount]
        /// This creates diagonal stripes when using 3+ terrain types.
        ///
        /// SAFETY: Validates all terrain def names and skips cells outside map bounds.
        /// Non-existent terrain defs are logged as warnings and skipped.
        /// </summary>
        /// <param name="map">The map to modify terrain on</param>
        /// <param name="rect">The rectangular area to apply the pattern to</param>
        /// <param name="terrainDefNames">List of terrain defNames to cycle through (minimum 2)</param>
        /// <returns>Number of tiles modified</returns>
        public static int ApplyCheckeredFloor(Map map, CellRect rect, List<string> terrainDefNames)
        {
            if (map == null || terrainDefNames == null || terrainDefNames.Count < 2)
            {
                Log.Warning("[Better Traders Guild] CheckeredFloorHelper requires a map and at least 2 terrain def names.");
                return 0;
            }

            // Resolve terrain defs from names
            List<TerrainDef> terrainDefs = new List<TerrainDef>();
            foreach (string defName in terrainDefNames)
            {
                TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(defName);
                if (terrain == null)
                {
                    Log.Warning($"[Better Traders Guild] CheckeredFloorHelper: Terrain def '{defName}' not found, skipping.");
                    continue;
                }
                terrainDefs.Add(terrain);
            }

            if (terrainDefs.Count < 2)
            {
                Log.Warning("[Better Traders Guild] CheckeredFloorHelper: Not enough valid terrain defs found (need at least 2).");
                return 0;
            }

            int tilesModified = 0;
            int terrainCount = terrainDefs.Count;

            // Iterate through each row and cell in the rect
            for (int z = rect.minZ; z <= rect.maxZ; z++)
            {
                int row = z - rect.minZ;

                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    int col = x - rect.minX;

                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map)) continue;

                    // Select terrain using modulo pattern
                    int terrainIndex = (row + col) % terrainCount;
                    TerrainDef terrain = terrainDefs[terrainIndex];

                    map.terrainGrid.SetTerrain(cell, terrain);
                    tilesModified++;
                }
            }

            return tilesModified;
        }
    }
}
