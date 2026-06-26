using UnityEngine;
using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// "Map Generation" settings section. <see cref="useCustomLayouts"/> is the master
    /// toggle for BTG's custom settlement generation; the Defenders and Defender
    /// Resupply sections (and the cargo vault below) all gate on it. Drawn first so
    /// the dependent sections that follow read as enabled/disabled beneath it.
    /// </summary>
    public partial class BetterTradersGuildSettings
    {
        /// <summary>
        /// Enable custom settlement layouts for TradersGuild bases.
        /// </summary>
        /// <remarks>
        /// When enabled: uses BTG_OrbitalSettlement layout with custom room types.
        /// When disabled: uses vanilla/other mod generation. Default: true.
        /// </remarks>
        public bool useCustomLayouts = true;

        /// <summary>
        /// Enable cargo vault access in TradersGuild settlements.
        /// </summary>
        /// <remarks>
        /// When enabled: cargo vault hatch spawns hackable. When disabled: spawns
        /// sealed. Default: true. Only affects newly generated maps. Requires
        /// <see cref="useCustomLayouts"/>.
        /// </remarks>
        public bool enableCargoVault = true;

        private void ExposeMapGenerationSettings()
        {
            Scribe_Values.Look(ref useCustomLayouts, "useCustomLayouts", true);
            Scribe_Values.Look(ref enableCargoVault, "enableCargoVault", true);
        }

        private void ResetMapGenerationSettings()
        {
            useCustomLayouts = true;
            enableCargoVault = true;
        }

        private void DrawMapGenerationSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_MapGeneration".Translate());

            // Master toggle — always editable.
            listing.CheckboxLabeled("BTG_Settings_UseCustomLayouts".Translate(), ref useCustomLayouts,
                "BTG_Settings_UseCustomLayoutsDesc".Translate());

            listing.Gap(12f);

            // Cargo vault only applies to custom-generated maps.
            GUI.enabled = useCustomLayouts;
            listing.CheckboxLabeled("BTG_Settings_EnableCargoVault".Translate(), ref enableCargoVault,
                "BTG_Settings_EnableCargoVaultDesc".Translate());
            GUI.enabled = true;

            listing.Gap(24f);
        }
    }
}
