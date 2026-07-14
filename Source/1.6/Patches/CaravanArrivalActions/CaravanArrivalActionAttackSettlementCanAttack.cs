using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.CaravanArrivalActions
{
    // Harmony patch: Structurally block caravan attacks on Traders Guild settlements.
    //
    // Counterpart to TransportersArrivalActionAttackSettlementCanAttack for the caravan arrival
    // action. Vanilla caravans cannot normally path to orbital tiles, but CanAttack is the
    // authoritative gate consulted by the float menu, gizmos, and StillValid, so rejecting it
    // here makes the attack ban language-independent instead of relying on the English
    // label-matching in the float-menu postfixes.
    [HarmonyPatch(typeof(CaravanArrivalAction_AttackSettlement),
        nameof(CaravanArrivalAction_AttackSettlement.CanAttack))]
    public static class CaravanArrivalActionAttackSettlementCanAttack
    {
        [HarmonyPostfix]
        public static void Postfix(ref FloatMenuAcceptanceReport __result, Settlement settlement)
        {
            // Only convert an acceptance into a reasoned rejection. A silent vanilla rejection
            // must stay silent, or the attack option would appear where vanilla showed none.
            if (!__result.Accepted)
                return;

            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            __result = FloatMenuAcceptanceReport.WithFailReason("BTG_RequiresSignalJammerReason".Translate());
        }
    }
}
