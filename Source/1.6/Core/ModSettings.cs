using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Mod settings and configuration
    /// </summary>
    public class BetterTradersGuildSettings : ModSettings
    {
        // ===== PHASE 2: CORE FEATURES (ALWAYS ENABLED) =====

        /// <summary>
        /// Trader rotation interval in days (how often orbital traders change at settlements)
        /// </summary>
        /// <remarks>
        /// Range: 5-30 days
        /// Default: 15 days
        /// Vanilla equivalent: 30 days
        /// </remarks>
        public int traderRotationIntervalDays = 15;

        // ===== PHASE 3: OPTIONAL MAP GENERATION FEATURES =====

        /// <summary>
        /// Enable custom settlement layouts for TradersGuild bases
        /// </summary>
        /// <remarks>
        /// When enabled: Uses BTG_OrbitalSettlement layout with 18 custom room types
        /// When disabled: Uses vanilla/other mod generation
        /// Default: true
        /// </remarks>
        public bool useCustomLayouts = true;

        /// <summary>
        /// Enable cargo vault access in TradersGuild settlements
        /// </summary>
        /// <remarks>
        /// When enabled: Cargo vault hatch spawns hackable (can be accessed)
        /// When disabled: Cargo vault hatch spawns sealed (permanently inaccessible)
        /// Default: true
        /// Only affects newly generated maps
        /// Requires useCustomLayouts to be enabled
        /// </remarks>
        public bool enableCargoVault = true;

        // ===== PHASE 3: SENTRY DRONE SYSTEM =====

        /// <summary>
        /// Sentry drone presence as a factor of threat points
        /// </summary>
        /// <remarks>
        /// Range: 0.0-2.0 (0-200% of threat points)
        /// 0 = disabled (no sentry drones spawn)
        /// Default: 0.35 (35% of threat points used for drone calculation)
        /// Uses minimum threat points cap from PawnGroupMakerUtilityMinimumPoints
        /// Requires useCustomLayouts to be enabled
        /// </remarks>
        public float sentryDronePresence = 0.35f;

        /// <summary>
        /// Threat points multiplier for TradersGuild settlement pawn generation
        /// </summary>
        /// <remarks>
        /// Range: 0.5-3.0
        /// Default: 1.0 (no modification)
        /// Applied after minimum threat points cap
        /// Requires useCustomLayouts to be enabled
        /// </remarks>
        public float threatPointsMultiplier = 1.0f;

        /// <summary>
        /// Minimum threat points for TradersGuild settlement pawn generation
        /// </summary>
        /// <remarks>
        /// Range: 0-5000
        /// 0 = disabled (uses vanilla wealth-based calculation)
        /// Default: 2400 (ensures elite pawn types can spawn)
        /// Requires useCustomLayouts to be enabled
        /// </remarks>
        public float minimumThreatPoints = 2400f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref traderRotationIntervalDays, "traderRotationIntervalDays", 15);
            Scribe_Values.Look(ref useCustomLayouts, "useCustomLayouts", true);
            Scribe_Values.Look(ref enableCargoVault, "enableCargoVault", true);
            Scribe_Values.Look(ref sentryDronePresence, "sentryDronePresence", 0.3f);
            Scribe_Values.Look(ref threatPointsMultiplier, "threatPointsMultiplier", 1.0f);
            Scribe_Values.Look(ref minimumThreatPoints, "minimumThreatPoints", 2400f);
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

            // ========== SECTION: CORE FEATURES ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("Core Features");
            Text.Font = GameFont.Small;
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
            float sliderValue = listingStandard.Slider(settings.traderRotationIntervalDays, 5f, 30f);
            settings.traderRotationIntervalDays = (int)(System.Math.Round(sliderValue / 5f) * 5f);

            listingStandard.Gap(6f);

            // Description text
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Tiny;
            listingStandard.Label("How often orbital traders rotate at TradersGuild settlements.");
            listingStandard.Label("Lower values = more variety, but less time to reach distant settlements.");
            Text.Font = previousFont;

            listingStandard.Gap(24f);

            // ========== SECTION: MAP GENERATION ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("Map Generation");
            Text.Font = GameFont.Small;
            listingStandard.Gap(12f);

            // Custom layouts checkbox
            listingStandard.CheckboxLabeled("Use custom settlement map generator", ref settings.useCustomLayouts,
                "Generate TradersGuild settlements with less abandoned-looking aesthetics.\n" +
                "Disable if using other map generation mods or prefer vanilla layouts.");
            listingStandard.Gap(24f);

            // ========== SECTION: CARGO VAULT ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("Cargo Vault");
            Text.Font = GameFont.Small;
            listingStandard.Gap(12f);

            // Cargo vault checkbox (grayed out if layouts disabled)
            UnityEngine.GUI.enabled = settings.useCustomLayouts;

            listingStandard.CheckboxLabeled("Enable cargo vault access", ref settings.enableCargoVault,
                "When enabled, cargo vault hatches in shuttle bays can be hacked to access trade inventory.\n" +
                "When disabled, cargo vaults spawn sealed and inaccessible.\n" +
                "Only affects newly generated settlements.");

            listingStandard.Gap(24f);

            // ========== SECTION: SENTRY DRONES ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("Sentry Drones");
            Text.Font = GameFont.Small;
            listingStandard.Gap(12f);

            // Sentry drone presence slider (grayed out if layouts disabled)
            int dronePercentageDisplay = (int)(settings.sentryDronePresence * 100f);
            string droneLabel = $"Sentry drone presence: {dronePercentageDisplay}%";

            if (dronePercentageDisplay == 0)
            {
                droneLabel += " (Disabled)";
            }
            else if (dronePercentageDisplay == 35)
            {
                droneLabel += " (Default)";
            }

            listingStandard.Label(droneLabel);

            // Slider with range 0-200%, step 5%
            float droneSliderValue = listingStandard.Slider(settings.sentryDronePresence * 100f, 0f, 200f);
            settings.sentryDronePresence = (int)(System.Math.Round(droneSliderValue / 5f) * 5f) / 100f;

            listingStandard.Gap(6f);

            // Description text
            Text.Font = GameFont.Tiny;
            listingStandard.Label("Factor of threat points used for patrolling sentry drone spawning.");
            Text.Font = previousFont;

            listingStandard.Gap(24f);

            // ========== SECTION: COMBAT DIFFICULTY ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("Combat Difficulty");
            Text.Font = GameFont.Small;
            listingStandard.Gap(12f);

            // Threat points multiplier slider (grayed out if layouts disabled)
            string multiplierLabel = $"Threat points multiplier: {settings.threatPointsMultiplier:F1}x";

            if (settings.threatPointsMultiplier == 1.0f)
            {
                multiplierLabel += " (Default)";
            }

            listingStandard.Label(multiplierLabel);

            // Slider with range 0.5-3.0, step 0.25
            float multiplierSliderValue = listingStandard.Slider(settings.threatPointsMultiplier, 0.5f, 3.0f);
            settings.threatPointsMultiplier = (float)(System.Math.Round(multiplierSliderValue / 0.25) * 0.25);

            listingStandard.Gap(6f);

            // Description text
            Text.Font = GameFont.Tiny;
            listingStandard.Label("Multiplier applied to threat points after minimum cap.");
            listingStandard.Label("Higher values = more/stronger defenders. Requires custom layouts.");
            Text.Font = previousFont;

            listingStandard.Gap(12f);

            // Minimum threat points slider (grayed out if layouts disabled)
            int threatPointsDisplay = (int)settings.minimumThreatPoints;
            string threatLabel = $"Minimum threat points: {threatPointsDisplay}";

            if (threatPointsDisplay == 0)
            {
                threatLabel += " (Disabled)";
            }
            else if (threatPointsDisplay == 2400)
            {
                threatLabel += " (Default)";
            }

            listingStandard.Label(threatLabel);

            // Slider with range 0-5000, step 100
            float threatSliderValue = listingStandard.Slider(settings.minimumThreatPoints, 0f, 5000f);
            settings.minimumThreatPoints = (int)(System.Math.Round(threatSliderValue / 100f) * 100f);

            listingStandard.Gap(6f);

            // Description text
            Text.Font = GameFont.Tiny;
            listingStandard.Label("Minimum combat points for TradersGuild settlement defenders.");
            listingStandard.Label("Higher values spawn stronger/more defenders to match custom layout loot.");
            listingStandard.Label("Set to 0 to use vanilla wealth-based calculation. Requires custom layouts.");
            Text.Font = previousFont;

            UnityEngine.GUI.enabled = true;

            listingStandard.Gap(24f);

            listingStandard.End();
        }

        public override string SettingsCategory()
        {
            return "Better Traders Guild";
        }
    }
}
