using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.CaravanArrivalActions
{
    // Harmony patch: Modifies attack float menu options to show "(requires signal jammer)" for Traders Guild
    [HarmonyPatch(typeof(CaravanArrivalAction_AttackSettlement), nameof(CaravanArrivalAction_AttackSettlement.GetFloatMenuOptions))]
    public static class CaravanArrivalActionAttackGetFloatMenuOptions
    {
        [HarmonyPostfix]
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Caravan caravan, Settlement settlement)
        {
            // Check if this is a Traders Guild settlement
            bool isTradersGuild = TradersGuildHelper.IsTradersGuildSettlement(settlement);

            foreach (FloatMenuOption option in __result)
            {
                // If not Traders Guild, return option unchanged
                if (!isTradersGuild)
                {
                    yield return option;
                    continue;
                }

                // For Traders Guild, all attack options become disabled with the signal jammer
                // requirement shown. CanAttack is already rejected by
                // CaravanArrivalActionAttackSettlementCanAttack, which appends the paren-free
                // reason to vanilla's label - only append here when the tag is missing
                // (e.g. options added by other mods' postfixes).
                string jammerReason = "BTG_RequiresSignalJammerReason".Translate();
                string label = option.Label.Contains(jammerReason)
                    ? option.Label
                    : option.Label + " " + "BTG_RequiresSignalJammer".Translate();

                yield return new FloatMenuOption(label, null); // null action keeps it disabled
            }
        }
    }
}
