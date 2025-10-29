using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: ACTUAL MAIN PATCH for shuttle Launch destination menu!
    /// Settlement.GetCaravanGizmos generates the action buttons when clicking on a settlement during targeting
    /// This is the method that's actually called for shuttle caravans!
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.Settlement), nameof(RimWorld.Planet.Settlement.GetCaravanGizmos))]
    public static class SettlementGetCaravanGizmos
    {
        /// <summary>
        /// Postfix method - modifies caravan action gizmos for Traders Guild settlements
        /// </summary>
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
                    string label = command.defaultLabel?.ToLower() ?? "";

                    // ATTACK GIZMOS: Disable and add signal jammer message
                    if (label.Contains("attack"))
                    {
                        command.Disable("Requires signal jammer");
                        yield return command;
                    }
                    // TRADE GIZMOS: Check if trade option exists
                    else if (label.Contains("trade"))
                    {
                        hasTradeGizmo = true;
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
                tradeGizmo.defaultLabel = "Trade with " + __instance.Label;
                tradeGizmo.defaultDesc = "Trade with this settlement";
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
