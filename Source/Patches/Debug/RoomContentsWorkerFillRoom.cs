using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Patches.Debug
{
    /// <summary>
    /// DEBUG: Logs detailed information when RoomContentsWorker fills rooms to diagnose prefab spawning issues.
    /// </summary>
    [HarmonyPatch(typeof(RoomContentsWorker), nameof(RoomContentsWorker.FillRoom))]
    public static class RoomContentsWorkerFillRoom
    {
        [HarmonyPrefix]
        public static void Prefix(Map map, LayoutRoom room, Faction faction)
        {
            if (room?.defs == null || room.defs.Count == 0)
                return;

            // Get the first (or required) def
            var roomDef = room.requiredDef ?? room.defs.FirstOrDefault();
            if (roomDef == null || !roomDef.defName.Contains("BTG_"))
                return;

            Log.Message($"[BTG DEBUG] FillRoom called for: {roomDef.defName}");
            Log.Message($"[BTG DEBUG]   Room size: {(room.rects?.FirstOrDefault().ToString() ?? "null")}");
            Log.Message($"[BTG DEBUG]   Room def prefabs count: {roomDef.prefabs?.Count ?? 0}");

            if (roomDef.prefabs != null)
            {
                foreach (var prefabEntry in roomDef.prefabs)
                {
                    var prefabDefName = prefabEntry.def?.defName ?? "null";
                    var countRange = prefabEntry.countRange;
                    Log.Message($"[BTG DEBUG]     Prefab entry: {prefabDefName} (count: {countRange.min}-{countRange.max})");

                    // Check if prefab def exists
                    if (prefabEntry.def == null)
                    {
                        Log.Error($"[BTG DEBUG] ERROR: Prefab def is NULL! Room: {roomDef.defName}");
                    }
                    else
                    {
                        var prefabDef = prefabEntry.def;
                        Log.Message($"[BTG DEBUG]       PrefabDef loaded: Size={prefabDef.size}, EdgeOnly={prefabDef.edgeOnly}, Rotations={prefabDef.rotations}");
                    }
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Map map, LayoutRoom room)
        {
            if (room?.defs == null || room.defs.Count == 0)
                return;

            var roomDef = room.requiredDef ?? room.defs.FirstOrDefault();
            if (roomDef == null || !roomDef.defName.Contains("BTG_"))
                return;

            Log.Message($"[BTG DEBUG] FillRoom completed for: {roomDef.defName}");

            // Count things spawned in the room
            if (room.rects != null && room.rects.Count > 0)
            {
                int wallCount = 0;
                int doorCount = 0;
                int furnitureCount = 0;

                foreach (var rect in room.rects)
                {
                    foreach (var cell in rect.Cells)
                    {
                        if (!cell.InBounds(map))
                            continue;

                        var things = cell.GetThingList(map);
                        foreach (var thing in things)
                        {
                            if (thing.def.IsWall)  // Property, not method
                                wallCount++;
                            else if (thing.def.IsDoor)
                                doorCount++;
                            else if (thing.def.category == ThingCategory.Building)
                                furnitureCount++;
                        }
                    }
                }

                Log.Message($"[BTG DEBUG]   Spawned: {wallCount} walls, {doorCount} doors, {furnitureCount} buildings");
            }
        }
    }
}
