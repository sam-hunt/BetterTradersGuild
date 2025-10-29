using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BetterTradersGuild.Patches.CaravanArrivalActions
{
    /// <summary>
    /// Harmony patch: Ensures "Trade" option appears for friendly Traders Guild settlements
    /// Postfix patches run AFTER CaravanArrivalAction_Trade.GetFloatMenuOptions generates options
    /// </summary>
    [HarmonyPatch(typeof(CaravanArrivalAction_Trade), nameof(CaravanArrivalAction_Trade.GetFloatMenuOptions))]
    public static class CaravanArrivalActionTradeGetFloatMenuOptions
    {
        [HarmonyPostfix]
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Caravan caravan, Settlement settlement)
        {
            // First, return all original options
            foreach (FloatMenuOption option in __result)
            {
                yield return option;
            }

            // Check if this is a Traders Guild settlement
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                yield break;

            // Check if we have good relations
            if (!TradersGuildHelper.CanPeacefullyVisit(settlement.Faction))
                yield break;

            // If we already got options from vanilla, don't add duplicates
            bool hasOptions = __result.Any();
            if (hasOptions)
            {
                // Vanilla generated options successfully, nothing more to do
                yield break;
            }

            // Vanilla didn't generate trade options for this space settlement
            // This happens because space settlements aren't normally tradeable
            // We'll manually create a trade option that triggers when selected
            FloatMenuOption tradeOption = new FloatMenuOption(
                "Trade with " + settlement.Label,
                delegate
                {
                    // Create and execute a trade arrival action
                    // Call Arrived() directly to open trade immediately
                    CaravanArrivalAction_Trade tradeAction = new CaravanArrivalAction_Trade(settlement);
                    tradeAction.Arrived(caravan);
                }
            );

            yield return tradeOption;
        }
    }
}
