using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.WorldComponents;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    /// <summary>
    /// Harmony patch: Settlement.GetInspectString method
    /// Appends the current docked orbital trader type to the inspection string for TradersGuild settlements
    /// </summary>
    /// <remarks>
    /// LEARNING NOTE: GetInspectString() returns the text displayed in the world map inspection panel
    /// when a settlement is selected. This is shown in the bottom-left corner of the screen.
    ///
    /// This patch enhances the player experience by showing which orbital trader is currently
    /// docked at each TradersGuild settlement, allowing players to plan trading expeditions
    /// across the world map instead of always using the comms console.
    /// </remarks>
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetInspectString))]
    public static class SettlementGetInspectString
    {
        /// <summary>
        /// Postfix method - appends orbital trader information for TradersGuild settlements
        /// </summary>
        /// <param name="__instance">The Settlement instance</param>
        /// <param name="__result">The inspection string result (can be modified)</param>
        [HarmonyPostfix]
        public static void Postfix(Settlement __instance, ref string __result)
        {
            // Only add trader info for TradersGuild settlements
            if (!TradersGuildHelper.IsTradersGuildSettlement(__instance))
                return;

            // Get the current trader type (our patch handles this for TradersGuild)
            // LEARNING NOTE: For uninitialized settlements, the TraderKind getter uses a rounded
            // tick value that's stable for ~1 day, preventing flickering while still showing
            // what trader WOULD be docked if the player visited. This encourages exploration!
            TraderKindDef traderKind = __instance.TraderKind;
            if (traderKind == null)
                return;

            // LEARNING NOTE: In RimWorld, inspection strings are built by concatenating
            // segments with newlines. We follow the same pattern here.
            if (!string.IsNullOrEmpty(__result))
            {
                __result += "\n";
            }

            // Build the docked trader message
            // Format: "Docked vessel: Combat supplier"
            // Format (Allied): "Docked vessel: Combat supplier (departs in 3.2 days)"
            // LEARNING NOTE: We use LabelCap to get the properly capitalized label
            // (e.g., "Combat supplier" instead of "combat supplier")
            // This matches how the label appears in the trade dialog

            // ENHANCEMENT: Show rotation timing for allied players when not on map
            // - Allied players get extra information as a reward for good relations
            // - Don't show departure time when player is on the map, as rotation is paused
            //   (our patches block stock regeneration while settlement map is loaded)
            bool showDepartureTime = __instance.Faction.PlayerRelationKind == FactionRelationKind.Ally
                && __instance.Map == null;

            if (showDepartureTime)
            {
                // Get departure time from cached expiration tick
                // IMPORTANT: Use cached expiration, not recalculated virtual schedule
                // Recalculating would cause erratic jumps when rotation interval setting changes
                var worldComponent = TradersGuildWorldComponent.GetComponent();
                int ticksRemaining;

                if (worldComponent != null && worldComponent.TryGetCachedExpirationTick(__instance.ID, out int expirationTick))
                {
                    // Use cached expiration tick - stable even during settings changes
                    ticksRemaining = expirationTick - Find.TickManager.TicksGame;
                }
                else
                {
                    // Fallback to calculated value if no cache exists yet
                    int nextRestockTick = TradersGuildTraderRotation.GetNextRestockTick(__instance.ID);
                    ticksRemaining = nextRestockTick - Find.TickManager.TicksGame;
                }

                // Convert to days (60000 ticks = 1 day)
                float daysRemaining = ticksRemaining / 60000f;

                // Format with one decimal place for readability
                // Example: "Docked vessel: Bulk goods trader (departs in 3.2 days)"
                __result += "BTG_DockedVesselDeparts".Translate(
                    "BTG_DockedVessel".Translate(),
                    traderKind.LabelCap,
                    daysRemaining.ToString("F1"));
            }
            else
            {
                // No timing info when: neutral/hostile, or player is on map
                __result += "BTG_DockedVessel".Translate() + ": " + traderKind.LabelCap;
            }
        }
    }
}
