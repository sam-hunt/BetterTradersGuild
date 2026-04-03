using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patches fixing shuttle trade-on-arrival at TG settlements with title-gated traders.
    ///
    /// Two patches work together:
    /// 1. CanTradeWith Prefix: Vanilla's StillValid calls CanTradeWith(pods, settlement) using
    ///    settlement.Faction (TradersGuild), which fails for Imperial traders. Without this fix,
    ///    the arrival action is discarded before Arrived is ever called.
    /// 2. Arrived Postfix: After vanilla's Arrived runs (and skips the dialog due to wrong faction
    ///    in HasNegotiator), this opens the trade dialog with the correct faction.
    /// </summary>
    [HarmonyPatch]
    public static class TransportersArrivalActionTradeCanTradeWith
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(TransportersArrivalAction_Trade), "CanTradeWith",
                new[] { typeof(IEnumerable<IThingHolder>), typeof(Settlement) });
        }

        [HarmonyPrefix]
        public static bool Prefix(IEnumerable<IThingHolder> pods, Settlement settlement,
            ref FloatMenuAcceptanceReport __result)
        {
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return true;

            if (settlement.TraderKind?.permitRequiredForTrading == null)
                return true;

            if (!settlement.Spawned || settlement.HasMap
                || settlement.Faction == null || settlement.Faction == Faction.OfPlayer
                || settlement.Faction.def.permanentEnemy
                || FactionUtility.HostileTo(settlement.Faction, Faction.OfPlayer)
                || !settlement.CanTradeNow)
            {
                __result = false;
                return false;
            }

            __result = TradersGuildHelper.HasNegotiatorInPods(pods, settlement);
            return false;
        }
    }

    [HarmonyPatch]
    public static class TransportersArrivalActionTradeArrived
    {
        private static readonly FieldInfo settlementField = AccessTools.Field(
            typeof(TransportersArrivalAction_VisitSettlement), "settlement");

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(TransportersArrivalAction_Trade), "Arrived");
        }

        [HarmonyPostfix]
        public static void Postfix(TransportersArrivalAction_Trade __instance)
        {
            Settlement settlement = (Settlement)settlementField.GetValue(__instance);
            if (settlement == null || !TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            if (settlement.TraderKind?.permitRequiredForTrading == null)
                return;

            if (Find.WindowStack.IsOpen<Dialog_Trade>())
                return;

            // Find the caravan that base.Arrived just formed at the settlement tile
            List<Caravan> caravans = Find.WorldObjects.Caravans;
            Caravan caravan = null;
            for (int i = 0; i < caravans.Count; i++)
            {
                if (caravans[i].Tile == settlement.Tile && caravans[i].IsPlayerControlled)
                {
                    caravan = caravans[i];
                    break;
                }
            }

            if (caravan == null)
            {
                Log.Warning("[Better Traders Guild] Shuttle arrived at TG settlement but no caravan found at tile.");
                return;
            }

            TradersGuildHelper.OpenTradeDialog(caravan, settlement);
        }
    }
}
