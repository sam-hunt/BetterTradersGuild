using Verse;

namespace BetterTradersGuild
{
    // "Events & Quests" settings section — storyteller frequency dials. Currently
    // just the smuggler's den quest weight; future incident/quest knobs belong
    // here too.
    public partial class BetterTradersGuildSettings
    {
        // Random-selection weight of the smuggler's den quest, pushed onto
        // QuestScriptDef.rootSelectionWeight (BTG_SmugglersDen ships at 1.0). The
        // storyteller weights it against every other eligible quest, so this is a
        // relative frequency, not a chance. 0 removes the quest from the random pool
        // outright (vanilla treats a zero weight as "not root-random selectable").
        // Range: 0-3.0. Default/BTG Recommended: 1.0. Applied live on settings close
        // via WriteSettings (no restart needed).
        public float smugglersDenQuestWeight = 1.0f;

        private void ExposeEventsSettings()
        {
            Scribe_Values.Look(ref smugglersDenQuestWeight, "smugglersDenQuestWeight", 1.0f);
        }

        private void ResetEventsSettings()
        {
            smugglersDenQuestWeight = 1.0f;
        }

        private void DrawEventsSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_Events".Translate());

            // Smuggler's den quest weight. Pushed onto the QuestScriptDef by
            // BetterTradersGuildMod.ApplySmugglersDenQuestWeightSetting when the
            // settings window closes.
            string denQuestLabel = Annotate(
                "BTG_Settings_SmugglersDenQuestWeight".Translate(smugglersDenQuestWeight.ToString("F2")),
                recommended: smugglersDenQuestWeight == 1.0f);
            LabelWithTooltip(listing, denQuestLabel, "BTG_Settings_SmugglersDenQuestWeightDesc".Translate());

            float denQuestSliderValue = listing.Slider(smugglersDenQuestWeight, 0f, 3f);
            smugglersDenQuestWeight = (float)(System.Math.Round(denQuestSliderValue / 0.25) * 0.25);

            listing.Gap(24f);
        }
    }
}
