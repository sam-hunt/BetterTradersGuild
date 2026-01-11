using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    /// <summary>
    /// Manages VE Pipes hidden conduit ThingDef discovery and caching.
    ///
    /// PURPOSE:
    /// Provides a centralized way to discover which VE hidden pipe defs are available
    /// based on which VE mods are installed. Only returns defs for networks where
    /// we provide tank+valve prefabs (ensuring the pipe network is meaningful to players).
    ///
    /// DESIGN NOTE:
    /// We use explicit defs from DefRefs/Things.cs rather than dynamic discovery because:
    /// 1. We only want pipes for networks where we provide tank+valve prefabs
    /// 2. Dynamic detection could spawn pipes for networks with no usable supply
    /// 3. Being explicit ensures pipe networks are always meaningful to players
    ///
    /// SUPPORTED NETWORKS:
    /// - VE Chemfuel: Chemfuel networks
    /// - VE Nutrient Paste: Nutrient paste network
    /// - VE Gravships: Oxygen and Astrofuel networks
    ///
    /// EXCLUDED:
    /// - VREA Neutroamine: No valve exists, so no prefab, so no hidden pipes
    /// - VRE Sanguophage Hemogen: Traders aren't sanguophages; hemogen pipes contain
    ///   gold which incentivizes immersion-breaking deconstruction
    /// </summary>
    public static class HiddenPipeHelper
    {
        /// <summary>
        /// Cached list of hidden pipe ThingDefs from supported Vanilla Expanded mods.
        /// Null entries (mod not installed) are filtered out.
        /// </summary>
        private static List<ThingDef> cachedHiddenPipeDefs = null;

        /// <summary>
        /// Flag indicating whether hidden pipe defs have been initialized.
        /// Separate from null check because an empty list is a valid result.
        /// </summary>
        private static bool hiddenPipeDefsInitialized = false;

        /// <summary>
        /// Gets the list of supported hidden pipe ThingDefs for installed VE mods.
        /// Results are cached after first call for performance.
        /// </summary>
        /// <returns>Read-only list of hidden pipe ThingDefs (may be empty if no VE mods installed)</returns>
        public static IReadOnlyList<ThingDef> GetSupportedHiddenPipeDefs()
        {
            if (!hiddenPipeDefsInitialized)
            {
                cachedHiddenPipeDefs = BuildSupportedHiddenPipeDefsList();
                hiddenPipeDefsInitialized = true;
            }
            return cachedHiddenPipeDefs;
        }

        /// <summary>
        /// Builds the list of supported hidden pipe ThingDefs.
        /// Uses DefRefs/Things.cs defs which are resolved at startup via [DefOf].
        /// Null entries (mod not installed) are filtered out.
        /// </summary>
        private static List<ThingDef> BuildSupportedHiddenPipeDefsList()
        {
            List<ThingDef> hiddenPipes = new List<ThingDef>();

            // VE Chemfuel hidden pipes
            if (Things.VCHE_UndergroundChemfuelPipe != null)
                hiddenPipes.Add(Things.VCHE_UndergroundChemfuelPipe);

            // VE Nutrient Paste hidden pipes
            if (Things.VNPE_UndergroundNutrientPastePipe != null)
                hiddenPipes.Add(Things.VNPE_UndergroundNutrientPastePipe);

            // VE Gravships hidden pipes
            if (Things.VGE_HiddenOxygenPipe != null)
                hiddenPipes.Add(Things.VGE_HiddenOxygenPipe);
            if (Things.VGE_HiddenAstrofuelPipe != null)
                hiddenPipes.Add(Things.VGE_HiddenAstrofuelPipe);

            return hiddenPipes;
        }
    }
}
