using BetterTradersGuild.LayoutWorkers.Settlement;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// GenStep that extends VE pipe networks from the main settlement structure
    /// to external landing pads.
    ///
    /// No XML parameters - uses the layout structure sketch from the map to
    /// determine where pipes should be extended.
    ///
    /// IMPORTANT: This GenStepDef should have MayRequire="OskarPotocki.VFE.Core"
    /// so it skips early when Vanilla Expanded Framework is not installed.
    ///
    /// TECHNICAL APPROACH:
    /// - Uses LandingPadDetector to find external landing pads via beacon markers
    /// - Traces BFS paths from pad edges back to main structure following terrain
    /// - Places visible VE pipes along the paths for thematic realism
    ///
    /// This enables traders' guild refueling stations to connect fluid resource
    /// networks (chemfuel, nutrient paste, oxygen, astrofuel) to ship landing areas.
    /// </summary>
    public class GenStep_ExtendLandingPadPipes : GenStep
    {
        /// <summary>
        /// Deterministic seed for this GenStep.
        /// </summary>
        public override int SeedPart => 847291003;

        /// <summary>
        /// Extends VE pipes from the settlement structure to external landing pads.
        /// </summary>
        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null)
                return;

            // Get the layout structure sketch from the map
            // This was added by GenStep_OrbitalPlatform during structure generation
            LayoutStructureSketch sketch = GetLayoutSketch(map);
            if (sketch == null)
                return;

            // Delegate to the helper for complex BFS pathfinding logic
            LandingPadPipeExtender.ExtendPipesToLandingPads(map, sketch);
        }

        /// <summary>
        /// Gets the first layout structure sketch from the map.
        /// </summary>
        private LayoutStructureSketch GetLayoutSketch(Map map)
        {
            if (map.layoutStructureSketches == null || map.layoutStructureSketches.Count == 0)
                return null;

            return map.layoutStructureSketches[0];
        }
    }
}
