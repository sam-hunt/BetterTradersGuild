using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    // Harmony patch: THE KEY PATCH for shuttle destination targeting!
    // Settlement.GetShuttleFloatMenuOptions is called when clicking on settlements during shuttle launch targeting
    // This handles the float menu that appears when targeting a settlement with shuttles
    [HarmonyPatch(typeof(RimWorld.Planet.Settlement), nameof(RimWorld.Planet.Settlement.GetShuttleFloatMenuOptions))]
    public static class SettlementGetShuttleFloatMenuOptions
    {
        // Postfix method - modifies shuttle destination menu options for Traders Guild settlements
        // pods = the shuttle contents (IThingHolder)
        // launchAction = the action to execute when launching (used to create TransportersArrivalAction)
        // Priority.Last ensures we wrap ALL other postfixes (e.g., "Choose where to land" mod)
        // so their added attack variants pass through our filter too.
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            RimWorld.Planet.Settlement __instance,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            // Check if this is a Traders Guild settlement
            bool isTradersGuild = TradersGuildHelper.IsTradersGuildSettlement(__instance);
            bool canPeacefullyVisit = isTradersGuild && TradersGuildHelper.CanPeacefullyVisit(__instance.Faction);

            // Track which option types we've seen
            bool hasTradeOption = false;

            foreach (FloatMenuOption option in __result)
            {
                // For non-TradersGuild settlements, return options unchanged
                if (!isTradersGuild)
                {
                    yield return option;
                    continue;
                }

                // ATTACK OPTIONS: Grey out the signal-jammer-blocked ones. Detection keys on our
                // own injected reason string, not the English word "attack", so it survives
                // translation and covers CWTL's attack option (its CanAttack is gated by
                // CWTLAttackSettlementCanAttack) as well as vanilla's. Priority.Last on this postfix
                // ensures CWTL's appended option is already present when we filter.
                if (TradersGuildHelper.IsSignalJammerBlockedAttackOption(option))
                {
                    yield return new FloatMenuOption(option.Label, null); // null action keeps it disabled
                    continue;
                }

                // TRADE OPTIONS: note vanilla already generated one so we don't add a duplicate.
                if (option.Label.ToLower().Contains("trade"))
                    hasTradeOption = true;

                // TRADE and everything else pass through unchanged.
                yield return option;
            }

            // If this is a friendly Traders Guild settlement and no trade option was generated, add one
            if (isTradersGuild && canPeacefullyVisit && !hasTradeOption)
            {
                string tradeLabel = "TradeWithSettlement".Translate(__instance.Label);
                string blockedReason = TradersGuildHelper.GetTradeBlockedReasonFromPods(pods, __instance);

                if (blockedReason != null)
                {
                    // Show disabled option with rejection reason (e.g., title requirement)
                    FloatMenuOption disabledOption = new FloatMenuOption(tradeLabel + " (" + blockedReason + ")", null);
                    yield return disabledOption;
                }
                else
                {
                    FloatMenuOption tradeOption = new FloatMenuOption(
                        tradeLabel,
                        delegate
                        {
                            TransportersArrivalAction_Trade tradeAction =
                                new TransportersArrivalAction_Trade(__instance, "MessageShuttleArrived");
                            launchAction(__instance.Tile, tradeAction);
                        }
                    );

                    yield return tradeOption;
                }
            }
        }
    }
}
