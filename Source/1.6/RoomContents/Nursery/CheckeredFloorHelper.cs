using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.RoomContents.Nursery
{
    /// <summary>
    /// Helper class for applying checkered floor patterns during nursery room generation.
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
        /// Applies a checkered floor pattern to the specified rect using the provided terrain defs.
        ///
        /// Pattern algorithm: terrain[(row + col) % terrainCount]
        /// This creates diagonal stripes when using 3+ terrain types.
        ///
        /// SAFETY: Skips null terrain defs and cells outside map bounds.
        /// </summary>
        /// <param name="map">The map to modify terrain on</param>
        /// <param name="rect">The rectangular area to apply the pattern to</param>
        /// <param name="terrainDefs">List of TerrainDef to cycle through (minimum 2 non-null)</param>
        /// <returns>Number of tiles modified</returns>
        public static int ApplyCheckeredFloor(Map map, CellRect rect, List<TerrainDef> terrainDefs)
        {
            if (map == null || terrainDefs == null)
            {
                Log.Warning("[Better Traders Guild] CheckeredFloorHelper requires a map and terrain defs list.");
                return 0;
            }

            // Filter out null defs
            List<TerrainDef> validDefs = new List<TerrainDef>();
            foreach (TerrainDef terrain in terrainDefs)
            {
                if (terrain != null)
                    validDefs.Add(terrain);
            }

            if (validDefs.Count < 2)
            {
                Log.Warning("[Better Traders Guild] CheckeredFloorHelper requires at least 2 valid terrain defs.");
                return 0;
            }

            int tilesModified = 0;
            int terrainCount = validDefs.Count;

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
                    TerrainDef terrain = validDefs[terrainIndex];

                    map.terrainGrid.SetTerrain(cell, terrain);
                    tilesModified++;
                }
            }

            return tilesModified;
        }
    }
}
