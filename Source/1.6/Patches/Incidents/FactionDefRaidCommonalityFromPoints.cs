using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;

namespace BetterTradersGuild.Patches.Incidents
{
    // Harmony patch: FactionDef.RaidCommonalityFromPoints
    // Increases Salvagers raid weight when on a Traders Guild map.
    // When the player assaults a Traders Guild settlement, this makes Salvagers
    // more likely to appear as raiders. The multiplier is configurable via mod settings.
    // This creates emergent gameplay where attacking the guild attracts opportunistic pirates.
    //
    // Context is provided by RaidFactionSelectionContext, which is set by the
    // PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroupWeighted patch.
    [HarmonyPatch(typeof(FactionDef), nameof(FactionDef.RaidCommonalityFromPoints))]
    public static class FactionDefRaidCommonalityFromPoints
    {
        [HarmonyPostfix]
        public static void Postfix(FactionDef __instance, ref float __result)
        {
            if (!RaidFactionSelectionContext.IsOnTradersGuildMap)
                return;

            if (__instance != Factions.Salvagers)
                return;

            float multiplier = BetterTradersGuildMod.Settings.salvagersRaidWeightMultiplier;
            if (multiplier != 1.0f)
            {
                __result *= multiplier;
            }
        }
    }
}
