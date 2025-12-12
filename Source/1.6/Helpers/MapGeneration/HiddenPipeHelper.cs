using System.Collections.Generic;
using System.Linq;
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
    /// We use explicit defNames rather than dynamic discovery because:
    /// 1. We only want pipes for networks where we provide tank+valve prefabs
    /// 2. Dynamic detection could spawn pipes for networks with no usable supply
    /// 3. Being explicit ensures pipe networks are always meaningful to players
    /// </summary>
    public static class HiddenPipeHelper
    {
        /// <summary>
        /// Cached list of hidden pipe ThingDefs from supported Vanilla Expanded mods.
        /// Empty list entries are filtered out at runtime if mod not installed.
        /// </summary>
        private static List<ThingDef> cachedHiddenPipeDefs = null;

        /// <summary>
        /// Flag indicating whether hidden pipe defs have been initialized.
        /// Separate from null check because an empty list is a valid result.
        /// </summary>
        private static bool hiddenPipeDefsInitialized = false;

        /// <summary>
        /// Explicit list of hidden pipe defNames we support.
        /// Each entry corresponds to a mod where we provide tank+valve prefabs.
        ///
        /// SUPPORTED NETWORKS:
        /// - VE Chemfuel: Chemfuel and Deepchem networks
        /// - VE Nutrient Paste: Nutrient paste network
        /// - VE Gravships: Oxygen and Astrofuel networks
        ///
        /// EXCLUDED:
        /// - VREA Neutroamine: No valve exists, so no prefab, so no hidden pipes
        /// - VRE Sanguophage Hemogen: Traders aren't sanguophages; hemogen pipes contain
        ///   gold which incentivizes immersion-breaking deconstruction
        /// </summary>
        private static readonly string[] SupportedHiddenPipeDefNames = new string[]
        {
            // VE Chemfuel (VanillaExpanded.VChemfuelE)
            "VCHE_UndergroundChemfuelPipe",
            "VCHE_UndergroundDeepchemPipe",
            // VE Nutrient Paste (VanillaExpanded.VNutrientE)
            "VNPE_UndergroundNutrientPastePipe",
            // VE Gravships (vanillaexpanded.gravship)
            "VGE_HiddenOxygenPipe",
            "VGE_HiddenAstrofuelPipe",
        };

        /// <summary>
        /// Gets the list of supported hidden pipe ThingDefs for installed VE mods.
        /// Results are cached after first call for performance.
        /// </summary>
        /// <returns>Read-only list of hidden pipe ThingDefs (may be empty if no VE mods installed)</returns>
        public static IReadOnlyList<ThingDef> GetSupportedHiddenPipeDefs()
        {
            if (!hiddenPipeDefsInitialized)
            {
                cachedHiddenPipeDefs = LoadSupportedHiddenPipeDefs();
                hiddenPipeDefsInitialized = true;

                if (cachedHiddenPipeDefs.Count > 0)
                {
                    Log.Message($"[Better Traders Guild] Loaded {cachedHiddenPipeDefs.Count} supported VE hidden pipe type(s): " +
                        string.Join(", ", cachedHiddenPipeDefs.Select(d => d.defName)));
                }
            }
            return cachedHiddenPipeDefs;
        }

        /// <summary>
        /// Loads ThingDefs for our explicitly supported hidden pipe types.
        /// Only includes defs that exist (mod is installed).
        /// </summary>
        /// <returns>List of hidden pipe ThingDefs for installed supported mods</returns>
        private static List<ThingDef> LoadSupportedHiddenPipeDefs()
        {
            List<ThingDef> hiddenPipes = new List<ThingDef>();

            foreach (string defName in SupportedHiddenPipeDefNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def != null)
                {
                    hiddenPipes.Add(def);
                }
            }

            return hiddenPipes;
        }
    }
}
