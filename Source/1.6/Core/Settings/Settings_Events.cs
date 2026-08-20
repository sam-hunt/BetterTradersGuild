using Verse;

namespace BetterTradersGuild
{
    // "Events & Quests" settings section — storyteller frequency dials for BTG's
    // quests. Future incident/quest knobs belong here too.
    public partial class BetterTradersGuildSettings
    {
        // Random-selection weight of the shuttle trade request quest, pushed onto
        // QuestScriptDef.rootSelectionWeight (BTG_TradeRequest ships at 1.0). The
        // storyteller weights it against every other eligible quest, so this is a
        // relative frequency, not a chance. 0 removes the quest from the random pool
        // outright (vanilla treats a zero weight as "not root-random selectable").
        // Range: 0-3.0. Default/Suggested: 1.0. Applied live on settings close
        // via WriteSettings (no restart needed).
        public float tradeRequestQuestWeight = 1.0f;

        // Random-selection weight of the smuggler's den quest, pushed onto
        // QuestScriptDef.rootSelectionWeight (BTG_SmugglersDen ships at 0.8). Same
        // semantics as tradeRequestQuestWeight above.
        // Range: 0-3.0. Default/Suggested: 0.8 — a touch below the trade request,
        // so the combat quest surfaces slightly less often than the peaceful one.
        // Applied live on settings close via WriteSettings (no restart needed).
        public float smugglersDenQuestWeight = 0.8f;

        private void ExposeEventsSettings()
        {
            Scribe_Values.Look(ref tradeRequestQuestWeight, "tradeRequestQuestWeight", 1.0f);
            Scribe_Values.Look(ref smugglersDenQuestWeight, "smugglersDenQuestWeight", 0.8f);
        }

        private void ResetEventsSettings()
        {
            tradeRequestQuestWeight = 1.0f;
            smugglersDenQuestWeight = 0.8f;
        }

        private void DrawEventsSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_Events".Translate());

            // Both weights are pushed onto their QuestScriptDefs by
            // BetterTradersGuildMod.ApplyQuestWeightSettings when the settings
            // window closes. Trade request (peaceful) first, den (combat) second,
            // matching the trade-then-combat section order of the window itself.
            string tradeQuestLabel = Annotate(
                "BTG_Settings_TradeRequestQuestWeight".Translate(tradeRequestQuestWeight.ToString("F2")),
                recommended: tradeRequestQuestWeight == 1.0f,
                isDefault: tradeRequestQuestWeight == 1.0f);
            LabelWithTooltip(listing, tradeQuestLabel, "BTG_Settings_TradeRequestQuestWeightDesc".Translate());

            float tradeQuestSliderValue = listing.Slider(tradeRequestQuestWeight, 0f, 3f);
            tradeRequestQuestWeight = (float)(System.Math.Round(tradeQuestSliderValue / 0.1) * 0.1);

            listing.Gap(12f);

            string denQuestLabel = Annotate(
                "BTG_Settings_SmugglersDenQuestWeight".Translate(smugglersDenQuestWeight.ToString("F2")),
                recommended: smugglersDenQuestWeight == 0.8f,
                isDefault: smugglersDenQuestWeight == 0.8f);
            LabelWithTooltip(listing, denQuestLabel, "BTG_Settings_SmugglersDenQuestWeightDesc".Translate());

            float denQuestSliderValue = listing.Slider(smugglersDenQuestWeight, 0f, 3f);
            smugglersDenQuestWeight = (float)(System.Math.Round(denQuestSliderValue / 0.1) * 0.1);

            listing.Gap(24f);
        }
    }
}
