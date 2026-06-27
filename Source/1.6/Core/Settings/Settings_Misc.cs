using Verse;

namespace BetterTradersGuild
{
    // "Misc" settings section — global balance knobs that apply to any TradersGuild
    // settlement regardless of the map generator, so they are never gated on custom
    // layouts (the salvagers raid weight acts on TG faction maps however they were
    // generated; the life-support power output is pushed onto the ThingDef at
    // startup).
    public partial class BetterTradersGuildSettings
    {
        // Salvagers raid weight multiplier when on TradersGuild maps.
        // Range: 0.0-5.0. 1.0 = vanilla. Default: 3.0 (BTG Recommended).
        // When attacking TradersGuild settlements, Salvagers raids become more likely —
        // emergent gameplay where assaulting the guild attracts opportunistic pirates.
        public float salvagersRaidWeightMultiplier = 3.0f;

        // LifeSupportUnit power output in watts.
        // Range: 0-5000W. Default: 3200W (vanilla; the value v1.0.x actually shipped, and a
        // safe fallback for existing saves). BTG Recommended: 800W for a tighter grid —
        // BTG connects buildings into a map-wide grid where vanilla output is generous.
        // Applied live on save via WriteSettings (no restart needed).
        public int lifeSupportUnitPowerOutput = 3200;

        private void ExposeMiscSettings()
        {
            Scribe_Values.Look(ref salvagersRaidWeightMultiplier, "salvagersRaidWeightMultiplier", 3.0f);
            Scribe_Values.Look(ref lifeSupportUnitPowerOutput, "lifeSupportUnitPowerOutput", 3200);
        }

        private void ResetMiscSettings()
        {
            salvagersRaidWeightMultiplier = 3.0f;
            lifeSupportUnitPowerOutput = 3200;
        }

        private void DrawMiscSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_Misc".Translate());

            // Salvagers raid weight multiplier
            string salvagersLabel = Annotate(
                "BTG_Settings_SalvagersRaidWeight".Translate(salvagersRaidWeightMultiplier.ToString("F1")),
                vanilla: salvagersRaidWeightMultiplier == 1.0f,
                recommended: salvagersRaidWeightMultiplier == 3.0f);
            listing.Label(salvagersLabel);

            float salvagersSliderValue = listing.Slider(salvagersRaidWeightMultiplier, 0f, 5f);
            salvagersRaidWeightMultiplier = (float)(System.Math.Round(salvagersSliderValue / 0.5) * 0.5);

            listing.Gap(2f);
            Description(listing, "BTG_Settings_SalvagersRaidWeightDesc".Translate());

            listing.Gap(16f);

            // LifeSupportUnit power output
            string powerLabel = Annotate(
                "BTG_Settings_LifeSupportPower".Translate(lifeSupportUnitPowerOutput),
                vanilla: lifeSupportUnitPowerOutput == 3200,
                recommended: lifeSupportUnitPowerOutput == 800);
            listing.Label(powerLabel);

            float powerSliderValue = listing.Slider(lifeSupportUnitPowerOutput, 0f, 5000f);
            lifeSupportUnitPowerOutput = (int)(System.Math.Round(powerSliderValue / 100f) * 100f);

            listing.Gap(2f);
            Description(listing, "BTG_Settings_LifeSupportDesc".Translate());

            listing.Gap(24f);
        }
    }
}
