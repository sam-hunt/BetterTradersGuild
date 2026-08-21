using BetterTradersGuild.WorldComponents;
using UnityEngine;
using Verse;

namespace BetterTradersGuild
{
    // Mod settings and configuration.
    //
    // This class is split across several files (see Core/Settings/): each UI
    // section owns its own fields, scribe entries, defaults, and draw method in a
    // dedicated partial-class file, so adding or tuning a setting is a one-file
    // edit. This file holds only the structural glue — the scroll/reset frame in
    // DoWindowContents, the per-section orchestration of
    // ExposeData / ResetToDefaults, and the shared
    // SectionHeader / Annotate / LabelWithTooltip
    // helpers.
    //
    // Help text convention: hover tooltips only (LabelWithTooltip for sliders, the
    // tooltip argument of CheckboxLabeled for checkboxes) — no always-visible
    // tiny-font sub-labels, which read as a wall of text at section scale.
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
            ExposeSettlementGenerationSettings();
            ExposeDefenderSettings();
            ExposeEventsSettings();
        }

        public void ResetToDefaults()
        {
            ResetTradingSettings();
            ResetSettlementGenerationSettings();
            ResetDefenderSettings();
            ResetEventsSettings();
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

            // Grouped by player activity: trade with the guild, generate their
            // settlements, fight their garrisons (settlements and smuggler's den
            // alike; resupply renders inside as a subgroup), storyteller dials.
            DrawTradingSection(listing);
            DrawSettlementGenerationSection(listing);
            DrawDefendersSection(listing);
            DrawEventsSection(listing);

            Text.Font = prevFont;
            settingsHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();

            if (Widgets.ButtonText(buttonRect, "BTG_Settings_ResetToDefaults".Translate()))
                ResetToDefaults();
        }

        // Top-level section heading (medium font), e.g. "Trading".
        private static void SectionHeader(Listing_Standard listing, string label)
        {
            Text.Font = GameFont.Medium;
            listing.Label(label);
            Text.Font = GameFont.Small;
            listing.Gap(8f);
        }

        // Appends status tags to a control's label: "(vanilla)" when the current
        // value matches vanilla behaviour, "(suggested)" when it matches BTG's
        // suggested value (the two are mutually exclusive in practice; vanilla wins
        // if both are passed), and "(default)" when it is the mod's shipped default.
        // The default tag is independent of the other two and renders last, so a
        // shipped default that is also the suggested value reads "(suggested) (default)".
        private static string Annotate(string label, bool vanilla = false, bool recommended = false,
            bool isDefault = false)
        {
            if (vanilla)
                label += " " + "BTG_Settings_Vanilla".Translate();
            else if (recommended)
                label += " " + "BTG_Settings_BTGRecommended".Translate();
            if (isDefault)
                label += " " + "BTG_Settings_Default".Translate();
            return label;
        }

        // Slider label with the explanatory text as a hover tooltip. The explicit
        // TipSignal keeps overload resolution away from the ambiguous
        // Label(TaggedString,...) / Label(string,...) pair.
        private static void LabelWithTooltip(Listing_Standard listing, string label, string tooltip)
        {
            listing.Label(label, -1f, new TipSignal(tooltip));
        }

        // Checkbox whose prerequisite may be off. GUI.enabled alone is not enough
        // for that state: it only fades the visuals, while RimWorld's invisible-
        // button hit test ignores it, so a "greyed" checkbox would still toggle on
        // click. When gated off this draws a genuinely non-interactive checkbox,
        // shown UNCHECKED (the effective state, since the feature can't run) while
        // the stored value stays untouched and reappears once re-enabled.
        private static void CheckboxLabeledGated(Listing_Standard listing, string label, ref bool value,
            string tooltip, bool enabled)
        {
            if (enabled)
            {
                listing.CheckboxLabeled(label, ref value, tooltip);
                return;
            }

            // Mirror Listing_Standard.CheckboxLabeled's rect/tooltip handling, but
            // draw through Widgets' disabled path with a throwaway unchecked state.
            bool prevGuiEnabled = GUI.enabled;
            GUI.enabled = false;
            float height = Text.CalcHeight(label, listing.ColumnWidth);
            Rect rect = listing.GetRect(height);
            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(rect))
                    Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, tooltip);
            }
            bool shownUnchecked = false;
            Widgets.CheckboxLabeled(rect, label, ref shownUnchecked, disabled: true);
            listing.Gap(listing.verticalSpacing);
            GUI.enabled = prevGuiEnabled;
        }
    }

    // Mod class for handling settings UI. The window itself is drawn by
    // BetterTradersGuildSettings.DoWindowContents; this class owns the
    // lifecycle glue (category name, and re-aligning the trader rotation cache when
    // the interval setting changes).
    public class BetterTradersGuildMod_ModClass : Mod
    {
        public BetterTradersGuildSettings settings;

        // Tracks the rotation interval before settings window changes.
        // Used to detect when the interval changes and update preview caches.
        private int previousRotationInterval;

        public BetterTradersGuildMod_ModClass(ModContentPack content) : base(content)
        {
            // PatchAll must run here, not from a [StaticConstructorOnStartup] class: Mod
            // constructors execute at the very start of LoadAllPlayData, which arms the
            // CallAll postfix that drives BTGStartup.Run in time to fire on the same load
            // (see Patches/StaticConstructorOnStartupUtility/StaticConstructorOnStartupUtilityCallAll.cs).
            BetterTradersGuildMod.Harmony.PatchAll();

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

            // Push the life-support power output onto the LifeSupportUnit def and refresh the
            // live output of any already-spawned units (ApplyLifeSupportUnitPowerSetting does
            // both), so the change takes effect immediately with no restart needed.
            BetterTradersGuildMod.ApplyLifeSupportUnitPowerSetting();

            // Push the quest weights onto their QuestScriptDefs, so the next
            // storyteller quest roll uses the new values with no restart needed.
            BetterTradersGuildMod.ApplyQuestWeightSettings();

            // Check if rotation interval changed
            if (settings.traderRotationIntervalDays != previousRotationInterval)
            {
                // Scale cache expiration times proportionally to preserve trader types
                // Example: 30→15 days means "departs in 12 days" becomes "departs in 6 days"
                int oldIntervalTicks = previousRotationInterval * 60000;
                int newIntervalTicks = settings.traderRotationIntervalDays * 60000;
                TradersGuildWorldComponent.GetComponent()?.ScaleExpirationsForIntervalChange(oldIntervalTicks, newIntervalTicks);

                previousRotationInterval = settings.traderRotationIntervalDays;
            }
        }
    }
}
