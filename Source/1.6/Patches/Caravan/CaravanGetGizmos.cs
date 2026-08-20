using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.CaravanPatches
{
    // Harmony patch: Adds trade gizmo and modifies attack gizmo when caravan is at Traders Guild settlement
    [HarmonyPatch(typeof(RimWorld.Planet.Caravan), nameof(RimWorld.Planet.Caravan.GetGizmos))]
    public static class CaravanGetGizmos
    {
        // Postfix method - adds/modifies gizmos for caravans at Traders Guild settlements
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

            // Vanilla's attack gizmo (Settlement.GetCaravanGizmos, re-yielded here) calls
            // SettlementUtility.Attack directly with no CanAttack/StillValid recheck, so this
            // disable is the only gate on it. Identify it by its exact translated label:
            // correct in every locale (substring-matching the English word missed translated
            // labels, leaving the gizmo live) and free of per-frame ToLower allocations.
            string attackLabel = tradersGuildSettlement != null
                ? (string)"CommandAttackSettlement".Translate()
                : null;

            // First, yield all original gizmos, modifying attack gizmo if needed
            foreach (Gizmo gizmo in __result)
            {
                if (attackLabel != null && gizmo is Command_Action attackCommand
                    && attackCommand.defaultLabel == attackLabel)
                {
                    // Disable this gizmo and add signal jammer message to tooltip
                    attackCommand.Disable("BTG_RequiresSignalJammer".Translate());
                    yield return attackCommand;
                    continue;
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

        // Creates a "Trade" gizmo for caravans at Traders Guild settlements
        private static Command_Action CreateTradeGizmo(RimWorld.Planet.Caravan caravan, Settlement settlement)
        {
            Command_Action tradeCommand = new Command_Action();

            tradeCommand.defaultLabel = "CommandTrade".Translate();
            tradeCommand.defaultDesc = "CommandTradeDesc".Translate();
            tradeCommand.icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/Trade", true);

            // Check for a valid negotiator (e.g., Imperial traders require Baron+ title)
            string blockedReason = TradersGuildHelper.GetTradeBlockedReason(caravan, settlement);
            if (blockedReason != null)
            {
                tradeCommand.Disable(blockedReason);
            }
            else
            {
                tradeCommand.action = delegate
                {
                    TradersGuildHelper.OpenTradeDialog(caravan, settlement);
                };
            }

            return tradeCommand;
        }
    }
}
