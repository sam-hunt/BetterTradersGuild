using System;
using BetterTradersGuild.LayoutWorkers.Settlement;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Per-room error containment during BTG settlement generation.
    ///
    /// WHY:
    /// LayoutRoomDef.ResolveContents is called for each room during FillAllRooms.
    /// If any room's RoomContentsWorker throws (e.g., a third-party mod's ThingComp
    /// crashes during thing spawning), the unhandled exception kills FillAllRooms
    /// and all subsequent rooms get no contents.
    ///
    /// HOW:
    /// Finalizer catches exceptions during BTG generation (LayoutWorker_Settlement.IsGenerating),
    /// logs the error with the room def name, and returns null to swallow the exception.
    /// The remaining rooms continue to generate normally.
    /// </summary>
    [HarmonyPatch(typeof(LayoutRoomDef), nameof(LayoutRoomDef.ResolveContents))]
    public static class LayoutRoomDefResolveContents
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception, LayoutRoomDef __instance)
        {
            if (__exception != null && LayoutWorker_Settlement.IsGenerating)
            {
                Log.Error($"[Better Traders Guild] Error filling room '{__instance?.defName}' " +
                          $"(skipping, other rooms unaffected): {__exception}");
                return null;
            }

            return __exception;
        }
    }
}
