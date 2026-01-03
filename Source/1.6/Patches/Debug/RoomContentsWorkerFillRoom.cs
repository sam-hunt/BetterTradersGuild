using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.DebugPatches
{
    /// <summary>
    /// Debug Harmony patch for RoomContentsWorker.FillRoom() to track room generation.
    ///
    /// PURPOSE:
    /// Logs all room content worker invocations during map generation to help diagnose
    /// empty room issues. Tracks counts by room type and shows total summary.
    ///
    /// USAGE:
    /// Enable this patch by keeping it in the codebase. Check Player.log for:
    /// - "[BTG Debug] FillRoom START:" messages for each room
    /// - "[BTG Debug] FillRoom COMPLETE:" messages when rooms finish
    /// - Summary counts at the end showing all room types generated
    ///
    /// DISABLE:
    /// To disable debug logging, comment out the [HarmonyPatch] attribute or
    /// exclude this file from compilation.
    /// </summary>
    [HarmonyPatch(typeof(RoomContentsWorker))]
    [HarmonyPatch("FillRoom")]
    public static class RoomContentsWorkerFillRoom
    {
        /// <summary>
        /// Tracks room type counts during generation.
        /// Key: Worker type name (e.g., "RoomContents_CrewQuarters")
        /// Value: Count of rooms generated
        /// </summary>
        private static Dictionary<string, int> roomTypeCounts = new Dictionary<string, int>();

        /// <summary>
        /// Total rooms processed in current generation session.
        /// </summary>
        private static int totalRoomsProcessed = 0;

        /// <summary>
        /// Last map ID processed (to detect new generation sessions).
        /// </summary>
        private static int lastMapId = -1;

        /// <summary>
        /// Prefix patch that logs when FillRoom starts for each room.
        /// Resets counts when a new map generation begins.
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(RoomContentsWorker __instance, Map map, LayoutRoom room)
        {
            // Detect new map generation session
            if (map != null && map.uniqueID != lastMapId)
            {
                // Log summary from previous session (if any)
                if (totalRoomsProcessed > 0)
                {
                    LogSummary();
                }

                // Reset for new session
                roomTypeCounts.Clear();
                totalRoomsProcessed = 0;
                lastMapId = map.uniqueID;
                Log.Message($"[BTG Debug] === NEW MAP GENERATION (Map ID: {map.uniqueID}) ===");
            }

            // Get worker type name
            string workerTypeName = __instance?.GetType().Name ?? "Unknown";

            // Get room rect info
            string rectInfo = "NoRect";
            if (room?.rects != null && room.rects.Count > 0)
            {
                var rect = room.rects[0];
                rectInfo = $"{rect.Width}x{rect.Height} at ({rect.minX},{rect.minZ})";
            }

            // Increment count
            if (!roomTypeCounts.ContainsKey(workerTypeName))
            {
                roomTypeCounts[workerTypeName] = 0;
            }
            roomTypeCounts[workerTypeName]++;
            totalRoomsProcessed++;

            Log.Message($"[BTG Debug] FillRoom START #{totalRoomsProcessed}: " +
                        $"Worker={workerTypeName}, Rect={rectInfo}");
        }

        /// <summary>
        /// Postfix patch that logs when FillRoom completes.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(RoomContentsWorker __instance)
        {
            string workerTypeName = __instance?.GetType().Name ?? "Unknown";

            Log.Message($"[BTG Debug] FillRoom COMPLETE: Worker={workerTypeName}");
        }

        /// <summary>
        /// Logs summary of all room types processed.
        /// Called when a new map generation session starts (to show previous session's summary).
        /// Can also be called manually via dev console if needed.
        /// </summary>
        public static void LogSummary()
        {
            Log.Message($"[BTG Debug] === ROOM GENERATION SUMMARY ===");
            Log.Message($"[BTG Debug] Total rooms processed: {totalRoomsProcessed}");

            foreach (var kvp in roomTypeCounts)
            {
                Log.Message($"[BTG Debug]   {kvp.Key}: {kvp.Value}");
            }

            Log.Message($"[BTG Debug] === END SUMMARY ===");
        }
    }
}
