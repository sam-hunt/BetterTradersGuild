using UnityEngine;
using Verse;

namespace BetterTradersGuild
{
    // "Garrison & Combat" settings section — how BTG garrisons fight, give up, and
    // sustain themselves. Everything here governs BTG-generated maps: custom-layout
    // settlements AND the smuggler's den quest site (whose generator ignores
    // useCustomLayouts entirely), so these controls are NOT greyed out under the
    // custom-layouts master toggle. The one exception is the initial-garrison
    // subgroup at the bottom (scaleDefendersToThreatLevel, threatPointsMultiplier):
    // those only feed the settlement points roll — the den's garrison is sized by
    // its quest — so they alone gate on useCustomLayouts.
    //
    // The defender resupply knobs render as an indented subgroup under the
    // entrenched-AI toggle, which is their true prerequisite: the resupply job is a
    // node of the BTG_DefendStructure duty, so it runs wherever the entrenched lord
    // runs — custom-layout settlements and the smuggler's den alike — and never
    // runs under the vanilla lord. The subgroup deliberately does NOT gate on
    // useCustomLayouts: that toggle has no effect on the den, whose garrison these
    // knobs still fully govern.
    public partial class BetterTradersGuildSettings
    {
        // Defender AI style ("Entrenched defender AI" in the settings UI). When
        // true (default), defenders use BTG's bounded lord
        // (LordJob_BTGDefendStructure): they hold the structure, never assault or
        // chase intruders into vacuum, and forage/rest/tend/resupply in-bounds.
        // When false, they revert to vanilla LordJob_DefendBase (defends the base,
        // then assaults). Decided at map generation; changing it only affects maps
        // entered afterwards. Governs settlement garrisons (which additionally need
        // useCustomLayouts for the BTG pawn GenStep to run at all) and the
        // smuggler's den garrison (always).
        public bool useEntrenchedDefenders = true;

        // Survival-meal packs delivered per surviving humanlike defender, each time the
        // garrison calls in a comms-console resupply drop (the last-resort hunger
        // escalation for starving entrenched defenders). Doubles as the feature's
        // master switch: 0 = defenders never call in a resupply drop.
        // Range: 0-10. Default: 2 (drop size = 2 × living humanlike defenders). The
        // drop shrinks as the player neutralizes defenders, so no per-map cap is
        // needed; drops are also debounced by a fixed per-map cooldown
        // (ResupplyDropTracker).
        public int resupplyMealsPerDefender = 2;

        // When on, a successful resupply call also summons a drop-pod reinforcement raid of
        // the map faction's own troops (ResupplyRaidUtility). Default: true. Only fires while
        // the map faction is hostile, so it never triggers on a peaceful visit.
        public bool resupplyTriggersRaid = true;

        // Scale initial defender generation to the world's current threat points
        // (colony wealth + difficulty) instead of vanilla's flat 1150-1600 roll.
        // The flat roll is intentional vanilla design for all settlements, so this
        // ships default OFF to avoid surprising existing players; the world value
        // is a floor-raise only (never below the vanilla roll). Settlements only:
        // the den's points always come from its quest, so this never applies there.
        // Requires useCustomLayouts.
        public bool scaleDefendersToThreatLevel = false;

        // Threat points multiplier for initial defender generation. Applied to the
        // base points (flat vanilla roll, or threat-scaled when
        // scaleDefendersToThreatLevel is on). Range: 0.5-3.0. Default: 1.0 (no
        // modification). Settlements only, same as scaleDefendersToThreatLevel.
        // Requires useCustomLayouts.
        public float threatPointsMultiplier = 1.0f;

        // Additional sentry drone presence as a factor of threat points.
        // Range: 0.0-2.0 (0-200% of threat points). 0 = vanilla. Default: 0.25.
        // Applies to settlements and the smuggler's den (see the deliberate
        // no-useCustomLayouts-check note on GenStep_SpawnSentryDrones).
        public float sentryDronePresence = 0.25f;

