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
        /// </summary>
        [HarmonyPostfix]
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
                        option.Label + " (requires signal jammer)",
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
                // Create a trade arrival action for transporters (shuttles)
                // This uses the same mechanism as transport pods
                FloatMenuOption tradeOption = new FloatMenuOption(
                    "Trade with " + __instance.Label,
                    delegate
                    {
                        // Create a TransportersArrivalAction_Trade and execute it
                        // Second parameter is the translation key for the arrival message
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
