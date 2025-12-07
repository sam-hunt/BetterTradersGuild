using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.WorldObjectPatches
{
    /// <summary>
    /// Harmony patch: Override RequiresSignalJammerToReach for Traders Guild settlements
    /// Allows transport pods (e.g., gifts) and shuttles to reach TradersGuild regardless of hostility
    /// Context-aware: Preserves vanilla gravship signal jammer requirement for attacks
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.Planet.WorldObject), nameof(RimWorld.Planet.WorldObject.RequiresSignalJammerToReach), MethodType.Getter)]
    public static class WorldObjectRequiresSignalJammer
    {
        /// <summary>
        /// Postfix for WorldObject.RequiresSignalJammerToReach property
        /// Context-aware: Preserves signal jammer requirement for gravship targeting
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(RimWorld.Planet.WorldObject __instance, ref bool __result)
        {
            // If no signal jammer is required, nothing to override
            if (!__result)
                return;

            // Check if this is a Settlement
            Settlement settlement = __instance as Settlement;
            if (settlement == null)
                return;

            // Double check it belongs to the Trader's guild in case other mods add factions
            // that inhabit the orbital layer but expect vanilla behavior
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            // GRAVSHIP CONTEXT DETECTION:
            // If gravship targeting is active, DON'T override the signal jammer requirement
            // This preserves vanilla behavior where gravships check engine.HasSignalJammer
            TilePicker tilePicker = Find.TilePicker;
            if (tilePicker != null)
            {
                // Access private fields using Harmony's Traverse
                bool isActive = HarmonyLib.Traverse.Create(tilePicker).Field("active").GetValue<bool>();
                bool isForGravship = HarmonyLib.Traverse.Create(tilePicker).Field("forGravship").GetValue<bool>();

                if (isActive && isForGravship)
                {
                    // Gravship targeting active - preserve signal jammer requirement
                    // The gravship's own validator will check engine.HasSignalJammer
                    return; // Keep __result = true (requires jammer)
                }
            }

            // Not gravship context (shuttles/caravans/pods) - override!
            // Transport pods are too small for heavy weapons platforms to target
            // This preserves vanilla behavior where you can send gift pods to repair relations
            // We handle targeting for shuttle raids via SettlementGetShuttleFloatMenuOptions
            // Note: Peaceful trading visits still require non-hostile relations (SettlementVisitable patch)
            __result = false;
        }
    }
}
