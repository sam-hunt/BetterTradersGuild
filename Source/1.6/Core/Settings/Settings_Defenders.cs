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
    // The resupply fields and their draw helper live in Settings_Resupply.cs; they
    // render here as an indented subgroup under the entrenched-AI toggle, which is
    // their true prerequisite (the resupply job only exists on the entrenched lord).
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
        // scaleDefendersToThreatLevel is on), before the minimum threat points
        // floor. Range: 0.5-3.0. Default: 1.0 (no modification). Settlements only,
        // same as scaleDefendersToThreatLevel. Requires useCustomLayouts.
        public float threatPointsMultiplier = 1.0f;

        // Minimum threat points for initial defender generation. Two consumers with
        // different scopes: floors the settlement garrison points roll (settlements
        // only — den pawn budget comes from its quest), and floors the threat level
        // GenStep_SpawnSentryDrones sizes the drone patrol from (settlements AND
        // den). Because of the drone floor it stays live regardless of
        // useCustomLayouts. Range: 0-5000. 0 = vanilla (no floor). Default: 0.
        // BTG Recommended: 2400 (ensures elite pawn types can spawn at low wealth).
        public float minimumThreatPoints = 0f;

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
            Scribe_Values.Look(ref scaleDefendersToThreatLevel, "scaleDefendersToThreatLevel", false);
            Scribe_Values.Look(ref threatPointsMultiplier, "threatPointsMultiplier", 1.0f);
            Scribe_Values.Look(ref minimumThreatPoints, "minimumThreatPoints", 0f);
            Scribe_Values.Look(ref sentryDronePresence, "sentryDronePresence", 0.25f);
            Scribe_Values.Look(ref securityDefeatFraction, "securityDefeatFraction", 0.8f);
        }

        private void ResetDefenderSettings()
        {
            useEntrenchedDefenders = true;
            scaleDefendersToThreatLevel = false;
            threatPointsMultiplier = 1.0f;
            minimumThreatPoints = 0f;
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

            // Resupply subgroup, indented beneath its prerequisite. Fields and draw
            // helper live in Settings_Resupply.cs.
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

            listing.Gap(16f);

            // Additional sentry drone presence. Settlements and den alike.
            int dronePercentageDisplay = (int)(sentryDronePresence * 100f);
            string droneLabel = Annotate(
                "BTG_Settings_SentryDronePresence".Translate(dronePercentageDisplay),
                vanilla: dronePercentageDisplay == 0,
                recommended: dronePercentageDisplay == 25);
            LabelWithTooltip(listing, droneLabel, "BTG_Settings_SentryDroneDesc".Translate());

            float droneSliderValue = listing.Slider(sentryDronePresence * 100f, 0f, 200f);
            sentryDronePresence = (int)(System.Math.Round(droneSliderValue / 5f) * 5f) / 100f;

            listing.Gap(16f);

            // Minimum threat points. Stays live regardless of useCustomLayouts
            // because it also floors the den's sentry drone patrol.
            int threatPointsDisplay = (int)minimumThreatPoints;
            string threatLabel = Annotate(
                "BTG_Settings_MinThreatPoints".Translate(threatPointsDisplay),
                vanilla: threatPointsDisplay == 0,
                recommended: threatPointsDisplay == 2400);
            LabelWithTooltip(listing, threatLabel, "BTG_Settings_MinThreatPointsDesc".Translate());

            float threatSliderValue = listing.Slider(minimumThreatPoints, 0f, 5000f);
            minimumThreatPoints = (int)(System.Math.Round(threatSliderValue / 100f) * 100f);

            listing.Gap(16f);

            // Initial settlement garrison subgroup: the only knobs here that are
            // truly settlement-only (the den's garrison is sized by its quest), so
            // they alone grey out with the custom-layouts master toggle.
            listing.Label("BTG_Settings_InitialGarrison".Translate());
            listing.Gap(4f);

            GUI.enabled = useCustomLayouts;
            listing.Indent(12f);
            listing.ColumnWidth -= 12f;

            string scaleLabel = Annotate(
                "BTG_Settings_ScaleDefenders".Translate(),
                vanilla: !scaleDefendersToThreatLevel);
            listing.CheckboxLabeled(scaleLabel, ref scaleDefendersToThreatLevel,
                "BTG_Settings_ScaleDefendersDesc".Translate());

            listing.Gap(8f);

            string multiplierLabel = Annotate(
                "BTG_Settings_ThreatMultiplier".Translate(threatPointsMultiplier.ToString("F1")),
                vanilla: threatPointsMultiplier == 1.0f);
            LabelWithTooltip(listing, multiplierLabel, "BTG_Settings_ThreatMultiplierDesc".Translate());

            float multiplierSliderValue = listing.Slider(threatPointsMultiplier, 0.5f, 3.0f);
            threatPointsMultiplier = (float)(System.Math.Round(multiplierSliderValue / 0.25) * 0.25);

            listing.ColumnWidth += 12f;
            listing.Outdent(12f);
            GUI.enabled = true;

            listing.Gap(24f);
        }
    }
}
