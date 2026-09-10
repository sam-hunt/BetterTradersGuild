using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    // Walks the rooms the platform step built (map.layoutStructureSketches) and hands each
    // one's XML rule extension to a GenStep, so room identity can live on the LayoutRoomDef
    // instead of in RoomContentsWorker code. Shared by the sowing and outfit-stand steps.
    // Prefab-built stations (VGE2) have no sketches, so the walk is a no-op there and the
    // steps fall through to their map-wide defaults.
    public static class BuiltRoomRules
    {
        // Calls apply(room, extensions, label) for every built room whose defs carry at least
        // one extension of type T (in def order); label is the first such def's defName.
        public static void ForEach<T>(Map map, Action<LayoutRoom, List<T>, string> apply) where T : DefModExtension
        {
            if (map?.layoutStructureSketches == null)
                return;

            var found = new List<T>();
            foreach (LayoutStructureSketch sketch in map.layoutStructureSketches)
            {
                if (sketch?.structureLayout?.Rooms == null)
                    continue;

                foreach (LayoutRoom room in sketch.structureLayout.Rooms)
                {
                    if (room?.rects == null || room.defs == null)
                        continue;

                    found.Clear();
                    string label = null;
                    foreach (LayoutRoomDef roomDef in room.defs)
                    {
                        T ext = roomDef?.GetModExtension<T>();
                        if (ext == null)
                            continue;
                        found.Add(ext);
                        label ??= roomDef.defName;
                    }
                    if (found.Count == 0)
                        continue;

                    apply(room, found, label);
                }
            }
        }

        // The candidates whose position lies inside one of the room's rects.
        public static List<TThing> Inside<TThing>(LayoutRoom room, List<TThing> candidates) where TThing : Thing
        {
            var inside = new List<TThing>();
            foreach (TThing thing in candidates)
            {
                if (Contains(room.rects, thing.Position))
                    inside.Add(thing);
            }
            return inside;
        }

        private static bool Contains(List<CellRect> rects, IntVec3 cell)
        {
            foreach (CellRect rect in rects)
            {
                if (rect.Contains(cell))
                    return true;
            }
            return false;
        }
    }
}
