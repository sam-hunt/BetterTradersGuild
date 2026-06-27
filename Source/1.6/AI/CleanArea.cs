using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    /// <summary>
    /// Shared confinement model for the cleansweeper mech's janitor behaviour.
    ///
    /// Unlike the paramedic (confined to a single named MedicalBay room), the
    /// cleansweeper is spawned in several different room types (MessHall, RecRoom,
    /// Storeroom, CommandersQuarters), so "the room" is not a single LayoutRoomDef we
    /// can resolve. Instead the cleaning area is defined two ways at once:
    ///
    ///   * a moderate <see cref="Radius"/> around the mech's anchor point - so it only
    ///     ever works "nearby" filth and stays near home rather than chasing a filth
    ///     trail across the whole settlement; and
    ///   * the settlement structure footprint (StructureBoundsCache) - so it never
    ///     scans, paths to, or considers any cell outside the walls.
    ///
    /// The anchor is the mech's duty focus point (the room centre the lord pinned it
    /// to), which is stable as the mech roams; it falls back to the mech's own cell.
    /// </summary>
    internal static class CleanArea
    {
        /// <summary>
        /// Moderate filth-search radius around the anchor point. Filth beyond this is
        /// ignored, keeping the cleansweeper bounded near its home point even when the
        /// structure (and its filth) is much larger.
        /// </summary>
        public const float Radius = 12f;

        /// <summary>
        /// The point the cleansweeper is anchored to (its lord's room centre). Used as
        /// the centre of the filth radius and the "return home" target for dormancy.
        /// Returns <see cref="IntVec3.Invalid"/> only if the mech has no duty/position.
        /// </summary>
        public static IntVec3 GetAnchor(Pawn mech)
        {
            PawnDuty duty = mech?.mindState?.duty;
            if (duty != null && duty.focus.IsValid)
                return duty.focus.Cell;
            return mech != null ? mech.Position : IntVec3.Invalid;
        }
    }
}
