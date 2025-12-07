using HarmonyLib;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Allows peaceful visits to Traders Guild settlements even with signal jammers
    /// Signal jammers should only block hostile actions (attacks), not peaceful trading
    /// This is a Postfix patch that overrides the result if it's a Traders Guild settlement
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.Settlement), nameof(RimWorld.Planet.Settlement.Visitable), MethodType.Getter)]
    public static class SettlementVisitable
    {
        /// <summary>
        /// Postfix for Settlement.Visitable property
        /// __instance = the Settlement being checked
        /// __result = the original return value (can be modified)
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(RimWorld.Planet.Settlement __instance, ref bool __result)
        {
            // If the settlement is already marked as visitable, no need to change anything
            if (__result)
                return;

            // Check if this is a Traders Guild settlement
            if (!TradersGuildHelper.IsTradersGuildSettlement(__instance))
                return;

            // Check if the player has good relations (non-hostile)
            if (!TradersGuildHelper.CanPeacefullyVisit(__instance.Faction))
                return;

            // For Traders Guild with good relations, they should always be visitable
            // regardless of signal jammer or space location
            __result = true;

            // LEARNING NOTE:
            // This is a key Harmony pattern - a Postfix patch can modify the return value
            // by changing the __result parameter (passed by reference with 'ref')
            // This lets us change the game's behavior without replacing the entire method
        }
    }
}
