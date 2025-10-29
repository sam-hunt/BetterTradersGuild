using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Mod settings and configuration
    /// </summary>
    public class BetterTradersGuildSettings : ModSettings
    {
        /// <summary>
        /// Trader rotation interval in days (how often orbital traders change at settlements)
        /// </summary>
        /// <remarks>
        /// Range: 5-30 days
        /// Default: 15 days
        /// Vanilla equivalent: 30 days
        /// </remarks>
        public int traderRotationIntervalDays = 15;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref traderRotationIntervalDays, "traderRotationIntervalDays", 15);
        }
    }

    /// <summary>
    /// Mod class for handling settings UI
    /// </summary>
    public class BetterTradersGuildMod_ModClass : Mod
    {
        public BetterTradersGuildSettings settings;

        public BetterTradersGuildMod_ModClass(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<BetterTradersGuildSettings>();
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Header
            listingStandard.Label("Orbital Trader Rotation");
            listingStandard.Gap(12f);

            // Trader rotation interval slider
            string intervalLabel = $"Trader rotation interval: {settings.traderRotationIntervalDays} days";

            // Add labels for special values
            if (settings.traderRotationIntervalDays == 15)
            {
                intervalLabel += " (Default)";
            }
            else if (settings.traderRotationIntervalDays == 30)
            {
                intervalLabel += " (Vanilla)";
            }

            listingStandard.Label(intervalLabel);

            // Slider with range 5-30, step 5
            // LEARNING NOTE: IntRange creates a slider that snaps to specific values
            // We manually round to nearest 5 to achieve 5-day increments
            float sliderValue = listingStandard.Slider(settings.traderRotationIntervalDays, 5f, 30f);
            settings.traderRotationIntervalDays = (int)(System.Math.Round(sliderValue / 5f) * 5f);

            listingStandard.Gap(6f);

            // Description text
            // LEARNING NOTE: Use Text.Font to set font size before calling Label
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Tiny;
            listingStandard.Label("How often orbital traders rotate at TradersGuild settlements.");
            listingStandard.Label("Lower values = more variety, but less time to reach distant settlements.");
            Text.Font = previousFont;

            listingStandard.Gap(12f);

            listingStandard.End();
        }

        public override string SettingsCategory()
        {
            return "Better Traders Guild";
        }
    }
}
