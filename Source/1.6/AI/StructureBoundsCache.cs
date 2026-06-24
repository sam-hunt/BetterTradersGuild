using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace BetterTradersGuild.AI
{
    /// <summary>
    /// Per-map cache of the union of room rects from a settlement's layout
    /// structure sketch. The rect list is fixed once the map is generated, so
    /// we compute it once on first access and reuse it for the lifetime of
    /// the Map object. Uses ConditionalWeakTable so the cache entry is
    /// collected automatically when the Map itself is.
    ///
    /// Returns null when the map has no layout sketch — callers should treat
    /// that as "no bounds known" and fall back to permissive behavior.
    /// </summary>
    internal static class StructureBoundsCache
    {
        private static readonly ConditionalWeakTable<Map, List<CellRect>> cache = new ConditionalWeakTable<Map, List<CellRect>>();

        public static List<CellRect> GetRoomRects(Map map)
        {
            if (map == null)
                return null;

            if (cache.TryGetValue(map, out List<CellRect> rects))
                return rects.Count == 0 ? null : rects;

            rects = ComputeRoomRects(map);
            cache.Add(map, rects);
            return rects.Count == 0 ? null : rects;
        }

        private static List<CellRect> ComputeRoomRects(Map map)
        {
            var result = new List<CellRect>();
            if (map.layoutStructureSketches == null || map.layoutStructureSketches.Count == 0)
                return result;

            foreach (LayoutStructureSketch sketch in map.layoutStructureSketches)
            {
                if (sketch?.structureLayout?.Rooms == null)
                    continue;

                foreach (LayoutRoom room in sketch.structureLayout.Rooms)
                {
                    if (room?.rects == null)
                        continue;

                    foreach (CellRect rect in room.rects)
                        result.Add(rect);
                }
            }

            return result;
        }
    }
}
