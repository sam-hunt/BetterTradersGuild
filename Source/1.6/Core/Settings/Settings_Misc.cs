using Verse;

namespace BetterTradersGuild
{
    // "Misc" settings section — global balance knobs that apply to any TradersGuild
    // settlement regardless of the map generator, so they are never gated on custom
    // layouts (the salvagers raid weight acts on TG faction maps however they were
    // generated; the life-support power output and the smuggler's den quest weight
    // are pushed onto their defs at startup and on settings close).
    public partial class BetterTradersGuildSettings
    {
        // Salvagers raid weight multiplier when on TradersGuild maps.
        // Range: 0.0-5.0. 1.0 = vanilla. Default: 3.0 (BTG Recommended).
        // When attacking TradersGuild settlements, Salvagers raids become more likely —
        // emergent gameplay where assaulting the guild attracts opportunistic pirates.
        public float salvagersRaidWeightMultiplier = 3.0f;

        // Random-selection weight of the smuggler's den quest, pushed onto
        // QuestScriptDef.rootSelectionWeight (BTG_SmugglersDen ships at 1.0). The
        // storyteller weights it against every other eligible quest, so this is a
        // relative frequency, not a chance. 0 removes the quest from the random pool
        // outright (vanilla treats a zero weight as "not root-random selectable").
        // Range: 0-3.0. Default/BTG Recommended: 1.0. Applied live on settings close
        // via WriteSettings (no restart needed).
        public float smugglersDenQuestWeight = 1.0f;

        // LifeSupportUnit power output in watts.
        // Range: 0-5000W. Default: 3200W (vanilla; the value v1.0.x actually shipped, and a
        // safe fallback for existing saves). BTG Recommended: 800W for a tighter grid —
        // BTG connects buildings into a map-wide grid where vanilla output is generous.
        // Applied live on save via WriteSettings (no restart needed).
        public int lifeSupportUnitPowerOutput = 3200;

        private void ExposeMiscSettings()
        {
            Scribe_Values.Look(ref salvagersRaidWeightMultiplier, "salvagersRaidWeightMultiplier", 3.0f);
            Scribe_Values.Look(ref smugglersDenQuestWeight, "smugglersDenQuestWeight", 1.0f);
            Scribe_Values.Look(ref lifeSupportUnitPowerOutput, "lifeSupportUnitPowerOutput", 3200);
        }

        private void ResetMiscSettings()
        {
            salvagersRaidWeightMultiplier = 3.0f;
            smugglersDenQuestWeight = 1.0f;
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

            // Smuggler's den quest weight. Pushed onto the QuestScriptDef by
            // BetterTradersGuildMod.ApplySmugglersDenQuestWeightSetting when the
            // settings window closes.
            string denQuestLabel = Annotate(
                "BTG_Settings_SmugglersDenQuestWeight".Translate(smugglersDenQuestWeight.ToString("F2")),
                recommended: smugglersDenQuestWeight == 1.0f);
            listing.Label(denQuestLabel);

            float denQuestSliderValue = listing.Slider(smugglersDenQuestWeight, 0f, 3f);
            smugglersDenQuestWeight = (float)(System.Math.Round(denQuestSliderValue / 0.25) * 0.25);

            listing.Gap(2f);
            Description(listing, "BTG_Settings_SmugglersDenQuestWeightDesc".Translate());

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
