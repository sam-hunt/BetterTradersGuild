using UnityEngine;
using Verse;

namespace BetterTradersGuild
{
    // "Defender Resupply" settings section — the last-resort comms-console food drop
    // for starving defenders. Gates on useCustomLayouts (the behavior
    // only runs in BTG-generated settlements); the two sliders additionally gate on
    // the enableResupply master toggle and sit indented beneath it.
    public partial class BetterTradersGuildSettings
    {
        // Master toggle for the defender comms-console food resupply behavior.
        // Default: true. When off, starving defenders never call in a resupply drop
        // (the meals/cooldown sliders retain their values, just grayed out).
        public bool enableResupply = true;

        // Survival-meal packs delivered per surviving humanlike defender, each time the
        // garrison calls in a comms-console resupply drop.
        // Range: 1-10. Default: 2 (drop size = 2 × living humanlike defenders). The
        // drop shrinks as the player neutralizes defenders, so no per-map cap is needed.
        public int resupplyMealsPerDefender = 2;

        // Cooldown between defender resupply drops on a single settlement map, in hours.
        // Range: 1-120 hours. Default: 12 hours.
        public int resupplyCooldownHours = 12;

        // When on, a successful resupply call also summons a drop-pod reinforcement raid of
        // the settlement's own troops (ResupplyRaidUtility). Default: true. Only fires while
        // the settlement faction is hostile, so it never triggers on a peaceful visit.
        public bool resupplyTriggersRaid = true;

        private void ExposeResupplySettings()
        {
            Scribe_Values.Look(ref enableResupply, "enableResupply", true);
            Scribe_Values.Look(ref resupplyMealsPerDefender, "resupplyMealsPerDefender", 2);
            Scribe_Values.Look(ref resupplyCooldownHours, "resupplyCooldownHours", 12);
            Scribe_Values.Look(ref resupplyTriggersRaid, "resupplyTriggersRaid", true);
        }

        private void ResetResupplySettings()
        {
            enableResupply = true;
            resupplyMealsPerDefender = 2;
            resupplyCooldownHours = 12;
            resupplyTriggersRaid = true;
        }

        private void DrawResupplySection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_Resupply".Translate());

            // Master toggle — editable whenever custom layouts are on.
            GUI.enabled = useCustomLayouts;
            listing.CheckboxLabeled("BTG_Settings_EnableResupply".Translate(), ref enableResupply,
                "BTG_Settings_EnableResupplyDesc".Translate());

            // The sliders and the reinforcement-raid toggle sit indented under the master
            // toggle and gray out with it.
            listing.Indent(12f);
            listing.ColumnWidth -= 12f;
            GUI.enabled = useCustomLayouts && enableResupply;

            listing.Gap(8f);

            // Meals per defender
            listing.Label("BTG_Settings_ResupplyMealsPerDefender".Translate(resupplyMealsPerDefender));
            float resupplyMealsSliderValue = listing.Slider(resupplyMealsPerDefender, 1f, 10f);
            resupplyMealsPerDefender = (int)System.Math.Round(resupplyMealsSliderValue);

            listing.Gap(2f);
            Description(listing, "BTG_Settings_ResupplyMealsPerDefenderDesc".Translate());

            listing.Gap(16f);

            // Cooldown (hours)
            listing.Label("BTG_Settings_ResupplyCooldown".Translate(resupplyCooldownHours));
            float resupplyCooldownSliderValue = listing.Slider(resupplyCooldownHours, 1f, 120f);
            resupplyCooldownHours = (int)System.Math.Round(resupplyCooldownSliderValue);

            listing.Gap(2f);
            Description(listing, "BTG_Settings_ResupplyCooldownDesc".Translate());

            listing.Gap(16f);

            // Reinforcement raid on a successful resupply call.
            listing.CheckboxLabeled("BTG_Settings_ResupplyTriggersRaid".Translate(), ref resupplyTriggersRaid,
                "BTG_Settings_ResupplyTriggersRaidDesc".Translate());
            listing.Gap(2f);
            Description(listing, "BTG_Settings_ResupplyTriggersRaidDesc".Translate());

            listing.ColumnWidth += 12f;
            listing.Outdent(12f);
            GUI.enabled = true;

            listing.Gap(24f);
        }
    }
}
