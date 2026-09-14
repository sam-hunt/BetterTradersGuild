using BetterTradersGuild.RoomContents.CargoVault;
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

        // Cargo vault spawn caps. Some modded orbital traders carry enough stock to
        // crash the game when it is all spawned into the vault at once, so the vault
        // spawns at most this many stacks / this much market value (proportionally
        // across item types; pawns always spawn but count). The rest stays in the
        // trade inventory. Each slider has one extra notch past its max that means
        // unlimited, stored as max + step so the sentinel survives a round-trip
        // through the slider; the *Cap accessors translate it for consumers.
        // Applies to every vault, including the smuggler's den, so never gated.
        public const int CargoVaultStacksMax = 350;
        public const int CargoVaultStacksStep = 5;
        public const int CargoVaultStacksDefault = 200;
        public int cargoVaultMaxStacks = CargoVaultStacksDefault;

        public const int CargoVaultWealthMax = 200000;
        public const int CargoVaultWealthStep = 1000;
        public const int CargoVaultWealthDefault = 75000;
        public int cargoVaultMaxWealth = CargoVaultWealthDefault;

        public int CargoVaultStackCap =>
            cargoVaultMaxStacks > CargoVaultStacksMax ? CargoLimitAllocator.Unlimited : cargoVaultMaxStacks;

        public float CargoVaultWealthCap =>
            cargoVaultMaxWealth > CargoVaultWealthMax ? CargoLimitAllocator.Unlimited : cargoVaultMaxWealth;

        private void ExposeTradingSettings()
        {
            Scribe_Values.Look(ref traderRotationIntervalDays, "traderRotationIntervalDays", 30);
            Scribe_Values.Look(ref enableCargoVault, "enableCargoVault", true);
            Scribe_Values.Look(ref cargoVaultMaxStacks, "cargoVaultMaxStacks", CargoVaultStacksDefault);
            Scribe_Values.Look(ref cargoVaultMaxWealth, "cargoVaultMaxWealth", CargoVaultWealthDefault);
        }

        private void ResetTradingSettings()
        {
            traderRotationIntervalDays = 30;
            enableCargoVault = true;
            cargoVaultMaxStacks = CargoVaultStacksDefault;
            cargoVaultMaxWealth = CargoVaultWealthDefault;
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

            // Spawn caps: not gated on useCustomLayouts because the den vault honours
            // them too. The top notch of each slider reads "Unlimited".
            string stacksValue = cargoVaultMaxStacks > CargoVaultStacksMax
                ? "BTG_Settings_Unlimited".Translate().ToString()
                : cargoVaultMaxStacks.ToString();
            string stacksLabel = Annotate(
                "BTG_Settings_CargoVaultMaxStacks".Translate(stacksValue),
                isDefault: cargoVaultMaxStacks == CargoVaultStacksDefault);
            LabelWithTooltip(listing, stacksLabel, "BTG_Settings_CargoVaultMaxStacksDesc".Translate());
            cargoVaultMaxStacks = SteppedSliderWithUnlimited(listing, cargoVaultMaxStacks,
                CargoVaultStacksMax, CargoVaultStacksStep);

            listing.Gap(12f);

            string wealthValue = cargoVaultMaxWealth > CargoVaultWealthMax
                ? "BTG_Settings_Unlimited".Translate().ToString()
                : "BTG_Settings_CargoVaultMaxWealthValue".Translate(cargoVaultMaxWealth.ToString("N0")).ToString();
            string wealthLabel = Annotate(
                "BTG_Settings_CargoVaultMaxWealth".Translate(wealthValue),
                isDefault: cargoVaultMaxWealth == CargoVaultWealthDefault);
            LabelWithTooltip(listing, wealthLabel, "BTG_Settings_CargoVaultMaxWealthDesc".Translate());
            cargoVaultMaxWealth = SteppedSliderWithUnlimited(listing, cargoVaultMaxWealth,
                CargoVaultWealthMax, CargoVaultWealthStep);

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

        // Integer slider from 0 to max in the given step, plus one final notch
        // (max + step) that stands for unlimited. Values snap to the step.
        private static int SteppedSliderWithUnlimited(Listing_Standard listing, int value, int max, int step)
        {
            float slider = listing.Slider(value, 0f, max + step);
            int snapped = (int)(System.Math.Round(slider / step) * step);
            return snapped > max ? max + step : snapped;
        }
    }
}
