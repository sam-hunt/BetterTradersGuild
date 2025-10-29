using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement.GetFloatMenuOptions for regular caravan interactions
    /// This is called when right-clicking on settlements or during caravan pathing
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.Settlement), nameof(RimWorld.Planet.Settlement.GetFloatMenuOptions))]
    public static class SettlementGetFloatMenuOptions
    {
        [HarmonyPostfix]
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, RimWorld.Planet.Settlement __instance, Caravan caravan)
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
                FloatMenuOption tradeOption = new FloatMenuOption(
                    "Trade with " + __instance.Label,
                    delegate
                    {
                        // Create and execute a trade arrival action
                        // Call Arrived() directly since caravan is already at the settlement
                        CaravanArrivalAction_Trade tradeAction = new CaravanArrivalAction_Trade(__instance);
                        tradeAction.Arrived(caravan);
                    }
                );

                yield return tradeOption;
            }
        }
    }
}