        // Fraction of a BTG map's original active security (garrison humans of
        // either defender AI style + roaming sentry drones) that must be
        // incapacitated before the map counts as defeated. Read live by
        // SecurityDefeatUtility, so changing it takes effect on maps already in
        // progress. Only governs BTG-generated maps; vanilla-generated TG
        // settlements (useCustomLayouts off) keep vanilla's defeat check.
        // Range: 0.5-1.0. Default/BTG Recommended: 0.8. 1.0 = every last defender.
        // No vanilla value exists: space maps never get vanilla's rout toil, so this
        // threshold is BTG's replacement for it.
        public float securityDefeatFraction = 0.8f;

        private void ExposeDefenderSettings()
        {
            Scribe_Values.Look(ref useEntrenchedDefenders, "useEntrenchedDefenders", true);
            Scribe_Values.Look(ref resupplyMealsPerDefender, "resupplyMealsPerDefender", 2);
            Scribe_Values.Look(ref resupplyTriggersRaid, "resupplyTriggersRaid", true);
            Scribe_Values.Look(ref scaleDefendersToThreatLevel, "scaleDefendersToThreatLevel", false);
            Scribe_Values.Look(ref threatPointsMultiplier, "threatPointsMultiplier", 1.0f);
            Scribe_Values.Look(ref sentryDronePresence, "sentryDronePresence", 0.25f);
            Scribe_Values.Look(ref securityDefeatFraction, "securityDefeatFraction", 0.8f);

            // Legacy migration: enableResupply was rolled into resupplyMealsPerDefender
            // (0 = disabled). Carry an old "off" over so those players keep resupply
            // disabled; the stale node is simply never written back.
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                bool legacyEnableResupply = true;
                Scribe_Values.Look(ref legacyEnableResupply, "enableResupply", true);
                if (!legacyEnableResupply)
                    resupplyMealsPerDefender = 0;
            }
        }

        private void ResetDefenderSettings()
        {
            useEntrenchedDefenders = true;
            resupplyMealsPerDefender = 2;
            resupplyTriggersRaid = true;
            scaleDefendersToThreatLevel = false;
            threatPointsMultiplier = 1.0f;
            sentryDronePresence = 0.25f;
            securityDefeatFraction = 0.8f;
        }

        private void DrawDefendersSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_Defenders".Translate());

            // Defender AI style: BTG's bounded entrenched lord vs vanilla
            // DefendBase. The headline choice for the section. Not gated on
            // useCustomLayouts: it also decides the smuggler's den garrison's lord.
            string defenderAiLabel = Annotate(
                "BTG_Settings_EntrenchedDefenders".Translate(),
                vanilla: !useEntrenchedDefenders);
            listing.CheckboxLabeled(defenderAiLabel, ref useEntrenchedDefenders,
                "BTG_Settings_EntrenchedDefendersDesc".Translate());

            // Resupply subgroup, indented beneath its prerequisite.
            listing.Gap(8f);
            DrawResupplySubgroup(listing);

            listing.Gap(16f);

            // Garrison defeat threshold. Governs settlements and the den alike.
            int defeatPercentageDisplay = (int)(securityDefeatFraction * 100f);
            string defeatLabel = Annotate(
                "BTG_Settings_SecurityDefeatThreshold".Translate(defeatPercentageDisplay),
                recommended: defeatPercentageDisplay == 80);
            LabelWithTooltip(listing, defeatLabel, "BTG_Settings_SecurityDefeatThresholdDesc".Translate());

            float defeatSliderValue = listing.Slider(securityDefeatFraction * 100f, 50f, 100f);
            securityDefeatFraction = (int)(System.Math.Round(defeatSliderValue / 5f) * 5f) / 100f;

            listing.Gap(12f);

