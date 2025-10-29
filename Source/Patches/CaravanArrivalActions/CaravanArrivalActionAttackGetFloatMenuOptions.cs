using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.CaravanArrivalActions
{
    /// <summary>
    /// Harmony patch: Modifies attack float menu options to show "(requires signal jammer)" for Traders Guild
    /// </summary>
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

                // For Traders Guild, we always want to add signal jammer message to attack options
                // The attack option might be disabled through various mechanisms (null action, Disabled flag, etc.)
                // So we'll modify ALL attack options for Traders Guild to show the signal jammer requirement

                // Modify the label to include signal jammer requirement
                // Create a new disabled option with modified label
                FloatMenuOption modifiedOption = new FloatMenuOption(
                    option.Label + " (requires signal jammer)",
                    null  // null action keeps it disabled
                );

                yield return modifiedOption;
            }
        }
    }
}
