using Verse;

namespace BetterTradersGuild
{
    // "Trading" settings section — orbital trader rotation cadence. Applies
    // regardless of the map generator, so it is never gated on custom layouts.
    public partial class BetterTradersGuildSettings
    {
        // Trader rotation interval in days (how often orbital traders change at settlements).
        // Range: 5-60 days. Default: 30 days (same as vanilla).
        public int traderRotationIntervalDays = 30;

        private void ExposeTradingSettings()
        {
            Scribe_Values.Look(ref traderRotationIntervalDays, "traderRotationIntervalDays", 30);
        }

        private void ResetTradingSettings()
        {
            traderRotationIntervalDays = 30;
        }

        private void DrawTradingSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_Trading".Translate());

            string intervalLabel = Annotate(
                "BTG_Settings_TraderRotationInterval".Translate(traderRotationIntervalDays),
                vanilla: traderRotationIntervalDays == 30);
            listing.Label(intervalLabel);

            float sliderValue = listing.Slider(traderRotationIntervalDays, 5f, 60f);
            traderRotationIntervalDays = (int)(System.Math.Round(sliderValue / 5f) * 5f);

            listing.Gap(2f);
            Description(listing, "BTG_Settings_TraderRotationDesc1".Translate());
            Description(listing, "BTG_Settings_TraderRotationDesc2".Translate());

            listing.Gap(24f);
        }
    }
}
