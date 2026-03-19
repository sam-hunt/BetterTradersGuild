using System.Collections.Generic;
using System.Reflection.Emit;
using BetterTradersGuild.LayoutWorkers.Settlement;
using HarmonyLib;
using Verse;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Downgrades the "Layout failed to spawn all required rooms" error to a warning
    /// during BTG settlement generation.
    ///
    /// WHY:
    /// The vanilla LayoutWorker.ResolveRoomDefs() fires Log.ErrorOnce when it can't
    /// place all required rooms (e.g., a third BTG_CrewQuarters). This pops the dev
    /// console in dev mode. The settlement still generates fine with fewer rooms,
    /// so a warning is the appropriate severity.
    ///
    /// HOW:
    /// Two coordinating patches:
    /// 1. GenerateStructureSketch Prefix/Finalizer sets LayoutWorker_Settlement.IsGenerating
    ///    flag when the instance is our subclass (ResolveRoomDefs runs inside this method,
    ///    BEFORE Spawn is called).
    /// 2. ResolveRoomDefs Transpiler swaps Log.ErrorOnce for a helper that checks the flag
    ///    and downgrades to Log.Warning during BTG generation.
    /// </summary>
    [HarmonyPatch(typeof(LayoutWorker), "GenerateStructureSketch")]
    public static class LayoutWorkerGenerateStructureSketch
    {
        [HarmonyPrefix]
        public static void Prefix(LayoutWorker __instance)
        {
            if (__instance is LayoutWorker_Settlement)
                LayoutWorker_Settlement.IsGenerating = true;
        }

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            LayoutWorker_Settlement.IsGenerating = false;
        }
    }

    [HarmonyPatch(typeof(LayoutWorker), "ResolveRoomDefs")]
    public static class LayoutWorkerResolveRoomDefs
    {
        /// <summary>
        /// Replacement for Log.ErrorOnce inside ResolveRoomDefs.
        /// During BTG generation, downgrades to Log.Warning. Otherwise calls original.
        /// </summary>
        public static void LogRoomPlacementResult(string text, int hash)
        {
            if (LayoutWorker_Settlement.IsGenerating)
                Log.Warning("[BTG] " + text);
            else
                Log.ErrorOnce(text, hash);
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(Log), nameof(Log.ErrorOnce),
                new[] { typeof(string), typeof(int) });
            var replacement = AccessTools.Method(typeof(LayoutWorkerResolveRoomDefs),
                nameof(LogRoomPlacementResult));

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                else
                    yield return instruction;
            }
        }
    }
}
