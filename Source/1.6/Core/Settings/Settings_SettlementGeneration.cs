using Verse;

namespace BetterTradersGuild
{
    // "Settlement Generation" settings section. useCustomLayouts is the master
    // toggle for BTG's custom SETTLEMENT generation only: the smuggler's den quest
    // site hard-wires its own map generator on its WorldObjectDef and never
    // consults it. Controls elsewhere that gate on useCustomLayouts do so because
    // they genuinely have no effect without BTG-generated settlement maps, not
    // because this section owns them.
    //
    // The life-support wattage lives here because it is an orbital-platform
    // infrastructure knob, but note its scope: it is pushed onto the vanilla
    // LifeSupportUnit def at startup and on settings close, so it affects every
    // orbital platform on the planet (ancient platforms included), not only
    // Traders Guild maps.
    public partial class BetterTradersGuildSettings
    {
        // Enable custom settlement layouts for TradersGuild bases.
        // When enabled: uses BTG_OrbitalSettlement layout with custom room types.
        // When disabled: uses vanilla/other mod generation. Default: true.
        // Settlement-only: the smuggler's den always generates with its own layout.
        public bool useCustomLayouts = true;

        // LifeSupportUnit power output in watts.
        // Range: 0-5000W. Default: 3200W (vanilla; the value v1.0.x actually shipped, and a
        // safe fallback for existing saves). Suggested: 800W for a tighter grid —
        // BTG connects buildings into a map-wide grid where vanilla output is generous.
        // Applied live on save via WriteSettings (no restart needed). Edits the shared
        // vanilla def, so it applies to all orbital platforms game-wide.
        public int lifeSupportUnitPowerOutput = 3200;

        private void ExposeSettlementGenerationSettings()
        {
            Scribe_Values.Look(ref useCustomLayouts, "useCustomLayouts", true);
            Scribe_Values.Look(ref lifeSupportUnitPowerOutput, "lifeSupportUnitPowerOutput", 3200);
        }

        private void ResetSettlementGenerationSettings()
        {
            useCustomLayouts = true;
            lifeSupportUnitPowerOutput = 3200;
        }

        private void DrawSettlementGenerationSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_MapGeneration".Translate());

            // Master toggle — always editable.
            string layoutsLabel = Annotate(
                "BTG_Settings_UseCustomLayouts".Translate(),
                isDefault: useCustomLayouts);
            listing.CheckboxLabeled(layoutsLabel, ref useCustomLayouts,
                "BTG_Settings_UseCustomLayoutsDesc".Translate());

            listing.Gap(12f);

            // LifeSupportUnit power output
            string powerLabel = Annotate(
                "BTG_Settings_LifeSupportPower".Translate(lifeSupportUnitPowerOutput),
                vanilla: lifeSupportUnitPowerOutput == 3200,
                recommended: lifeSupportUnitPowerOutput == 800,
                isDefault: lifeSupportUnitPowerOutput == 3200);
            LabelWithTooltip(listing, powerLabel, "BTG_Settings_LifeSupportDesc".Translate());

            float powerSliderValue = listing.Slider(lifeSupportUnitPowerOutput, 0f, 5000f);
            lifeSupportUnitPowerOutput = (int)(System.Math.Round(powerSliderValue / 100f) * 100f);

            listing.Gap(24f);
        }
    }
}
