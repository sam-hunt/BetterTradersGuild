using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.TransportersArrivalActionPatches
{
    // Harmony patch: Structurally block shuttle/pod attacks on Traders Guild settlements.
    //
    // BTG's WorldObjectRequiresSignalJammer patch lets shuttles and pods REACH TG settlements
    // (for trade and gifts), which also removes the vanilla signal-jammer gate that would have
    // blocked shuttle attacks. Attacks must stay gravship-with-jammer only, so this rejects
    // CanAttack at the source. That covers every consumer language-independently: the shuttle
    // float menu (both the drop-attack options and the transport-ship boarding attack), and
    // StillValid on any already-launched attack action.
    //
    // The label-matching filters in SettlementGetShuttleFloatMenuOptions remain as a cosmetic
    // layer (they gray out attack options added by other mods' postfixes), but correctness no
    // longer depends on English label text.
    [HarmonyPatch(typeof(TransportersArrivalAction_AttackSettlement),
        nameof(TransportersArrivalAction_AttackSettlement.CanAttack))]
    public static class TransportersArrivalActionAttackSettlementCanAttack
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
