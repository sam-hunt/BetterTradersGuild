using System.Reflection;
using BetterTradersGuild.Integrations;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.TransportersArrivalActionPatches
{
    // Harmony patch: Structurally block "Choose where to land" attacks on Traders Guild settlements.
    //
    // The exact counterpart to TransportersArrivalActionAttackSettlementCanAttack, but targeting
    // CWTL's own TransportersArrivalAction_CWTLAttackSettlement.CanAttack, which vanilla's gate never
    // reaches (see CWTLIntegration). Rejecting CanAttack here blocks the attack at StillValid for
    // both CWTL's shuttle and transport-pod options and, because the rejection carries our
    // signal-jammer reason, vanilla's float-menu builder appends it to the label so the cosmetic
    // disablers grey the option out.
    //
    // CONDITIONAL: only applies when CWTL is loaded and its CanAttack resolved (Prepare gates it);
    // CWTLIntegration's static constructor warns if CWTL is present but its API shifted.
    [HarmonyPatch]
    public static class CWTLAttackSettlementCanAttack
    {
        public static bool Prepare()
        {
            return CWTLIntegration.Available;
        }

        public static MethodBase TargetMethod()
        {
            return CWTLIntegration.CanAttackMethod;
        }

        [HarmonyPostfix]
        public static void Postfix(ref FloatMenuAcceptanceReport __result, Settlement settlement)
        {
            // Only convert an acceptance into a reasoned rejection. A silent rejection must stay
            // silent, or the attack option would appear where CWTL/vanilla showed none.
            if (!__result.Accepted)
                return;

            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            __result = FloatMenuAcceptanceReport.WithFailReason("BTG_RequiresSignalJammerReason".Translate());
        }
    }
}
