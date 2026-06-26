using UnityEngine;
using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// "Defenders" settings section — strength and composition of the garrison that
    /// spawns when the player enters a settlement. All three knobs only affect
    /// BTG's custom generation, so the whole section gates on
    /// <see cref="useCustomLayouts"/> (greyed out, values preserved, when off).
    /// None of these affect subsequent raid incidents, only initial defenders.
    /// </summary>
    public partial class BetterTradersGuildSettings
    {
        /// <summary>
        /// Threat points multiplier for initial defender generation.
        /// </summary>
        /// <remarks>
        /// Range: 0.5-3.0. Default: 1.0 (no modification). Applied after the minimum
        /// threat points cap. Requires <see cref="useCustomLayouts"/>.
        /// </remarks>
        public float threatPointsMultiplier = 1.0f;

        /// <summary>
        /// Minimum threat points for initial defender generation.
        /// </summary>
        /// <remarks>
        /// Range: 0-5000. 0 = vanilla (no floor). Default: 0. BTG Recommended: 2400
        /// (ensures elite pawn types can spawn at low wealth). Requires
        /// <see cref="useCustomLayouts"/>.
        /// </remarks>
        public float minimumThreatPoints = 0f;

        /// <summary>
        /// Additional sentry drone presence as a factor of threat points.
        /// </summary>
        /// <remarks>
        /// Range: 0.0-2.0 (0-200% of threat points). 0 = vanilla. Default: 0.25.
        /// Requires <see cref="useCustomLayouts"/>.
        /// </remarks>
        public float sentryDronePresence = 0.25f;

        private void ExposeDefenderSettings()
        {
            Scribe_Values.Look(ref threatPointsMultiplier, "threatPointsMultiplier", 1.0f);
            Scribe_Values.Look(ref minimumThreatPoints, "minimumThreatPoints", 0f);
            Scribe_Values.Look(ref sentryDronePresence, "sentryDronePresence", 0.25f);
        }

        private void ResetDefenderSettings()
        {
            threatPointsMultiplier = 1.0f;
            minimumThreatPoints = 0f;
            sentryDronePresence = 0.25f;
        }

        private void DrawDefendersSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_Defenders".Translate());

            // Whole section depends on custom layouts.
            GUI.enabled = useCustomLayouts;

            // Threat points multiplier
            string multiplierLabel = Annotate(
                "BTG_Settings_ThreatMultiplier".Translate(threatPointsMultiplier.ToString("F1")),
                vanilla: threatPointsMultiplier == 1.0f);
            listing.Label(multiplierLabel);

            float multiplierSliderValue = listing.Slider(threatPointsMultiplier, 0.5f, 3.0f);
            threatPointsMultiplier = (float)(System.Math.Round(multiplierSliderValue / 0.25) * 0.25);

            listing.Gap(2f);
            Description(listing, "BTG_Settings_ThreatMultiplierDesc".Translate());

            listing.Gap(16f);

            // Minimum threat points
            int threatPointsDisplay = (int)minimumThreatPoints;
            string threatLabel = Annotate(
                "BTG_Settings_MinThreatPoints".Translate(threatPointsDisplay),
                vanilla: threatPointsDisplay == 0,
                recommended: threatPointsDisplay == 2400);
            listing.Label(threatLabel);

            float threatSliderValue = listing.Slider(minimumThreatPoints, 0f, 5000f);
            minimumThreatPoints = (int)(System.Math.Round(threatSliderValue / 100f) * 100f);

            listing.Gap(2f);
            Description(listing, "BTG_Settings_MinThreatPointsDesc".Translate());

            listing.Gap(16f);

            // Additional sentry drone presence
            int dronePercentageDisplay = (int)(sentryDronePresence * 100f);
            string droneLabel = Annotate(
                "BTG_Settings_SentryDronePresence".Translate(dronePercentageDisplay),
                vanilla: dronePercentageDisplay == 0,
                recommended: dronePercentageDisplay == 25);
            listing.Label(droneLabel);

            float droneSliderValue = listing.Slider(sentryDronePresence * 100f, 0f, 200f);
            sentryDronePresence = (int)(System.Math.Round(droneSliderValue / 5f) * 5f) / 100f;

            listing.Gap(2f);
            Description(listing, "BTG_Settings_SentryDroneDesc".Translate());

            GUI.enabled = true;
            listing.Gap(24f);
        }
    }
}
