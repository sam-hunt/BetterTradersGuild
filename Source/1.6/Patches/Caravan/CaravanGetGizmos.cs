using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.CaravanPatches
{
    /// <summary>
    /// Harmony patch: Adds trade gizmo and modifies attack gizmo when caravan is at Traders Guild settlement
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.Caravan), nameof(RimWorld.Planet.Caravan.GetGizmos))]
    public static class CaravanGetGizmos
    {
        /// <summary>
        /// Postfix method - adds/modifies gizmos for caravans at Traders Guild settlements
        /// </summary>
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, RimWorld.Planet.Caravan __instance)
        {
            // Track if we need to modify gizmos for Traders Guild
            Settlement tradersGuildSettlement = null;
            bool canTrade = false;

            // Check if caravan is at a Traders Guild settlement tile
            WorldObjectsHolder worldObjects = Find.WorldObjects;
            if (worldObjects != null)
            {
                Settlement settlement = worldObjects.SettlementAt(__instance.Tile);
                if (settlement != null && TradersGuildHelper.IsTradersGuildSettlement(settlement))
                {
                    tradersGuildSettlement = settlement;
                    canTrade = TradersGuildHelper.CanPeacefullyVisit(settlement.Faction);
                }
            }

            // First, yield all original gizmos, modifying attack gizmo if needed
            foreach (Gizmo gizmo in __result)
            {
                // Check if this is an attack gizmo for Traders Guild
                if (tradersGuildSettlement != null && gizmo is Command_Action attackCommand)
                {
                    // Identify attack gizmos by their icon or label
                    // Attack gizmos typically have "Attack" in the label
                    if (attackCommand.defaultLabel != null && attackCommand.defaultLabel.Contains("Attack"))
                    {
                        // Disable this gizmo and add signal jammer message to tooltip
                        attackCommand.Disable("Requires signal jammer");
                        yield return attackCommand;
                        continue;
                    }
                }

                // Return gizmo unchanged
                yield return gizmo;
            }

            // If at friendly Traders Guild settlement, add trade gizmo
            if (tradersGuildSettlement != null && canTrade)
            {
                yield return CreateTradeGizmo(__instance, tradersGuildSettlement);
            }
        }

        /// <summary>
        /// Creates a "Trade" gizmo for caravans at Traders Guild settlements
        /// </summary>
        private static Command_Action CreateTradeGizmo(RimWorld.Planet.Caravan caravan, Settlement settlement)
        {
            Command_Action tradeCommand = new Command_Action();

            tradeCommand.defaultLabel = "Trade";
            tradeCommand.defaultDesc = "Trade with " + settlement.Label;
            tradeCommand.icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/Trade", true);

            tradeCommand.action = delegate
            {
                // Open trading dialog
                // The caravan is already at the settlement, so we can directly initiate trade

                // For TradersGuild settlements, we trust our orbital trader system patches
                // to ensure traders are properly initialized
                bool isTradersGuild = TradersGuildHelper.IsTradersGuildSettlement(settlement);

                if (isTradersGuild || settlement.CanTradeNow)
                {
                    // Open the trade dialog using the caravan arrival trade mechanism
                    // This works for both traditional settlements and TradersGuild space bases
                    CaravanArrivalAction_Trade tradeAction = new CaravanArrivalAction_Trade(settlement);
                    tradeAction.Arrived(caravan);
                }
                else
                {
                    Messages.Message("Settlement cannot trade right now.", MessageTypes.RejectInput);
                }
            };

            return tradeCommand;
        }
    }
}
