using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.AI
{
    // Resolves the MedicalBay footprint that a paramedic mech belongs to, so the
    // medic job-givers can confine every action (target search, bed search, medicine
    // search, dormancy spot) to that one room - regardless of its size or shape.
    //
    // Confinement is rect membership (CellRect.Contains), never a radius, so an
    // arbitrarily large or multi-rect medbay is handled exactly. The rects are
    // re-found on demand from the persisted layout sketch via StructureRoomLocator
    // (LayoutRoom.rects survives save/load), so no extra scribe state is needed -
    // the same approach JobGiver_BTGCallResupply uses for the drop room.
    //
    // The anchor is the mech's duty focus point (the room centre the lord pinned it
    // to), which never moves while the mech walks around tending; it falls back to
    // the mech's own cell. Medic job-givers re-evaluate only every ~211 ticks
    // (dormant LayDown re-check) or per tend/rescue job, so the short sketch
    // traversal is never on a hot path.
    internal static class MedicRoomBounds
    {
        // The rect list of the medbay this mech is anchored in, or null when no
        // MedicalBay layout room can be matched (caller should then do nothing).
        public static List<CellRect> GetRects(Pawn mech)
        {
            Map map = mech?.Map;
            if (map == null)
                return null;

            IntVec3 anchor = mech.mindState?.duty != null && mech.mindState.duty.focus.IsValid
                ? mech.mindState.duty.focus.Cell
                : mech.Position;

            return RectsContaining(map, anchor) ?? RectsContaining(map, mech.Position);
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
            foreach (LayoutRoom room in StructureRoomLocator.RoomsOfDef(map, LayoutRooms.BTG_MedicalBay))
            {
                if (Contains(room.rects, cell))
                    return room.rects;
            }
            return null;
        }
    }
}
