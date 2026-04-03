using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
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
            // Track whether vanilla generated any options
            bool hasOptions = false;
            foreach (FloatMenuOption option in __result)
            {
                hasOptions = true;
                yield return option;
            }

            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                yield break;

            if (!TradersGuildHelper.CanPeacefullyVisit(settlement.Faction))
                yield break;

            // If vanilla already generated options, don't add duplicates
            if (hasOptions)
                yield break;

            string tradeLabel = "TradeWithSettlement".Translate(settlement.Label);
            string blockedReason = TradersGuildHelper.GetTradeBlockedReason(caravan, settlement);

            if (blockedReason != null)
            {
                yield return new FloatMenuOption(tradeLabel + " (" + blockedReason + ")", null);
                yield break;
            }

            yield return new FloatMenuOption(
                tradeLabel,
                delegate { TradersGuildHelper.OpenTradeDialog(caravan, settlement); }
            );
        }
    }
}
