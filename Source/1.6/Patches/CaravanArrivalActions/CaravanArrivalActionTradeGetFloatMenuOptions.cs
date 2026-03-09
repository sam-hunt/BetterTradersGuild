using HarmonyLib;
using RimWorld;
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
            // Check if the caravan has a valid negotiator (e.g., Imperial traders require Baron+ title)
            string tradeLabel = "TradeWithSettlement".Translate(settlement.Label);
            string blockedReason = TradersGuildHelper.GetTradeBlockedReason(caravan, settlement);

            if (blockedReason != null)
            {
                // Show disabled option with rejection reason (e.g., title requirement)
                yield return new FloatMenuOption(tradeLabel + " (" + blockedReason + ")", null);
                yield break;
            }

            FloatMenuOption tradeOption = new FloatMenuOption(
                tradeLabel,
                delegate
                {
                    CaravanArrivalAction_Trade tradeAction = new CaravanArrivalAction_Trade(settlement);
                    tradeAction.Arrived(caravan);
                }
            );

            yield return tradeOption;
        }
    }
}
