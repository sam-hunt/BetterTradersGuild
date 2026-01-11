using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// Custom MapPortal class for the cargo hold hatch.
    ///
    /// Unlike AncientHatch, this class does NOT add extra GenSteps to the pocket map.
    /// All generation is controlled entirely by the MapGeneratorDef (BTG_CargoHold)
    /// and its specified GenSteps.
    ///
    /// This prevents vanilla's AncientStockpile generation from interfering with
    /// our custom BTG_CargoHoldVault GenStep.
    /// </summary>
    public class BTG_CargoHoldHatch : MapPortal
    {
        /// <summary>
        /// Override GetExtraGenSteps to return empty - we don't want any extra steps.
        /// All pocket map generation is handled by BTG_CargoHoldVault GenStep
        /// specified in the MapGeneratorDef.
        /// </summary>
        protected override IEnumerable<GenStepWithParams> GetExtraGenSteps()
        {
            // Return nothing - all generation is handled by our MapGeneratorDef
            yield break;
        }
    }
}
