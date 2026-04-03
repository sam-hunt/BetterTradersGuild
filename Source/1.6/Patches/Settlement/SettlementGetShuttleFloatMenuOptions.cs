using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: THE KEY PATCH for shuttle destination targeting!
    /// Settlement.GetShuttleFloatMenuOptions is called when clicking on settlements during shuttle launch targeting
    /// This handles the float menu that appears when targeting a settlement with shuttles
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.Settlement), nameof(RimWorld.Planet.Settlement.GetShuttleFloatMenuOptions))]
    public static class SettlementGetShuttleFloatMenuOptions
    {
        /// <summary>
        /// Postfix method - modifies shuttle destination menu options for Traders Guild settlements
        /// pods = the shuttle contents (IThingHolder)
        /// launchAction = the action to execute when launching (used to create TransportersArrivalAction)
        /// Priority.Last ensures we wrap ALL other postfixes (e.g., "Choose where to land" mod)
        /// so their added attack variants pass through our filter too.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            RimWorld.Planet.Settlement __instance,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            // Check if this is a Traders Guild settlement
            bool isTradersGuild = TradersGuildHelper.IsTradersGuildSettlement(__instance);
            bool canPeacefullyVisit = isTradersGuild && TradersGuildHelper.CanPeacefullyVisit(__instance.Faction);

            // Track which option types we've seen
            bool hasTradeOption = false;

            foreach (FloatMenuOption option in __result)
            {
                // For non-TradersGuild settlements, return options unchanged
                if (!isTradersGuild)
                {
                    yield return option;
                    continue;
                }

                // Check option label to determine type
                string label = option.Label.ToLower();

                // ATTACK OPTIONS: Modify to show signal jammer requirement
                if (label.Contains("attack"))
                {
                    // For Traders Guild, always add signal jammer message and disable
                    FloatMenuOption modifiedOption = new FloatMenuOption(
                        option.Label + " " + "BTG_RequiresSignalJammer".Translate(),
                        null  // Disable the action
                    );

                    yield return modifiedOption;
                }
                // TRADE OPTIONS: Check if trade option exists
                else if (label.Contains("trade"))
                {
                    hasTradeOption = true;
                    yield return option;  // Return as-is
                }
                // OTHER OPTIONS: Return unchanged
                else
                {
                    yield return option;
                }
            }

            // If this is a friendly Traders Guild settlement and no trade option was generated, add one
            if (isTradersGuild && canPeacefullyVisit && !hasTradeOption)
            {
                string tradeLabel = "TradeWithSettlement".Translate(__instance.Label);
                string blockedReason = TradersGuildHelper.GetTradeBlockedReasonFromPods(pods, __instance);

                if (blockedReason != null)
                {
                    // Show disabled option with rejection reason (e.g., title requirement)
                    FloatMenuOption disabledOption = new FloatMenuOption(tradeLabel + " (" + blockedReason + ")", null);
                    yield return disabledOption;
                }
                else
                {
                    FloatMenuOption tradeOption = new FloatMenuOption(
                        tradeLabel,
                        delegate
                        {
                            TransportersArrivalAction_Trade tradeAction =
                                new TransportersArrivalAction_Trade(__instance, "MessageShuttleArrived");
                            launchAction(__instance.Tile, tradeAction);
                        }
                    );

                    yield return tradeOption;
                }
            }
        }
    }
}
