using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    // Harmony patch: Grey out signal-jammer-blocked attack options on the transport-pod (drop-pod)
    // launch flow for Traders Guild settlements.
    //
    // The transport-pod counterpart to SettlementGetShuttleFloatMenuOptions. Settlement.
    // GetTransportersFloatMenuOptions yields the vanilla attack variants ("Attack X: Drop at edge /
    // in center"), and "Choose where to land" appends its own ("Attack X (Specify a landing spot)")
    // via its postfix on the same method. Both sets are structurally blocked at CanAttack (vanilla
    // by TransportersArrivalActionAttackSettlementCanAttack, CWTL by CWTLAttackSettlementCanAttack),
    // which makes vanilla's float-menu builder render each ENABLED with the reason appended and a
    // click-time re-check that silently no-ops. Re-yield the blocked ones disabled so they grey out.
    //
    // Detection keys on our own injected reason string rather than the English word "attack", so it
    // survives translation and never touches the legitimate trade/visit/gift/form-caravan options.
    // Priority.Last ensures CWTL's appended option is already present when we filter.
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetTransportersFloatMenuOptions))]
    public static class SettlementGetTransportersFloatMenuOptions
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result, Settlement __instance)
        {
            bool isTradersGuild = TradersGuildHelper.IsTradersGuildSettlement(__instance);

            foreach (FloatMenuOption option in __result)
            {
                if (isTradersGuild && TradersGuildHelper.IsSignalJammerBlockedAttackOption(option))
                {
                    yield return new FloatMenuOption(option.Label, null); // null action keeps it disabled
                    continue;
                }

                yield return option;
            }
        }
    }
}
