using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Confinement model for the agrihand mech's greenhouse behaviour: the mech works
    // strictly inside the one layout room it was spawned into (the greenhouse), treating
    // that room as its work area. It never scans, reserves, or paths to anything in another
    // room or outside the walls - the same model the cleansweeper (CleanArea) and paramedic
    // (MedicRoomBounds) use.
    //
    // Confinement is rect membership (CellRect.Contains), never a radius, so a room composed
    // of several rects is handled exactly and a large greenhouse is covered to its corners.
    // The room is matched def-agnostically: whichever layout room contains the mech's anchor
    // (StructureRoomLocator.RoomContaining). The agrihand is only ever spawned in the
    // Greenhouse, and only the greenhouse grows food in hydroponics, so this keeps it
    // tending its own crops; the harvest/haul givers additionally filter to food plants, so
    // a medical-bay healroot basin is never touched even if it shared the room.
    //
    // The rects are re-found on demand from the persisted layout sketch (LayoutRoom.rects
    // survives save/load), so no extra scribe state is needed. The anchor is the mech's duty
    // focus point (the room centre the lord pinned it to), which is stable as the mech roams;
    // it falls back to the mech's own cell.
    internal static class FarmArea
    {
        // The point the agrihand is anchored to (its lord's greenhouse centre). Used to
        // resolve the room and as the "return home" target for dormancy. Returns
        // IntVec3.Invalid only if the mech has no duty/position.
        public static IntVec3 GetAnchor(Pawn mech)
        {
            PawnDuty duty = mech?.mindState?.duty;
            if (duty != null && duty.focus.IsValid)
                return duty.focus.Cell;
            return mech != null ? mech.Position : IntVec3.Invalid;
        }

        // The rect list of the layout room (the greenhouse) this mech is anchored in, or
        // null when no layout room can be matched (caller should then do nothing).
        public static List<CellRect> GetRects(Pawn mech)
        {
            Map map = mech?.Map;
            if (map == null)
                return null;

            return StructureRoomLocator.RoomContaining(map, GetAnchor(mech))?.rects
                ?? StructureRoomLocator.RoomContaining(map, mech.Position)?.rects;
        }

        // True when cell lies in any of the mech's room rects.
        public static bool Contains(List<CellRect> rects, IntVec3 cell)
        {
            if (rects == null)
                return false;
            for (int i = 0; i < rects.Count; i++)
            {
                if (rects[i].Contains(cell))
                    return true;
            }
            return false;
        }
    }
}
