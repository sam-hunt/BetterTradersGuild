using Verse;

namespace BetterTradersGuild
{
    // "Trading" settings section — orbital trader rotation cadence and the cargo
    // vault. Rotation applies regardless of the map generator; the vault is trade
    // stock made physical, so players look for it here, but it only exists on
    // custom-generated settlement maps (greyed out, value preserved, when
    // useCustomLayouts is off). The smuggler's den vault ignores the toggle: its
    // WorldObjectComp_QuestVault overrides it with the quest reward choice.
    public partial class BetterTradersGuildSettings
    {
        // Trader rotation interval in days (how often orbital traders change at settlements).
        // Range: 5-60 days. Default: 30 days (same as vanilla).
        public int traderRotationIntervalDays = 30;

        // Enable cargo vault access in TradersGuild settlements.
        // When enabled: cargo vault hatch spawns hackable. When disabled: spawns
        // sealed. Default: true. Only affects newly generated maps. Requires
        // useCustomLayouts (no vault room exists under vanilla generation). On the
        // smuggler's den the quest comp overrides this, so it is never consulted there.
        public bool enableCargoVault = true;

        private void ExposeTradingSettings()
        {
            Scribe_Values.Look(ref traderRotationIntervalDays, "traderRotationIntervalDays", 30);
            Scribe_Values.Look(ref enableCargoVault, "enableCargoVault", true);
        }

        private void ResetTradingSettings()
        {
            traderRotationIntervalDays = 30;
            enableCargoVault = true;
        }

        private void DrawTradingSection(Listing_Standard listing)
        {
            SectionHeader(listing, "BTG_Settings_Trading".Translate());

            // The vault only exists on custom-generated settlement maps. While
            // gated off it renders unchecked, so the default tag follows the shown
            // state rather than the stored one.
            string vaultLabel = Annotate(
                "BTG_Settings_EnableCargoVault".Translate(),
                isDefault: useCustomLayouts && enableCargoVault);
            CheckboxLabeledGated(listing, vaultLabel, ref enableCargoVault,
                "BTG_Settings_EnableCargoVaultDesc".Translate(), useCustomLayouts);

            listing.Gap(12f);

            string intervalLabel = Annotate(
                "BTG_Settings_TraderRotationInterval".Translate(traderRotationIntervalDays),
                vanilla: traderRotationIntervalDays == 30,
                isDefault: traderRotationIntervalDays == 30);
            LabelWithTooltip(listing, intervalLabel,
                "BTG_Settings_TraderRotationDesc1".Translate() + "\n" + "BTG_Settings_TraderRotationDesc2".Translate());

            float sliderValue = listing.Slider(traderRotationIntervalDays, 5f, 60f);
            traderRotationIntervalDays = (int)(System.Math.Round(sliderValue / 5f) * 5f);

            listing.Gap(24f);
        }
    }
}
