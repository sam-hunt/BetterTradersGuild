using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.Helpers.MapGeneration;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Harmony postfix patch for GenStep_OrbitalPlatform.Generate() to perform
    /// post-processing after all rooms have been filled.
    ///
    /// PURPOSE:
    /// 1. Replace all ancient tiles with modern metal tiles (from Gauss cannons,
    ///    corridors, exterior prefabs, etc.)
    /// 2. Paint all metal tiles with custom "orbital steel" color for consistent aesthetic
    /// 3. Set all WallLamps to blue glow color (powered alternative to AncientEmergencyLight_Blue)
    ///
    /// ARCHITECTURE:
    /// This runs AFTER GenStep_OrbitalPlatform.Generate() completes, making it the
    /// final step in orbital platform generation. Allows structure-wide modifications
    /// without requiring custom LayoutWorker classes or complex Harmony transpilers.
    ///
    /// PERFORMANCE NOTE: Full map iteration on 200×200 orbital platform (~40k cells)
    /// takes <1ms. More performant and maintainable than tracking individual prefab
    /// placement rects.
    ///
    /// LEARNING NOTE: This approach is cleaner than custom LayoutWorkers or transpiler
    /// patches when you only need post-processing and don't need to modify the
    /// generation algorithm itself.
    /// </summary>
    [HarmonyPatch(typeof(GenStep_OrbitalPlatform))]
    [HarmonyPatch("Generate")]
    public static class GenStepOrbitalPlatformPostProcess
    {
        /// <summary>
        /// Postfix that runs after GenStep_OrbitalPlatform.Generate() completes.
        ///
        /// Replaces ancient tiles with modern variants, then paints metal tiles
        /// with custom color for TradersGuild settlements.
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

            // Step 1: Replace ancient tiles with modern metal tiles
            // This catches tiles from all sources:
            // - Gauss cannon landing pads (spawned by SpawnCannons)
            // - Corridor floors (spawned by RoomContents_Orbital_Corridor base class)
            // - Exterior prefab terrain
            // - Any other ancient tile sources
            TerrainReplacementHelper.ReplaceTerrainGlobally(map, Terrains.AncientTile, Terrains.MetalTile);

            // Step 2: Paint all metal tiles with custom orbital steel color
            if (Colors.BTG_OrbitalSteel != null)
            {
                TerrainReplacementHelper.PaintTerrainGlobally(map, Terrains.MetalTile, Colors.BTG_OrbitalSteel);
            }
            else
            {
                Log.Warning("[Better Traders Guild] Colors.BTG_OrbitalSteel is null. Metal tiles will not be painted.");
            }

            // Step 3: Set all WallLamps to blue color (matches AncientEmergencyLight_Blue aesthetic)
            // WallLamp is used instead of inherited AncientEmergencyLight_Blue to utilize
            // the settlement's power grid. Color is set via CompGlower's public API.
            ColorInt blueGlowColor = new ColorInt(187, 187, 221); // AncientEmergencyLight_Blue glow color

            foreach (Thing lamp in map.listerThings.ThingsOfDef(Things.WallLamp))
            {
                CompGlower glower = lamp.TryGetComp<CompGlower>();
                if (glower != null)
                {
                    glower.GlowColor = blueGlowColor;
                }
            }
        }
    }
}
