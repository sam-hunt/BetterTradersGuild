using System.Collections.Generic;
using Verse;
using BetterTradersGuild.DefRefs;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Handles carpet color customization for CrewQuarters subrooms.
    /// Each subroom gets a random carpet color from a curated neutral/muted palette.
    ///
    /// USAGE: Call Customize() after subroom prefabs are spawned and their rects are stored.
    /// This paints the entire interior of each subroom with a randomly selected carpet.
    /// </summary>
    internal static class SubroomCarpetCustomizer
    {
        /// <summary>
        /// Curated subset of carpet colors for crew quarters subrooms.
        /// These are neutral and muted tones that work well in residential spaces.
        /// Lazily built to ensure DefOf initialization has completed.
        /// </summary>
        private static List<TerrainDef> _carpetOptions;
        private static List<TerrainDef> CarpetOptions => _carpetOptions ?? (_carpetOptions = BuildCarpetOptions());

        private static List<TerrainDef> BuildCarpetOptions()
        {
            var options = new List<TerrainDef>();

            // Add all available carpet options (null-safe)
            if (Terrains.CarpetGranite != null) options.Add(Terrains.CarpetGranite);
            if (Terrains.CarpetMarble != null) options.Add(Terrains.CarpetMarble);
            if (Terrains.CarpetSlate != null) options.Add(Terrains.CarpetSlate);
            if (Terrains.CarpetSandstone != null) options.Add(Terrains.CarpetSandstone);
            if (Terrains.CarpetBlack != null) options.Add(Terrains.CarpetBlack);
            if (Terrains.CarpetBlueSubtle != null) options.Add(Terrains.CarpetBlueSubtle);
            if (Terrains.CarpetPurpleSubtle != null) options.Add(Terrains.CarpetPurpleSubtle);
            if (Terrains.CarpetGreyDark != null) options.Add(Terrains.CarpetGreyDark);
            if (Terrains.CarpetGreenFaded != null) options.Add(Terrains.CarpetGreenFaded);

            if (options.Count == 0)
            {
                Log.Warning("[Better Traders Guild] SubroomCarpetCustomizer: No carpet terrain defs found. Carpet customization disabled.");
            }

            return options;
        }

        /// <summary>
        /// Applies a random carpet color to each subroom.
        /// Each subroom gets its own independently chosen color from the curated palette.
        ///
        /// SAFETY: Skips cells outside map bounds and handles empty carpet options gracefully.
        /// </summary>
        /// <param name="map">The map to modify terrain on</param>
        /// <param name="subroomRects">List of subroom bounds (as CellRect) to paint</param>
        /// <returns>Total number of tiles modified across all subrooms</returns>
        internal static int Customize(Map map, List<CellRect> subroomRects)
        {
            if (map == null || subroomRects == null || subroomRects.Count == 0)
                return 0;

            if (CarpetOptions.Count == 0)
                return 0;

            int totalTilesModified = 0;

            foreach (CellRect subroomRect in subroomRects)
            {
                // Select a random carpet color for this subroom
                TerrainDef carpetDef = CarpetOptions.RandomElement();

                // Paint all cells within the subroom bounds
                totalTilesModified += PaintRect(map, subroomRect, carpetDef);
            }

            return totalTilesModified;
        }

        /// <summary>
        /// Paints all cells in a rect with the specified terrain.
        /// </summary>
        private static int PaintRect(Map map, CellRect rect, TerrainDef terrain)
        {
            int tilesModified = 0;

            for (int z = rect.minZ; z <= rect.maxZ; z++)
            {
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map)) continue;

                    map.terrainGrid.SetTerrain(cell, terrain);
                    tilesModified++;
                }
            }

            return tilesModified;
        }
    }
}