            // Additional sentry drone presence. Settlements and den alike.
            int dronePercentageDisplay = (int)(sentryDronePresence * 100f);
            string droneLabel = Annotate(
                "BTG_Settings_SentryDronePresence".Translate(dronePercentageDisplay),
                vanilla: dronePercentageDisplay == 0,
                recommended: dronePercentageDisplay == 25);
            LabelWithTooltip(listing, droneLabel, "BTG_Settings_SentryDroneDesc".Translate());

            float droneSliderValue = listing.Slider(sentryDronePresence * 100f, 0f, 200f);
            sentryDronePresence = (int)(System.Math.Round(droneSliderValue / 5f) * 5f) / 100f;

            listing.Gap(12f);

            // Initial settlement garrison subgroup: the only knobs here that are
            // truly settlement-only (the den's garrison is sized by its quest), so
            // they alone grey out with the custom-layouts master toggle.
            listing.Label("BTG_Settings_InitialGarrison".Translate());
            listing.Gap(4f);

            GUI.enabled = useCustomLayouts;
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;

            // While gated off the effective state is "no scaling" (= vanilla), so
            // the annotation follows the shown state rather than the stored one.
            string scaleLabel = Annotate(
                "BTG_Settings_ScaleDefenders".Translate(),
                vanilla: !(useCustomLayouts && scaleDefendersToThreatLevel));
            CheckboxLabeledGated(listing, scaleLabel, ref scaleDefendersToThreatLevel,
                "BTG_Settings_ScaleDefendersDesc".Translate(), useCustomLayouts);

            listing.Gap(8f);

            string multiplierLabel = Annotate(
                "BTG_Settings_ThreatMultiplier".Translate(threatPointsMultiplier.ToString("F1")),
                vanilla: threatPointsMultiplier == 1.0f);
            LabelWithTooltip(listing, multiplierLabel, "BTG_Settings_ThreatMultiplierDesc".Translate());

            // Discard the slider result while gated off: greyed sliders still take
            // drags (the fade is visual only), and the stored value must survive.
            float multiplierSliderValue = listing.Slider(threatPointsMultiplier, 0.5f, 3.0f);
            if (useCustomLayouts)
                threatPointsMultiplier = (float)(System.Math.Round(multiplierSliderValue / 0.25) * 0.25);

            listing.ColumnWidth += 16f;
            listing.Outdent(16f);
            GUI.enabled = true;

            listing.Gap(24f);
        }

        // Indented subgroup rendered by DrawDefendersSection directly beneath the
        // entrenched-AI checkbox. Restores GUI.enabled to true before returning.
        private void DrawResupplySubgroup(Listing_Standard listing)
        {
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;

            // The whole subgroup is dead weight without the entrenched lord.
            GUI.enabled = useEntrenchedDefenders;
            listing.Label("BTG_Settings_Resupply".Translate());
            listing.Gap(4f);

            // Meals per defender. Doubles as the master switch: 0 = resupply off.
            LabelWithTooltip(listing, "BTG_Settings_ResupplyMealsPerDefender".Translate(resupplyMealsPerDefender),
                "BTG_Settings_ResupplyMealsPerDefenderDesc".Translate());
            float resupplyMealsSliderValue = listing.Slider(resupplyMealsPerDefender, 0f, 10f);
            if (useEntrenchedDefenders)
                resupplyMealsPerDefender = (int)System.Math.Round(resupplyMealsSliderValue);

            listing.Gap(4f);

            // Reinforcement raid on a successful resupply call. Meaningless while
            // resupply itself is off, so it additionally gates on the meals switch.
            CheckboxLabeledGated(listing, "BTG_Settings_ResupplyTriggersRaid".Translate(), ref resupplyTriggersRaid,
                "BTG_Settings_ResupplyTriggersRaidDesc".Translate(),
                useEntrenchedDefenders && resupplyMealsPerDefender > 0);

            listing.ColumnWidth += 16f;
            listing.Outdent(16f);
            GUI.enabled = true;
        }
    }
}
