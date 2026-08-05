using UnityEngine;
using Verse;

namespace BetterTradersGuild
{
    // "Map Generation" settings section. useCustomLayouts is the master
    // toggle for BTG's custom settlement generation; the Defenders and Defender
    // Resupply sections (and the cargo vault below) all gate on it. Drawn first so
    // the dependent sections that follow read as enabled/disabled beneath it.
    public partial class BetterTradersGuildSettings
    {
        // Enable custom settlement layouts for TradersGuild bases.
        // When enabled: uses BTG_OrbitalSettlement layout with custom room types.
        // When disabled: uses vanilla/other mod generation. Default: true.
        public bool useCustomLayouts = true;

        // Enable cargo vault access in TradersGuild settlements.
        // When enabled: cargo vault hatch spawns hackable. When disabled: spawns
        // sealed. Default: true. Only affects newly generated maps. Requires
        // useCustomLayouts.
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
