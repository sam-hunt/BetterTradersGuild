using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Reflection;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.WorldObjects;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Harmony patch for GenStep_OrbitalPlatform.Generate() to override layout for TradersGuild settlements.
    ///
    /// PURPOSE:
    /// Makes TradersGuild settlements use custom BTG_OrbitalSettlement layout
    /// instead of vanilla OrbitalSettlementPlatform layout.
    ///
    /// TECHNICAL APPROACH:
    /// - Prefix patch runs before vanilla Generate() executes
    /// - Checks if map parent is a TradersGuild settlement
    /// - Uses reflection to override private layoutDef field
    /// - Initializes TradersGuildSettlementComponent for cargo tracking
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
        /// 1. Check if map parent is a TradersGuild settlement
        /// 2. If yes: Override layoutDef field with custom layout
        /// 3. Initialize TradersGuildSettlementComponent for cargo tracking
        /// 4. Return true to allow vanilla Generate() to run
        ///
        /// PARAMETERS:
        /// - __instance: The GenStep_OrbitalPlatform instance
        /// - map: The map being generated
        /// - parms: Generation parameters (unused, but part of signature)
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(GenStep_OrbitalPlatform __instance, Map map, GenStepParams parms)
        {
            // STEP 1: Get settlement from map parent
            // Maps generated from settlements have their parent set to the Settlement object
            Settlement settlement = map?.Parent as Settlement;
            if (settlement == null)
            {
                return true; // Not a settlement map, continue normally
            }

            // STEP 2: Check if this is a TradersGuild settlement
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                return true; // Not TradersGuild, continue normally
            }

            // STEP 3: Initialize reflection (lazy, only once)
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

            // STEP 4: Cache custom LayoutDef (lazy lookup)
            if (tradersGuildLayoutDef == null)
            {
                tradersGuildLayoutDef = DefDatabase<LayoutDef>.GetNamedSilentFail("BTG_OrbitalSettlement");

                if (tradersGuildLayoutDef == null)
                {
                    Log.Error("[Better Traders Guild] Failed to find BTG_OrbitalSettlement LayoutDef. " +
                              "Ensure Defs/LayoutDefs/BTG_OrbitalSettlement.xml is loaded correctly.");
                    return true; // Continue with vanilla layout
                }
            }

            // STEP 5: Override layoutDef field with custom layout
            layoutDefField.SetValue(__instance, tradersGuildLayoutDef);

            Log.Message($"[Better Traders Guild] Overriding layout for TradersGuild settlement '{settlement.Name}' " +
                        $"(ID: {settlement.ID}) to use BTG_OrbitalSettlement layout.");

            // STEP 6: Initialize TradersGuildSettlementComponent for cargo tracking
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

            // Return true to allow vanilla Generate() to proceed with our custom layout
            return true;
        }
    }
}
