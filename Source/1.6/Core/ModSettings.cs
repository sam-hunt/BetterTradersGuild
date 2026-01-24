using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Mod settings and configuration
    /// </summary>
    public class BetterTradersGuildSettings : ModSettings
    {
        // ===== CORE FEATURES (ALWAYS ENABLED) =====

        /// <summary>
        /// Trader rotation interval in days (how often orbital traders change at settlements)
        /// </summary>
        /// <remarks>
        /// Range: 5-30 days
        /// Default: 30 days (same as vanilla)
        /// </remarks>
        public int traderRotationIntervalDays = 30;

        // ===== MAP GENERATION FEATURES =====

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

        // ===== SENTRY DRONE SYSTEM =====

        /// <summary>
        /// Additional sentry drone presence as a factor of threat points
        /// </summary>
        /// <remarks>
        /// Range: 0.0-2.0 (0-200% of threat points)
        /// 0 = vanilla (no additional sentry drones)
        /// Default: 0.25 (25% of threat points used for drone calculation)
        /// Uses minimum threat points cap from PawnGroupMakerUtilityMinimumPoints
        /// Requires useCustomLayouts to be enabled
        /// </remarks>
        public float sentryDronePresence = 0.25f;

        /// <summary>
        /// Threat points multiplier for TradersGuild settlement initial defender generation
        /// </summary>
        /// <remarks>
        /// Range: 0.5-3.0
        /// Default: 1.0 (no modification)
        /// Applied after minimum threat points cap
        /// Requires useCustomLayouts to be enabled
        /// Only affects initial defenders when entering settlements, not subsequent raid incidents
        /// </remarks>
        public float threatPointsMultiplier = 1.0f;

        /// <summary>
        /// Minimum threat points for TradersGuild settlement initial defender generation
        /// </summary>
        /// <remarks>
        /// Range: 0-5000
        /// 0 = vanilla (no minimum floor)
        /// Default: 0 (vanilla behavior)
        /// BTG Recommended: 2400 (ensures elite pawn types can spawn at low wealth)
        /// Requires useCustomLayouts to be enabled
        /// Only affects initial defenders when entering settlements, not subsequent raid incidents
        /// </remarks>
        public float minimumThreatPoints = 0f;

        // ===== BALANCE ADJUSTMENTS =====

        /// <summary>
        /// Salvagers raid weight multiplier when on TradersGuild maps
        /// </summary>
        /// <remarks>
        /// Range: 0.0-5.0
        /// 1.0 = vanilla (no change)
        /// Default: 3.0 (BTG Recommended)
        /// When attacking TradersGuild settlements, Salvagers raids are more likely.
        /// This creates emergent gameplay where attacking the guild attracts opportunistic pirates.
        /// </remarks>
        public float salvagersRaidWeightMultiplier = 3.0f;

        /// <summary>
        /// LifeSupportUnit power output in watts
        /// </summary>
        /// <remarks>
        /// Range: 0-5000W
        /// Default: 1200W (balanced for connected power grids)
        /// Vanilla: 3200W (designed for isolated rooms)
        /// BTG connects buildings in a map-wide grid, so vanilla output would be excessive
        /// </remarks>
        public int lifeSupportUnitPowerOutput = 1200;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref traderRotationIntervalDays, "traderRotationIntervalDays", 30);
            Scribe_Values.Look(ref useCustomLayouts, "useCustomLayouts", true);
            Scribe_Values.Look(ref enableCargoVault, "enableCargoVault", true);
            Scribe_Values.Look(ref sentryDronePresence, "sentryDronePresence", 0.25f);
            Scribe_Values.Look(ref threatPointsMultiplier, "threatPointsMultiplier", 1.0f);
            Scribe_Values.Look(ref minimumThreatPoints, "minimumThreatPoints", 0f);
            Scribe_Values.Look(ref lifeSupportUnitPowerOutput, "lifeSupportUnitPowerOutput", 1200);
            Scribe_Values.Look(ref salvagersRaidWeightMultiplier, "salvagersRaidWeightMultiplier", 3.0f);
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

            GameFont previousFont = Text.Font;

            // ========== SECTION: TRADING ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("BTG_Settings_Trading".Translate());
            Text.Font = GameFont.Small;
            listingStandard.Gap(6f);

            listingStandard.Indent(12f);
            listingStandard.ColumnWidth -= 12f;

            // Trader rotation interval slider
            string intervalLabel = "BTG_Settings_TraderRotationInterval".Translate(settings.traderRotationIntervalDays);

            if (settings.traderRotationIntervalDays == 30)
            {
                intervalLabel += " " + "BTG_Settings_Vanilla".Translate();
            }

            listingStandard.Label(intervalLabel);

            float sliderValue = listingStandard.Slider(settings.traderRotationIntervalDays, 0f, 60f);
            settings.traderRotationIntervalDays = (int)(System.Math.Round(sliderValue / 5f) * 5f);

            listingStandard.Gap(2f);

            Text.Font = GameFont.Tiny;
            listingStandard.Label("BTG_Settings_TraderRotationDesc1".Translate());
            listingStandard.Label("BTG_Settings_TraderRotationDesc2".Translate());
            Text.Font = previousFont;

            listingStandard.ColumnWidth += 12f;
            listingStandard.Outdent(12f);

            listingStandard.Gap(24f);

            // ========== SECTION: MAP GENERATION ==========
            Text.Font = GameFont.Medium;
            listingStandard.Label("BTG_Settings_MapGeneration".Translate());
            Text.Font = GameFont.Small;
            listingStandard.Gap(6f);

            listingStandard.Indent(12f);
            listingStandard.ColumnWidth -= 12f;

            // Custom layouts checkbox
            listingStandard.CheckboxLabeled("BTG_Settings_UseCustomLayouts".Translate(), ref settings.useCustomLayouts,
                "BTG_Settings_UseCustomLayoutsDesc".Translate());
            listingStandard.Gap(12f);

            // All remaining settings are grayed out if custom layouts disabled
            UnityEngine.GUI.enabled = settings.useCustomLayouts;

            // Cargo vault checkbox
            listingStandard.CheckboxLabeled("BTG_Settings_EnableCargoVault".Translate(), ref settings.enableCargoVault,
                "BTG_Settings_EnableCargoVaultDesc".Translate());
            listingStandard.Gap(12f);

            // Threat points multiplier slider
            string multiplierLabel = "BTG_Settings_ThreatMultiplier".Translate(settings.threatPointsMultiplier.ToString("F1"));

            if (settings.threatPointsMultiplier == 1.0f)
            {
                multiplierLabel += " " + "BTG_Settings_Vanilla".Translate();
            }

            listingStandard.Label(multiplierLabel);

            float multiplierSliderValue = listingStandard.Slider(settings.threatPointsMultiplier, 0.5f, 3.0f);
            settings.threatPointsMultiplier = (float)(System.Math.Round(multiplierSliderValue / 0.25) * 0.25);

            listingStandard.Gap(2f);

            Text.Font = GameFont.Tiny;
            listingStandard.Label("BTG_Settings_ThreatMultiplierDesc".Translate());
            Text.Font = previousFont;

            listingStandard.Gap(16f);

            // Minimum threat points slider
            int threatPointsDisplay = (int)settings.minimumThreatPoints;
            string threatLabel = "BTG_Settings_MinThreatPoints".Translate(threatPointsDisplay);

            if (threatPointsDisplay == 0)
            {
                threatLabel += " " + "BTG_Settings_Vanilla".Translate();
            }
            else if (threatPointsDisplay == 2400)
            {
                threatLabel += " " + "BTG_Settings_BTGRecommended".Translate();
            }

            listingStandard.Label(threatLabel);

            float threatSliderValue = listingStandard.Slider(settings.minimumThreatPoints, 0f, 5000f);
            settings.minimumThreatPoints = (int)(System.Math.Round(threatSliderValue / 100f) * 100f);

            listingStandard.Gap(2f);

            Text.Font = GameFont.Tiny;
            listingStandard.Label("BTG_Settings_MinThreatPointsDesc".Translate());
            Text.Font = previousFont;

            listingStandard.Gap(16f);

            // Additional sentry drone presence slider
            int dronePercentageDisplay = (int)(settings.sentryDronePresence * 100f);
            string droneLabel = "BTG_Settings_SentryDronePresence".Translate(dronePercentageDisplay);

            if (dronePercentageDisplay == 0)
            {
                droneLabel += " " + "BTG_Settings_Vanilla".Translate();
            }
            else if (dronePercentageDisplay == 25)
            {
                droneLabel += " " + "BTG_Settings_BTGRecommended".Translate();
            }

            listingStandard.Label(droneLabel);

            float droneSliderValue = listingStandard.Slider(settings.sentryDronePresence * 100f, 0f, 200f);
            settings.sentryDronePresence = (int)(System.Math.Round(droneSliderValue / 5f) * 5f) / 100f;

            listingStandard.Gap(2f);

            Text.Font = GameFont.Tiny;
            listingStandard.Label("BTG_Settings_SentryDroneDesc".Translate());
            Text.Font = previousFont;

            listingStandard.Gap(16f);

            // Salvagers raid weight multiplier slider
            string salvagersLabel = "BTG_Settings_SalvagersRaidWeight".Translate(settings.salvagersRaidWeightMultiplier.ToString("F1"));

            if (settings.salvagersRaidWeightMultiplier == 1.0f)
            {
                salvagersLabel += " " + "BTG_Settings_Vanilla".Translate();
            }
            else if (settings.salvagersRaidWeightMultiplier == 3.0f)
            {
                salvagersLabel += " " + "BTG_Settings_BTGRecommended".Translate();
            }

            listingStandard.Label(salvagersLabel);

            float salvagersSliderValue = listingStandard.Slider(settings.salvagersRaidWeightMultiplier, 0f, 5f);
            settings.salvagersRaidWeightMultiplier = (float)(System.Math.Round(salvagersSliderValue / 0.5) * 0.5);

            listingStandard.Gap(2f);

            Text.Font = GameFont.Tiny;
            listingStandard.Label("BTG_Settings_SalvagersRaidWeightDesc".Translate());
            Text.Font = previousFont;

            listingStandard.Gap(16f);

            // LifeSupportUnit power output slider
            string powerLabel = "BTG_Settings_LifeSupportPower".Translate(settings.lifeSupportUnitPowerOutput);

            if (settings.lifeSupportUnitPowerOutput == 1200)
            {
                powerLabel += " " + "BTG_Settings_BTGRecommended".Translate();
            }
            else if (settings.lifeSupportUnitPowerOutput == 3200)
            {
                powerLabel += " " + "BTG_Settings_Vanilla".Translate();
            }

            listingStandard.Label(powerLabel);

            float powerSliderValue = listingStandard.Slider(settings.lifeSupportUnitPowerOutput, 0f, 5000f);
            settings.lifeSupportUnitPowerOutput = (int)(System.Math.Round(powerSliderValue / 100f) * 100f);

            listingStandard.Gap(2f);

            Text.Font = GameFont.Tiny;
            listingStandard.Label("BTG_Settings_LifeSupportDesc".Translate());
            Text.Font = previousFont;

            listingStandard.ColumnWidth += 12f;
            listingStandard.Outdent(12f);

            UnityEngine.GUI.enabled = true;

            listingStandard.End();
        }

        public override string SettingsCategory()
        {
            return "BTG_Settings_ModName".Translate();
        }
    }
}
