using UnityEngine;
using Verse;

namespace BetterTradersGuild
{
    // Defender resupply subgroup — the last-resort comms-console food drop for
    // starving defenders. Drawn inside the Garrison & Combat section
    // (Settings_Defenders.cs) as an indented subgroup under the entrenched-AI
    // toggle, which is its true prerequisite: the resupply job is a node of the
    // BTG_DefendStructure duty, so it runs wherever the entrenched lord runs —
    // custom-layout settlements and the smuggler's den alike — and never runs
    // under the vanilla lord. It deliberately does NOT gate on useCustomLayouts:
    // that toggle has no effect on the den, whose garrison these knobs still
    // fully govern.
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

        // Cooldown between defender resupply drops on a single map, in hours.
        // Range: 1-120 hours. Default: 12 hours.
        public int resupplyCooldownHours = 12;

        // When on, a successful resupply call also summons a drop-pod reinforcement raid of
        // the map faction's own troops (ResupplyRaidUtility). Default: true. Only fires while
        // the map faction is hostile, so it never triggers on a peaceful visit.
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

        // Indented subgroup rendered by DrawDefendersSection directly beneath the
        // entrenched-AI checkbox. Restores GUI.enabled to true before returning.
        private void DrawResupplySubgroup(Listing_Standard listing)
        {
            listing.Indent(12f);
            listing.ColumnWidth -= 12f;

            // Subgroup label + master toggle: editable whenever entrenched AI is on.
            GUI.enabled = useEntrenchedDefenders;
            listing.Label("BTG_Settings_Resupply".Translate());
            listing.Gap(4f);

            listing.CheckboxLabeled("BTG_Settings_EnableResupply".Translate(), ref enableResupply,
                "BTG_Settings_EnableResupplyDesc".Translate());

            // The sliders and the reinforcement-raid toggle additionally gray out
            // with the master toggle.
            GUI.enabled = useEntrenchedDefenders && enableResupply;

            listing.Gap(8f);

            // Meals per defender
            LabelWithTooltip(listing, "BTG_Settings_ResupplyMealsPerDefender".Translate(resupplyMealsPerDefender),
                "BTG_Settings_ResupplyMealsPerDefenderDesc".Translate());
            float resupplyMealsSliderValue = listing.Slider(resupplyMealsPerDefender, 1f, 10f);
            resupplyMealsPerDefender = (int)System.Math.Round(resupplyMealsSliderValue);

            listing.Gap(8f);

            // Cooldown (hours)
            LabelWithTooltip(listing, "BTG_Settings_ResupplyCooldown".Translate(resupplyCooldownHours),
                "BTG_Settings_ResupplyCooldownDesc".Translate());
            float resupplyCooldownSliderValue = listing.Slider(resupplyCooldownHours, 1f, 120f);
            resupplyCooldownHours = (int)System.Math.Round(resupplyCooldownSliderValue);

            listing.Gap(8f);

            // Reinforcement raid on a successful resupply call.
            listing.CheckboxLabeled("BTG_Settings_ResupplyTriggersRaid".Translate(), ref resupplyTriggersRaid,
                "BTG_Settings_ResupplyTriggersRaidDesc".Translate());

            listing.ColumnWidth += 12f;
            listing.Outdent(12f);
            GUI.enabled = true;
        }
    }
}
