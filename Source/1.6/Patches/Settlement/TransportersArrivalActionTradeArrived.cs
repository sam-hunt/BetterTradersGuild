using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Fixes shuttle trade-on-arrival at TG settlements with title-gated traders.
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

        /// <summary>
        /// For TG settlements with title-gated traders, replaces vanilla's faction check
        /// with our corrected version so the arrival action passes StillValid.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(IEnumerable<IThingHolder> pods, Settlement settlement,
            ref FloatMenuAcceptanceReport __result)
        {
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return true; // Let vanilla handle non-TG settlements

            if (settlement.TraderKind?.permitRequiredForTrading == null)
                return true; // No title gate, vanilla's logic is fine

            // Replicate vanilla's checks but use the correct faction for the permit check
            if (settlement == null || !settlement.Spawned || settlement.HasMap
                || settlement.Faction == null || settlement.Faction == Faction.OfPlayer
                || settlement.Faction.def.permanentEnemy
                || FactionUtility.HostileTo(settlement.Faction, Faction.OfPlayer)
                || !settlement.CanTradeNow)
            {
                __result = false;
                return false;
            }

            // Check pods for a negotiator using the trader's faction (not settlement faction)
            Faction tradeCheckFaction = TradersGuildHelper.GetFactionForTradeCheck(settlement);
            foreach (IThingHolder pod in pods)
            {
                ThingOwner thingsOwner = pod.GetDirectlyHeldThings();
                CompTransporter compTransporter = pod as CompTransporter;
                if (compTransporter != null && CaravanShuttleUtility.IsCaravanShuttle(compTransporter))
                {
                    Caravan caravan = CaravanUtility.GetCaravan(compTransporter.parent);
                    if (caravan != null)
                        thingsOwner = caravan.GetDirectlyHeldThings();
                }

                foreach (Thing thing in thingsOwner)
                {
                    Pawn pawn = thing as Pawn;
                    if (pawn == null || !pawn.RaceProps.Humanlike)
                        continue;

                    AcceptanceReport report = FactionUtility.CanTradeWith(
                        pawn, tradeCheckFaction, settlement.TraderKind);
                    if (report.Accepted)
                    {
                        __result = true;
                        return false;
                    }
                }
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    public static class TransportersArrivalActionTradeArrived
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(TransportersArrivalAction_Trade), "Arrived");
        }

        /// <summary>
        /// After vanilla's Arrived runs (skipping the dialog due to wrong faction in
        /// HasNegotiator), opens the trade dialog using the correct faction.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(TransportersArrivalAction_Trade __instance)
        {
            Settlement settlement = Traverse.Create(__instance).Field("settlement").GetValue<Settlement>();
            if (settlement == null || !TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            if (settlement.TraderKind?.permitRequiredForTrading == null)
                return;

            // If vanilla already opened the dialog, don't duplicate
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

            Pawn negotiator = TradersGuildHelper.FindNegotiator(caravan, settlement);
            if (negotiator == null)
                return;

            CameraJumper.TryJumpAndSelect(
                (GlobalTargetInfo)caravan,
                CameraJumper.MovementMode.Cut);
            Find.WindowStack.Add(new Dialog_Trade(negotiator, settlement, false));
        }
    }
}
