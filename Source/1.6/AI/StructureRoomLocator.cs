using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.AI
{
    // Locates specific named rooms inside a settlement's layout structure sketch,
    // reusing the same persisted sketch traversal as StructureBoundsCache. No extra
    // scribe state is needed: LayoutRoom.rects (and its def list) already survive
    // save/load, so the room can be re-found on demand rather than recorded.
    //
    // Used by the comms-console resupply (JobGiver_BTGCallResupply) to resolve the
    // drop room from a priority list of LayoutRoomDefs.
    internal static class StructureRoomLocator
    {
        // Yields every LayoutRoom on map that satisfies
        // def — either its single requiredDef or any entry in its
        // defs list (a room may carry several). Empty when the map has no layout sketch
        // or nothing matches.
        public static IEnumerable<LayoutRoom> RoomsOfDef(Map map, LayoutRoomDef def)
        {
            if (map?.layoutStructureSketches == null || def == null)
                yield break;

            foreach (LayoutStructureSketch sketch in map.layoutStructureSketches)
            {
                if (sketch?.structureLayout?.Rooms == null)
                    continue;

                foreach (LayoutRoom room in sketch.structureLayout.Rooms)
                {
                    if (room?.rects == null)
                        continue;
                    if (room.requiredDef == def || room.HasLayoutDef(def))
                        yield return room;
                }
            }
        }
    }
}
