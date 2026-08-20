using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    // Harmony patch: ACTUAL MAIN PATCH for shuttle Launch destination menu!
    // Settlement.GetCaravanGizmos generates the action buttons when clicking on a settlement during targeting
    // This is the method that's actually called for shuttle caravans!
    [HarmonyPatch(typeof(RimWorld.Planet.Settlement), nameof(RimWorld.Planet.Settlement.GetCaravanGizmos))]
    public static class SettlementGetCaravanGizmos
    {
        // Postfix method - modifies caravan action gizmos for Traders Guild settlements
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, RimWorld.Planet.Settlement __instance, Caravan caravan)
        {
            // Check if this is a Traders Guild settlement
            bool isTradersGuild = TradersGuildHelper.IsTradersGuildSettlement(__instance);
            bool canPeacefullyVisit = isTradersGuild && TradersGuildHelper.CanPeacefullyVisit(__instance.Faction);

            // Check if caravan is already AT this settlement (don't add duplicate gizmos)
            bool caravanAtSettlement = caravan.Tile == __instance.Tile;

            // Track which gizmo types we've seen
            bool hasTradeGizmo = false;

            // Vanilla's gizmo labels, matched by their exact translation keys (the attack gizmo
            // from the Attackable branch of Settlement.GetCaravanGizmos, the trade gizmo from
            // CaravanVisitUtility.TradeCommand). Exact translated equality stays correct in every
            // locale (substring-matching the English word missed translated attack labels,
            // leaving a gizmo live whose action calls SettlementUtility.Attack with no
            // CanAttack recheck), avoids catching quest gizmos like Fulfill trade request, and
            // adds no per-frame ToLower allocations.
            string attackLabel = isTradersGuild ? (string)"CommandAttackSettlement".Translate() : null;
            string tradeLabel = isTradersGuild ? (string)"CommandTrade".Translate() : null;

            foreach (Gizmo gizmo in __result)
            {
                // For non-TradersGuild settlements, return gizmos unchanged
                if (!isTradersGuild)
                {
                    yield return gizmo;
                    continue;
                }

                // Check if this is a Command_Action (most action buttons)
                if (gizmo is Command_Action command)
                {
                    // ATTACK GIZMOS: Disable and add signal jammer message
                    if (command.defaultLabel == attackLabel)
                    {
                        command.Disable("BTG_RequiresSignalJammer".Translate());
                        yield return command;
                    }
                    // TRADE GIZMOS: Replace with correctly faction-checked version
                    else if (command.defaultLabel == tradeLabel)
                    {
                        hasTradeGizmo = true;
                        string blockedReason = TradersGuildHelper.GetTradeBlockedReason(caravan, __instance);
                        if (blockedReason != null)
                        {
                            command.Disable(blockedReason);
                        }
                        else
                        {
                            command.action = delegate
                            {
                                TradersGuildHelper.OpenTradeDialog(caravan, __instance);
                            };
                        }
                        yield return command;
                    }
                    // OTHER GIZMOS: Return unchanged
                    else
                    {
                        yield return command;
                    }
                }
                else
                {
                    // Non-command gizmos, return as-is
                    yield return gizmo;
                }
            }

            // If this is a friendly Traders Guild settlement and no trade gizmo was generated, add one
            // BUT only if caravan is not already at the settlement (to avoid duplicate gizmos)
            if (isTradersGuild && canPeacefullyVisit && !hasTradeGizmo && !caravanAtSettlement)
            {
                // Create a trade gizmo
                Command_Action tradeGizmo = new Command_Action();
                tradeGizmo.defaultLabel = "CommandTrade".Translate();
                tradeGizmo.defaultDesc = "CommandTradeDesc".Translate();
                tradeGizmo.icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/Trade", true);
                tradeGizmo.action = delegate
                {
                    // Initiate trade arrival action
                    CaravanArrivalAction_Trade tradeAction = new CaravanArrivalAction_Trade(__instance);
                    caravan.pather.StartPath(__instance.Tile, tradeAction, true);
                };

                yield return tradeGizmo;
            }
        }
    }
}
