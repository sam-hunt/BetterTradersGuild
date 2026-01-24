using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.Incidents
{
    /// <summary>
    /// Static context holder for raid faction selection.
    /// Set by PawnGroupMakerUtility patch, read by FactionDef patch.
    /// </summary>
    public static class RaidFactionSelectionContext
    {
        public static bool IsOnTradersGuildMap = false;
    }

    /// <summary>
    /// Harmony patch: PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroupWeighted
    /// Sets context flag when raid faction selection occurs on a Traders Guild map.
    /// </summary>
    /// <remarks>
    /// This patch establishes context for the FactionDef.RaidCommonalityFromPoints patch
    /// to know when to boost Salvagers raid weight. The context flag is set in Prefix
    /// and cleared in Finalizer to ensure cleanup even on exceptions.
    /// </remarks>
    [HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroupWeighted))]
    public static class PawnGroupMakerUtilityTryGetRandomFactionForCombatPawnGroupWeighted
    {
        [HarmonyPrefix]
        public static void Prefix(IncidentParms parms)
        {
            if (!BetterTradersGuildMod.Settings.useCustomLayouts)
                return;

            if (parms?.target is Map map && TradersGuildHelper.IsMapInTradersGuildSettlement(map))
            {
                RaidFactionSelectionContext.IsOnTradersGuildMap = true;
            }
        }

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            RaidFactionSelectionContext.IsOnTradersGuildMap = false;
        }
    }
}
