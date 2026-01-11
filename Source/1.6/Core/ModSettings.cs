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
        /// Enable enhanced pawn generation for TradersGuild settlements
        /// </summary>
        /// <remarks>
        /// Requires useCustomLayouts to be enabled
        /// Default: true
        /// </remarks>
        public bool useEnhancedPawnGeneration = true;

        /// <summary>
        /// Percentage of trade inventory to spawn as cargo in shuttle bay
        /// </summary>
        /// <remarks>
        /// Range: 0.0-1.0 (0-100%)
        /// 0 = disabled (no cargo spawns, no TradersGuildSettlementComponent added)
        /// Default: 0.05 (5%)
        /// Requires useCustomLayouts to be enabled
        /// </remarks>
        public float cargoInventoryPercentage = 0.05f;

        // ===== PHASE 3: SENTRY DRONE SYSTEM =====

        /// <summary>
        /// Sentry drone presence as a factor of threat points
        /// </summary>
        /// <remarks>
        /// Range: 0.0-2.0 (0-200% of threat points)
        /// 0 = disabled (no sentry drones spawn)
        /// Default: 0.3 (30% of threat points used for drone calculation)
        /// Uses minimum threat points cap from PawnGroupMakerUtilityMinimumPoints
        /// Requires useCustomLayouts to be enabled
        /// </remarks>
        public float sentryDronePresence = 0.3f;

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
            Scribe_Values.Look(ref useEnhancedPawnGeneration, "useEnhancedPawnGeneration", true);
            Scribe_Values.Look(ref cargoInventoryPercentage, "cargoInventoryPercentage", 0.05f);
            Scribe_Values.Look(ref sentryDronePresence, "sentryDronePresence", 0.3f);
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
            listingStandard.CheckboxLabeled("Use custom settlement layouts", ref settings.useCustomLayouts,
                "Generate TradersGuild settlements with custom merchant aesthetics (18 room types).\n" +
                "Disable if using other map generation mods or prefer vanilla layouts.");
            listingStandard.Gap(6f);

            // Enhanced pawn generation checkbox (grayed out if layouts disabled)
            UnityEngine.GUI.enabled = settings.useCustomLayouts;
            listingStandard.CheckboxLabeled("Use enhanced pawn generation", ref settings.useEnhancedPawnGeneration,
                "Spawn specialized crew members in custom rooms.\n" +
                "Requires custom layouts to be enabled.");
            UnityEngine.GUI.enabled = true;

            listingStandard.Gap(24f);

            // ========== SECTION: CARGO SYSTEM ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("Cargo System");
            Text.Font = GameFont.Small;
            listingStandard.Gap(12f);

            // Cargo percentage slider (grayed out if layouts disabled)
            UnityEngine.GUI.enabled = settings.useCustomLayouts;

            int cargoPercentageDisplay = (int)(settings.cargoInventoryPercentage * 100f);
            string cargoLabel = $"Cargo bay inventory: {cargoPercentageDisplay}%";

            if (cargoPercentageDisplay == 0)
            {
                cargoLabel += " (Disabled)";
            }
            else if (cargoPercentageDisplay == 5)
            {
                cargoLabel += " (Default)";
            }

            listingStandard.Label(cargoLabel);

            // Slider with range 0-100%, step 5%
            float cargoSliderValue = listingStandard.Slider(settings.cargoInventoryPercentage * 100f, 0f, 100f);
            settings.cargoInventoryPercentage = (int)(System.Math.Round(cargoSliderValue / 5f) * 5f) / 100f;

            listingStandard.Gap(6f);

            // Description text
            Text.Font = GameFont.Tiny;
            listingStandard.Label("Percentage of trade inventory spawned as cargo in shuttle bay.");
            listingStandard.Label("Set to 0% to disable cargo spawning (reduces save file size).");
            listingStandard.Label("Requires custom layouts to be enabled.");
            Text.Font = previousFont;

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
            else if (dronePercentageDisplay == 25)
            {
                droneLabel += " (Default)";
            }

            listingStandard.Label(droneLabel);

            // Slider with range 0-200%, step 25%
            float droneSliderValue = listingStandard.Slider(settings.sentryDronePresence * 100f, 0f, 200f);
            settings.sentryDronePresence = (int)(System.Math.Round(droneSliderValue / 25f) * 25f) / 100f;

            listingStandard.Gap(6f);

            // Description text
            Text.Font = GameFont.Tiny;
            listingStandard.Label("Factor of threat points used for sentry drone spawning.");
            listingStandard.Label("Sentry drones patrol until they detect intruders, then attack.");
            listingStandard.Label("Set to 0% to disable sentry drones. Requires custom layouts.");
            Text.Font = previousFont;

            listingStandard.Gap(24f);

            // ========== SECTION: COMBAT DIFFICULTY ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("Combat Difficulty");
            Text.Font = GameFont.Small;
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
