using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Utility class for terrain replacement and painting during map generation.
    ///
    /// Provides centralized methods for post-generation terrain processing:
    /// - Replacing ancient tiles with modern variants
    /// - Applying custom color painting to metal tiles
    ///
    /// ARCHITECTURE NOTE: These methods are designed for one-time post-generation
    /// processing, typically called from GenStep postfix patches after all vanilla
    /// generation completes.
    /// </summary>
    public static class TerrainReplacementHelper
    {
        /// <summary>
        /// Replaces all instances of a terrain type across the entire map.
        ///
        /// Optimized for one-time post-generation processing. Typically used to
        /// convert ancient/degraded aesthetics to modern maintained variants.
        ///
        /// PERFORMANCE NOTE: Full map iteration on 200×200 orbital platform (~40k cells)
        /// takes <1ms. More performant than searching for specific prefabs/structures
        /// due to cache locality and simpler code path.
        /// </summary>
        /// <param name="map">The map to process</param>
        /// <param name="oldTerrain">Terrain type to replace</param>
        /// <param name="newTerrain">Terrain type to replace with</param>
        /// <returns>Number of tiles replaced</returns>
        public static int ReplaceTerrainGlobally(
            Map map,
            TerrainDef oldTerrain,
            TerrainDef newTerrain)
        {
            if (map == null || oldTerrain == null || newTerrain == null)
            {
                return 0;
            }

            int tilesReplaced = 0;

            foreach (IntVec3 cell in map.AllCells)
            {
                if (map.terrainGrid.TerrainAt(cell) == oldTerrain)
                {
                    map.terrainGrid.SetTerrain(cell, newTerrain);
                    tilesReplaced++;
                }
            }

            return tilesReplaced;
        }

        /// <summary>
        /// Replaces terrain within specified rects.
        ///
        /// Useful for localized room-specific replacements when full map iteration
        /// is not needed. Currently unused but provided for future extensibility.
        /// </summary>
        /// <param name="map">The map to process</param>
        /// <param name="rects">Cell rectangles to process</param>
        /// <param name="oldTerrain">Terrain type to replace</param>
        /// <param name="newTerrain">Terrain type to replace with</param>
        /// <returns>Number of tiles replaced</returns>
        public static int ReplaceTerrainInRects(
            Map map,
            IEnumerable<CellRect> rects,
            TerrainDef oldTerrain,
            TerrainDef newTerrain)
        {
            if (map == null || rects == null || oldTerrain == null || newTerrain == null)
            {
                return 0;
            }

            int tilesReplaced = 0;

            foreach (CellRect rect in rects)
            {
                foreach (IntVec3 cell in rect)
                {
                    if (!cell.InBounds(map)) continue;

                    if (map.terrainGrid.TerrainAt(cell) == oldTerrain)
                    {
                        map.terrainGrid.SetTerrain(cell, newTerrain);
                        tilesReplaced++;
                    }
                }
            }

            return tilesReplaced;
        }

        /// <summary>
        /// Paints all instances of a specific terrain type with a custom color across the entire map.
        ///
        /// Applies color painting to all tiles matching the specified terrain type.
        /// Commonly used to paint MetalTile with custom orbital steel colors for
        /// consistent aesthetic throughout the settlement.
        ///
        /// SAFETY: Checks terrain.isPaintable before applying color. Non-paintable
        /// terrain types are skipped with a warning.
        ///
        /// RENDERING NOTE: Calls map.mapDrawer.RegenerateEverythingNow() after painting
        /// to flush cached terrain materials and show updated colors.
        ///
        /// LEARNING NOTE: SetTerrainColor() modifies TerrainGrid.colorGrid without
        /// replacing the TerrainDef itself. This is more efficient than creating
        /// duplicate terrain defs with different colors.
        /// </summary>
        /// <param name="map">The map to process</param>
        /// <param name="terrainDef">Terrain type to paint (e.g., MetalTile)</param>
        /// <param name="colorDef">Color to apply</param>
        /// <returns>Number of tiles painted</returns>
        public static int PaintTerrainGlobally(
            Map map,
            TerrainDef terrainDef,
            ColorDef colorDef)
        {
            if (map == null || terrainDef == null || colorDef == null)
            {
                return 0;
            }

            // Verify terrain is paintable
            if (!terrainDef.isPaintable)
            {
                Log.Warning($"[Better Traders Guild] {terrainDef.defName} terrain is not paintable (isPaintable=false). Cannot apply color.");
                return 0;
            }

            int tilesColored = 0;

            // Iterate through all cells and paint matching terrain
            foreach (IntVec3 cell in map.AllCells)
            {
                if (map.terrainGrid.TerrainAt(cell) == terrainDef)
                {
                    map.terrainGrid.SetTerrainColor(cell, colorDef);
                    tilesColored++;
                }
            }

            if (tilesColored > 0)
            {
                // Regenerate map rendering to show updated colors
                map.mapDrawer.RegenerateEverythingNow();
            }

            return tilesColored;
        }

        /// <summary>
        /// Paints terrain within specified rects with a custom color.
        ///
        /// Useful for localized room-specific painting when full map iteration
        /// is not needed. Currently unused but provided for future extensibility.
        ///
        /// SAFETY: Checks terrain.isPaintable before applying color.
        /// </summary>
        /// <param name="map">The map to process</param>
        /// <param name="rects">Cell rectangles to process</param>
        /// <param name="terrainDef">Terrain type to paint</param>
        /// <param name="colorDef">Color to apply</param>
        /// <returns>Number of tiles painted</returns>
        public static int PaintTerrainInRects(
            Map map,
            IEnumerable<CellRect> rects,
            TerrainDef terrainDef,
            ColorDef colorDef)
        {
            if (map == null || rects == null || terrainDef == null || colorDef == null)
            {
                return 0;
            }

            // Verify terrain is paintable
            if (!terrainDef.isPaintable)
            {
                Log.Warning($"[Better Traders Guild] {terrainDef.defName} terrain is not paintable (isPaintable=false). Cannot apply color.");
                return 0;
            }

            int tilesColored = 0;

            foreach (CellRect rect in rects)
            {
                foreach (IntVec3 cell in rect)
                {
                    if (!cell.InBounds(map)) continue;

                    if (map.terrainGrid.TerrainAt(cell) == terrainDef)
                    {
                        map.terrainGrid.SetTerrainColor(cell, colorDef);
                        tilesColored++;
                    }
                }
            }

            if (tilesColored > 0)
            {
                // Regenerate map rendering to show updated colors
                map.mapDrawer.RegenerateEverythingNow();
            }

            return tilesColored;
        }
    }
}
