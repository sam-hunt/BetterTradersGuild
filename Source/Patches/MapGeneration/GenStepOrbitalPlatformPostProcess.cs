using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Harmony postfix patch for GenStep_OrbitalPlatform.Generate() to perform
    /// post-processing after all rooms have been filled.
    ///
    /// PURPOSE:
    /// Paint all metal tiles (MetalTile + OrbitalPlatform) in TradersGuild settlements
    /// with custom "orbital steel" color AFTER all rooms have been generated and filled.
    ///
    /// ARCHITECTURE:
    /// This runs AFTER GenStep_OrbitalPlatform.Generate() completes, making it the
    /// final step in orbital platform generation. Allows structure-wide modifications
    /// without requiring custom LayoutWorker classes.
    ///
    /// LEARNING NOTE: This approach is cleaner than custom LayoutWorkers when you only
    /// need post-processing and don't need to modify the generation algorithm itself.
    /// </summary>
    [HarmonyPatch(typeof(GenStep_OrbitalPlatform))]
    [HarmonyPatch("Generate")]
    public static class GenStepOrbitalPlatformPostProcess
    {
        /// <summary>
        /// Postfix that runs after GenStep_OrbitalPlatform.Generate() completes.
        /// Paints all metal terrain in TradersGuild settlements with custom color.
        ///
        /// LEARNING NOTE: Postfix patches run AFTER the original method completes,
        /// with access to the method's parameters and return value. Perfect for
        /// post-processing without interfering with vanilla logic.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(GenStep_OrbitalPlatform __instance, Map map, GenStepParams parms)
        {
            // Check if this is a TradersGuild settlement
            Settlement settlement = map?.Parent as Settlement;
            if (settlement == null || !TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                return; // Not TradersGuild, skip post-processing
            }

            // Paint all metal tiles in the settlement
            PaintAllMetalTiles(map);

            Log.Message($"[Better Traders Guild] Post-processing completed for TradersGuild settlement '{settlement.Name}'");
        }

        /// <summary>
        /// Paints ONLY MetalTile (steel tile) terrain to the custom BTG_OrbitalSteel color.
        ///
        /// SAFETY: Only paints terrain with defName "MetalTile" specifically. Does NOT paint:
        /// - Gold tiles (GoldTile)
        /// - Silver tiles (SilverTile)
        /// - Wooden floors (WoodPlank, etc.)
        /// - OrbitalPlatform terrain (not paintable)
        /// - Any other terrain types
        ///
        /// LEARNING NOTE: Since we don't have access to room definitions at this point,
        /// we simply iterate through all cells in the map. This is efficient enough for
        /// orbital platform maps which are relatively small.
        /// </summary>
        /// <param name="map">The map being generated</param>
        private static void PaintAllMetalTiles(Map map)
        {
            // Get our custom color definition
            ColorDef orbitalSteelColor = DefDatabase<ColorDef>.GetNamed("BTG_OrbitalSteel", errorOnFail: false);
            if (orbitalSteelColor == null)
            {
                Log.Warning("[Better Traders Guild] Could not find BTG_OrbitalSteel ColorDef. Steel tiles will not be painted.");
                return;
            }

            // Get MetalTile terrain (steel tiles specifically)
            // We ONLY paint this terrain type - no other tiles will be affected
            TerrainDef metalTileTerrain = TerrainDefOf.MetalTile;

            if (metalTileTerrain == null)
            {
                Log.Warning("[Better Traders Guild] Could not find MetalTile terrain for coloring");
                return;
            }

            // Verify MetalTile is paintable (it should be in vanilla)
            if (!metalTileTerrain.isPaintable)
            {
                Log.Warning($"[Better Traders Guild] MetalTile terrain is not paintable (isPaintable=false). Cannot apply color.");
                return;
            }

            int tilesColored = 0;

            // Iterate through all cells in the map
            // LEARNING NOTE: map.AllCells returns IEnumerable<IntVec3> for all cells
            // This is more efficient than nested loops and handles map bounds automatically
            foreach (IntVec3 cell in map.AllCells)
            {
                // Check if this cell has MetalTile terrain specifically
                TerrainDef terrain = map.terrainGrid.TerrainAt(cell);

                // ONLY paint MetalTile - ignore all other terrain types
                if (terrain == metalTileTerrain)
                {
                    // Paint the terrain with our custom color!
                    // LEARNING NOTE: SetTerrainColor() sets TerrainGrid.colorGrid[cellIndex] = colorDef
                    // This overrides the terrain's default color without replacing the TerrainDef
                    map.terrainGrid.SetTerrainColor(cell, orbitalSteelColor);
                    tilesColored++;
                }
            }

            if (tilesColored > 0)
            {
                Log.Message($"[Better Traders Guild] Painted {tilesColored} MetalTile (steel) tiles with {orbitalSteelColor.defName} color in orbital settlement");

                // Regenerate map rendering to show updated colors
                // LEARNING NOTE: MapDrawer caches terrain materials, must regenerate after color changes
                map.mapDrawer.RegenerateEverythingNow();
            }
            else
            {
                Log.Message("[Better Traders Guild] No metal tiles found to paint in orbital settlement");
            }
        }
    }
}
