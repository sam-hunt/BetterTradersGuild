using System;
using System.Reflection;
using BetterTradersGuild.Integrations;
using BetterTradersGuild.LayoutWorkers.Settlement;
using HarmonyLib;
using Verse;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    // Prevents HAR's Comp_OutfitStandHAR.PostSpawnSetup from crashing outfit stand
    // spawns during BTG settlement generation.
    //
    // WHY:
    // HAR's PostSpawnSetup accesses faction.def.basicMemberKind.race without null checks.
    // TradersGuild has no basicMemberKind → NullReferenceException → outfit stand fails
    // to spawn entirely, and the exception propagates up through FillAllRooms.
    //
    // HOW:
    // Finalizer catches the exception during BTG generation (IsGenerating flag) and returns
    // null to swallow it. The stand completes SpawnSetup with HAR in a partially-initialized
    // state. OutfitStandHarFixer then normalizes the stand before apparel is added.
    //
    // CONDITIONAL: This patch only applies if HAR is loaded (Prepare returns false otherwise).
    [HarmonyPatch]
    public static class CompOutfitStandHARPostSpawnSetup
    {
        public static bool Prepare()
        {
            return HARIntegration.CompType != null;
        }

        public static MethodBase TargetMethod()
        {
            return HARIntegration.PostSpawnSetupMethod;
        }

        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null && LayoutWorker_Settlement.IsGenerating)
            {
                Log.Warning("[Better Traders Guild] Suppressed HAR outfit stand error " +
                            $"during settlement generation: {__exception.Message}");
                return null;
            }

            return __exception;
        }
    }
}
