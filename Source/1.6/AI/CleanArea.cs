using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Confinement model for the cleansweeper mech's janitor behaviour: the mech operates
    // strictly inside the one layout room it was spawned into, treating that room as its
    // private "home area" (the role the player's painted Home area plays for a player
    // cleansweeper - see JobDriver_BTGCleanFilth).
    //
    // Unlike the paramedic (which resolves a single named MedicalBay via MedicRoomBounds),
    // the cleansweeper spawns in several different room types (MessHall, RecRoom, Storeroom,
    // CommandersQuarters), so the room is matched def-agnostically: whichever layout room
    // contains the mech's anchor (see StructureRoomLocator.RoomContaining). Confinement is
    // rect membership (CellRect.Contains), never a radius, so a room composed of several
    // rects is handled exactly. The rects are re-found on demand from the persisted layout
    // sketch (LayoutRoom.rects survives save/load), so no extra scribe state is needed -
    // the same approach MedicRoomBounds uses.
    //
    // The anchor is the mech's duty focus point (the room centre the lord pinned it to),
    // which is stable as the mech roams; it falls back to the mech's own cell. The clean
    // and standby givers re-evaluate only per clean job or every ~250 idle ticks, so the
    // short sketch traversal is never on a hot path.
    internal static class CleanArea
    {
        // The point the cleansweeper is anchored to (its lord's room centre). Used to
        // resolve the room and as the "return home" target for dormancy. Returns
        // IntVec3.Invalid only if the mech has no duty/position.
        public static IntVec3 GetAnchor(Pawn mech)
        {
            PawnDuty duty = mech?.mindState?.duty;
            if (duty != null && duty.focus.IsValid)
                return duty.focus.Cell;
            return mech != null ? mech.Position : IntVec3.Invalid;
        }

        // The rect list of the layout room this mech is anchored in (multi-rect aware), or
        // null when no layout room can be matched (caller should then do nothing).
        public static List<CellRect> GetRects(Pawn mech)
        {
            Map map = mech?.Map;
            if (map == null)
                return null;

            return RectsContaining(map, GetAnchor(mech)) ?? RectsContaining(map, mech.Position);
        }

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

        private static List<CellRect> RectsContaining(Map map, IntVec3 cell)
        {
            return StructureRoomLocator.RoomContaining(map, cell)?.rects;
        }
    }
}
