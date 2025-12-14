using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.Helpers.MapGeneration;
using BetterTradersGuild.WorldObjects;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Harmony patch for GenStep_OrbitalPlatform.Generate() to override layout for TradersGuild settlements.
    ///
    /// PURPOSE:
    /// Makes TradersGuild settlements use custom BTG_Settlement layout
    /// instead of vanilla OrbitalSettlementPlatform layout.
    ///
    /// TECHNICAL APPROACH:
    /// - Prefix patch runs before vanilla Generate() executes
    /// - Checks if map parent is a TradersGuild settlement
    /// - Uses reflection to override private layoutDef field
    /// - Initializes TradersGuildSettlementComponent for cargo tracking
    ///
    /// - Postfix patch runs after vanilla Generate() completes
    /// - Places hidden conduits and VE pipes under walls (via LayoutConduitPlacer)
    /// - Fills VE pipe network tanks to random levels (via PipeNetworkTankFiller)
    /// - Closes VE pipe valves and removes faction ownership (via PipeValveHandler)
    ///
    /// ARCHITECTURE:
    /// - Phase 3.2: Layout override + component initialization
    /// - Phase 3.4: SymbolResolver reads same layoutDef for cargo spawning
    /// - Phase 3.5: TradersGuildCargoRefresher reads component for refresh logic
    ///
    /// LEARNING NOTE (Harmony Prefix):
    /// Prefix patches run BEFORE the original method. They can:
    /// - Modify instance fields via reflection
    /// - Read/write method parameters (via ref)
    /// - Skip original method execution (return false)
    /// This patch modifies the layoutDef field then allows original method to run.
    /// </summary>
    [HarmonyPatch(typeof(GenStep_OrbitalPlatform))]
    [HarmonyPatch("Generate")]
    public static class GenStepOrbitalPlatformGenerate
    {
        /// <summary>
        /// Reflected FieldInfo for GenStep_OrbitalPlatform.layoutDef private field.
        /// Cached for performance (reflection is expensive).
        /// </summary>
        private static FieldInfo layoutDefField = null;

        /// <summary>
        /// Cached reference to BTG_OrbitalSettlement LayoutDef.
        /// Looked up once on first use for performance.
        /// </summary>
        private static LayoutDef tradersGuildLayoutDef = null;

        /// <summary>
        /// Harmony Prefix patch that runs before GenStep_OrbitalPlatform.Generate().
        ///
        /// EXECUTION FLOW:
        /// 1. Check if custom layouts feature enabled in mod settings
        /// 2. Check if map parent is a TradersGuild settlement
        /// 3. If yes: Override layoutDef field with custom layout
        /// 4. Initialize TradersGuildSettlementComponent for cargo tracking (if cargo system enabled)
        /// 5. Return true to allow vanilla Generate() to run
        ///
        /// PARAMETERS:
        /// - __instance: The GenStep_OrbitalPlatform instance
        /// - map: The map being generated
        /// - parms: Generation parameters (unused, but part of signature)
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(GenStep_OrbitalPlatform __instance, Map map, GenStepParams parms)
        {
            // STEP 1: Check if custom layouts feature enabled
            if (!BetterTradersGuildMod.Settings.useCustomLayouts)
            {
                // Feature disabled - use vanilla/other mod generation
                return true;
            }

            // STEP 2: Get settlement from map parent
            // Maps generated from settlements have their parent set to the Settlement object
            Settlement settlement = map?.Parent as Settlement;
            if (settlement == null)
            {
                return true; // Not a settlement map, continue normally
            }

            // STEP 3: Check if this is a TradersGuild settlement
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                return true; // Not TradersGuild, continue normally
            }

            // STEP 4: Initialize reflection (lazy, only once)
            if (layoutDefField == null)
            {
                layoutDefField = typeof(GenStep_OrbitalPlatform).GetField(
                    "layoutDef",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (layoutDefField == null)
                {
                    Log.Error("[Better Traders Guild] Failed to reflect GenStep_OrbitalPlatform.layoutDef field. " +
                              "Map generation layout override will not work.");
                    return true; // Continue with vanilla layout
                }
            }

            // STEP 5: Cache custom LayoutDef (lazy lookup)
            if (tradersGuildLayoutDef == null)
            {
                tradersGuildLayoutDef = DefDatabase<LayoutDef>.GetNamedSilentFail("BTG_Settlement");

                if (tradersGuildLayoutDef == null)
                {
                    Log.Error("[Better Traders Guild] Failed to find BTG_Settlement LayoutDef. " +
                              "Ensure Defs/LayoutDefs/BTG_OrbitalSettlement.xml is loaded correctly.");
                    return true; // Continue with vanilla layout
                }
            }

            // STEP 6: Override layoutDef field with custom layout
            layoutDefField.SetValue(__instance, tradersGuildLayoutDef);

            Log.Message($"[Better Traders Guild] Overriding layout for TradersGuild settlement '{settlement.Name}' " +
                        $"(ID: {settlement.ID}) to use BTG_Settlement layout.");

            // STEP 7: Initialize TradersGuildSettlementComponent for cargo tracking (if enabled)
            // Only add component if cargo system is enabled (percentage > 0)
            if (BetterTradersGuildMod.Settings.cargoInventoryPercentage > 0f)
            {
                // Check if component already exists (shouldn't, but safety check)
                TradersGuildSettlementComponent component = settlement.GetComponent<TradersGuildSettlementComponent>();

                if (component == null)
                {
                    // Add component to settlement
                    component = new TradersGuildSettlementComponent();
                    settlement.AllComps.Add(component);
                    component.parent = settlement;

                    Log.Message($"[Better Traders Guild] Initialized TradersGuildSettlementComponent for settlement '{settlement.Name}' " +
                                $"(ID: {settlement.ID}) to track cargo refresh state.");
                }
            }
            else
            {
                Log.Message($"[Better Traders Guild] Cargo system disabled (0%) - skipping TradersGuildSettlementComponent " +
                            $"for settlement '{settlement.Name}' (ID: {settlement.ID}).");
            }

            // Return true to allow vanilla Generate() to proceed with our custom layout
            return true;
        }

        /// <summary>
        /// Harmony Postfix patch that runs AFTER GenStep_OrbitalPlatform.Generate().
        ///
        /// PURPOSE:
        /// 1. Places hidden conduits and VE pipes under all walls (station-wide networks)
        /// 2. Fills VE pipe network tanks to random levels (operational feel)
        /// 3. Closes VE pipe valves and removes faction ownership (lockdown + claimable)
        ///
        /// ARCHITECTURE:
        /// Delegates to helper classes for single-responsibility:
        /// - LayoutConduitPlacer: Hidden conduit/pipe placement under walls
        /// - PipeNetworkTankFiller: VE tank filling via reflection
        /// - PipeValveHandler: Valve closing + faction removal via reflection
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Map map)
        {
            // STEP 1: Check if custom layouts feature enabled
            if (!BetterTradersGuildMod.Settings.useCustomLayouts)
            {
                return;
            }

            // STEP 2: Check if this is a TradersGuild settlement
            Settlement settlement = map?.Parent as Settlement;
            if (settlement == null || !TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                return;
            }

            // STEP 3: Get the most recently generated structure sketch
            // (our structure was just added during Generate())
            LayoutStructureSketch sketch = map.layoutStructureSketches?.LastOrDefault();
            if (sketch?.structureLayout == null)
            {
                Log.Warning("[Better Traders Guild] Could not find LayoutStructureSketch for conduit placement. " +
                            "Power network may not be fully connected.");
                return;
            }

            // STEP 4: Place hidden conduits (and VE hidden pipes) under all wall cells
            int conduitCount = LayoutConduitPlacer.PlaceHiddenConduits(map, sketch);

            // Build log message including VE pipe info if applicable
            var hiddenPipeDefs = HiddenPipeHelper.GetSupportedHiddenPipeDefs();
            int vePipeTypeCount = hiddenPipeDefs.Count;
            if (vePipeTypeCount > 0)
            {
                Log.Message($"[Better Traders Guild] Placed {conduitCount} conduits and " +
                            $"{conduitCount * vePipeTypeCount} VE hidden pipes ({vePipeTypeCount} type(s)) under walls " +
                            $"in settlement '{settlement.Name}' for station-wide networks.");
            }
            else
            {
                Log.Message($"[Better Traders Guild] Placed {conduitCount} conduits under walls " +
                            $"in settlement '{settlement.Name}' for station-wide power network.");
            }

            // STEP 5: Fill VE pipe network tanks to random levels
            int filledTankCount = PipeNetworkTankFiller.FillTanksOnMap(map);
            if (filledTankCount > 0)
            {
                Log.Message($"[Better Traders Guild] Filled {filledTankCount} VE pipe network tank(s) " +
                            $"in settlement '{settlement.Name}'.");
            }

            // STEP 6: Close all VE pipe valves and remove faction ownership
            // Simulates station lockdown - players must claim valves room-by-room
            int closedValveCount = PipeValveHandler.CloseAllValvesAndClearFaction(map);
            if (closedValveCount > 0)
            {
                Log.Message($"[Better Traders Guild] Closed {closedValveCount} VE pipe valve(s) " +
                            $"and removed faction ownership in settlement '{settlement.Name}'.");
            }
        }
    }
}
