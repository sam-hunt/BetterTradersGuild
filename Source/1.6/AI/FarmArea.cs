using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Shared confinement model for the agrihand mech's greenhouse behaviour.
    //
    // The working area is defined two ways at once, and a cell must satisfy BOTH to be
    // considered -
    //
    //   * a moderate Radius around the mech's anchor point - so it only
    //     ever harvests/sows/hauls "nearby" and stays near home rather than crossing the
    //     whole settlement; and
    //   * the settlement structure footprint (StructureBoundsCache) - so it never scans,
    //     paths to, or considers any cell outside the walls.
    //
    // (The cleansweeper's CleanArea instead confines to exact room rects; the agrihand
    // keeps a radius because the greenhouse is a single known room and the radius cheaply
    // covers its hydroponics clusters from the centre.)
    //
    // The anchor is the mech's duty focus point (the greenhouse centre the lord pinned it
    // to), which is stable as the mech roams; it falls back to the mech's own cell.
    //
    // The agrihand is only ever spawned in the Greenhouse, and only the greenhouse grows
    // food in hydroponics, so radius + structure-bounds keeps it tending its own crops;
    // the harvest giver additionally filters to food plants, so a medical-bay healroot
    // basin that happened to fall inside the radius is never touched.
    internal static class FarmArea
    {
        // Moderate search radius around the anchor point, sized to cover a typical
        // greenhouse (its hydroponics clusters and edge shelves) from the room centre
        // while still keeping the mech bounded near home.
        public const float Radius = 16f;

        // Much tighter radius used for the post-harvest produce pickup, centred on the
        // mech itself rather than the anchor. The harvest toil drops yield onto the
        // mech's own cell (ThingPlaceMode.Near), so the mech is always standing on its
        // fresh produce; a small radius is enough to collect it and keeps the haul both
        // local (it never treks across the room for a stray stack) and cheap (few
        // candidates ever reach the reservation/reachability check).
        public const float HaulPickupRadius = 8f;

        // The point the agrihand is anchored to (its lord's greenhouse centre). Used as
        // the centre of the search radius and the "return home" target for dormancy.
        // Returns IntVec3.Invalid only if the mech has no duty/position.
        public static IntVec3 GetAnchor(Pawn mech)
        {
            PawnDuty duty = mech?.mindState?.duty;
            if (duty != null && duty.focus.IsValid)
                return duty.focus.Cell;
            return mech != null ? mech.Position : IntVec3.Invalid;
        }

        // True when pos is both within Radius of
        // anchor and inside the settlement structure footprint.
        public static bool Contains(Map map, IntVec3 anchor, IntVec3 pos)
        {
            return WithinRadius(map, anchor, pos, Radius);
        }

        // True when pos is both within radius of
        // center and inside the settlement structure footprint. Lets
        // the haul giver re-centre on the mech with a tighter HaulPickupRadius.
        public static bool WithinRadius(Map map, IntVec3 center, IntVec3 pos, float radius)
        {
            if ((pos - center).LengthHorizontalSquared > radius * radius)
                return false;
            return StructureBoundsCache.Contains(map, pos);
        }
    }
}
