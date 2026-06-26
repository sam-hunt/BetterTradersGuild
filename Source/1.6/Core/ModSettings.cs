using BetterTradersGuild.Patches.SettlementPatches;
using BetterTradersGuild.WorldComponents;
using UnityEngine;
using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Mod settings and configuration.
    ///
    /// This class is split across several files (see <c>Core/Settings/</c>): each UI
    /// section owns its own fields, scribe entries, defaults, and draw method in a
    /// dedicated partial-class file, so adding or tuning a setting is a one-file
    /// edit. This file holds only the structural glue — the scroll/reset frame in
    /// <see cref="DoWindowContents"/>, the per-section orchestration of
    /// <see cref="ExposeData"/> / <see cref="ResetToDefaults"/>, and the shared
    /// <see cref="SectionHeader"/> / <see cref="Annotate"/> / <see cref="Description"/>
    /// helpers.
    /// </summary>
    public partial class BetterTradersGuildSettings : ModSettings
    {
        // Transient UI state for the scrollable settings panel — not serialized.
        private Vector2 settingsScroll;
        private float settingsHeight;

        // Each section's fields, scribe entries, defaults, and draw method live in
        // its own partial-class file under Core/Settings/. These orchestrators just
        // fan out to them in display order; serialization order is immaterial
        // (Scribe is keyed by name).
        public override void ExposeData()
        {
            base.ExposeData();
            ExposeTradingSettings();
            ExposeMiscSettings();
            ExposeMapGenerationSettings();
            ExposeDefenderSettings();
            ExposeResupplySettings();
        }

        public void ResetToDefaults()
        {
            ResetTradingSettings();
            ResetMiscSettings();
            ResetMapGenerationSettings();
            ResetDefenderSettings();
            ResetResupplySettings();
        }

        public void DoWindowContents(Rect inRect)
        {
            const float buttonHeight = 30f;
            const float buttonGap = 10f;

            // Reserve a row at the bottom for the reset button; everything above
            // scrolls. settingsHeight (set at the end of the previous frame) drives
            // the scrollable content height so the view grows as we add settings.
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - buttonHeight - buttonGap);
            Rect buttonRect = new Rect(inRect.x, inRect.yMax - buttonHeight, 200f, buttonHeight);

            float innerWidth = viewRect.width - 16f;
            Rect innerRect = new Rect(0f, 0f, innerWidth, Mathf.Max(settingsHeight, viewRect.height));
            Widgets.BeginScrollView(viewRect, ref settingsScroll, innerRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(new Rect(0f, 0f, innerWidth - 8f, 99999f));
            GameFont prevFont = Text.Font;

            // Trading and Misc are global/balance knobs that apply regardless of the
            // map generator; the rest depend on (and self-gate on) custom layouts.
            DrawTradingSection(listing);
            DrawMiscSection(listing);
            DrawMapGenerationSection(listing);
            DrawDefendersSection(listing);
            DrawResupplySection(listing);

            Text.Font = prevFont;
            settingsHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();

            if (Widgets.ButtonText(buttonRect, "BTG_Settings_ResetToDefaults".Translate()))
                ResetToDefaults();
        }

        /// <summary>Top-level section heading (medium font), e.g. "Trading".</summary>
        private static void SectionHeader(Listing_Standard listing, string label)
        {
            Text.Font = GameFont.Medium;
            listing.Label(label);
            Text.Font = GameFont.Small;
            listing.Gap(8f);
        }

        /// <summary>
        /// Appends a "(Vanilla)" / "(BTG Recommended)" tag to a slider label when the
        /// current value matches the vanilla default or BTG's recommended value. The
        /// two are mutually exclusive in practice; vanilla wins if both are passed.
        /// </summary>
        private static string Annotate(string label, bool vanilla = false, bool recommended = false)
        {
            if (vanilla)
                return label + " " + "BTG_Settings_Vanilla".Translate();
            if (recommended)
                return label + " " + "BTG_Settings_BTGRecommended".Translate();
            return label;
        }

        /// <summary>Explanatory sub-label rendered in tiny font under a control.</summary>
        private static void Description(Listing_Standard listing, string text)
        {
            GameFont prev = Text.Font;
            Text.Font = GameFont.Tiny;
            listing.Label(text);
            Text.Font = prev;
        }
    }

    /// <summary>
    /// Mod class for handling settings UI. The window itself is drawn by
    /// <see cref="BetterTradersGuildSettings.DoWindowContents"/>; this class owns the
    /// lifecycle glue (category name, and re-aligning the trader rotation cache when
    /// the interval setting changes).
    /// </summary>
    public class BetterTradersGuildMod_ModClass : Mod
    {
        public BetterTradersGuildSettings settings;

        /// <summary>
        /// Tracks the rotation interval before settings window changes.
        /// Used to detect when the interval changes and update preview caches.
        /// </summary>
        private int previousRotationInterval;

        public BetterTradersGuildMod_ModClass(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<BetterTradersGuildSettings>();
            this.previousRotationInterval = settings.traderRotationIntervalDays;
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "BTG_Settings_ModName".Translate();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();

            // Check if rotation interval changed
            if (settings.traderRotationIntervalDays != previousRotationInterval)
            {
                // Scale cache expiration times proportionally to preserve trader types
                // Example: 30→15 days means "departs in 12 days" becomes "departs in 6 days"
                int oldIntervalTicks = previousRotationInterval * 60000;
                int newIntervalTicks = settings.traderRotationIntervalDays * 60000;
                TradersGuildWorldComponent.GetComponent()?.ScaleExpirationsForIntervalChange(oldIntervalTicks, newIntervalTicks);

                // Clear local cache - it uses lastStockTicks as key which doesn't update for scaling
                SettlementTraderTrackerGetTraderKind.ClearLocalCache();

                previousRotationInterval = settings.traderRotationIntervalDays;
            }
        }
    }
}
