using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.AI
{
    // Picks where a work mech should wind down when it goes idle. Left to itself a mech
    // self-shuts-down at the point its lord pinned it to - the room centre - so it ends up
    // standing in the middle of the floor, in the way. This nudges it to a low-effort
    // parking spot instead: the cell nearest to where it is already standing that sits
    // against a wall and clear of any doorway.
    //
    // The chosen cell is only a *search root*: the standby givers hand it to
    // RCellFinder.TryFindNearbyMechSelfShutdownSpot, which keeps the cell when it is a legal
    // shutdown spot (it also checks reachable / reservable / not forbidden-or-dangerous) and
    // otherwise snaps to the closest one - so the mech parks against the wall in the common
    // case, and the giver's own room/bounds check still vetoes anything that lands outside.
    //
    // Only the rect the mech is currently standing in is searched - not the whole room or
    // its largest rect - so it tucks into the nearest wall rather than trekking across a
    // multi-rect room. When that rect has no wall spot (or the mech is somehow outside its
    // rects) the supplied fallback is returned and the vanilla finder just parks it nearby,
    // wall or not.
    internal static class MechIdlePark
    {
        public static IntVec3 RootFor(Pawn mech, List<CellRect> rects, IntVec3 fallback)
        {
            Map map = mech?.Map;
            if (map == null || rects == null)
                return fallback;

            IntVec3 from = mech.Position;
            if (!TryRectContaining(rects, from, out CellRect rect))
                return fallback;

            IntVec3 best = IntVec3.Invalid;
            int bestDist = int.MaxValue;
            for (int z = rect.minZ; z <= rect.maxZ; z++)
            {
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    IntVec3 c = new IntVec3(x, 0, z);
                    if (!IsWallParkSpot(c, map))
                        continue;

                    int dist = (c - from).LengthHorizontalSquared;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = c;
                    }
                }
            }

            return best.IsValid ? best : fallback;
        }

        // A cell worth parking on: standable, not already taken by a building, sat against a
        // wall (or the map edge) on at least one side, and not flanking a door. Reachability
        // and reservation are deliberately left to the vanilla shutdown-spot finder this
        // feeds, so the per-cell test stays cheap.
        private static bool IsWallParkSpot(IntVec3 c, Map map)
        {
            if (!c.Standable(map) || c.GetFirstBuilding(map) != null)
                return false;

            bool againstWall = false;
            IntVec3[] dirs = GenAdj.CardinalDirections;
            for (int i = 0; i < dirs.Length; i++)
            {
                IntVec3 n = c + dirs[i];
                if (n.GetDoor(map) != null)
                    return false;
                if (!againstWall && (!n.InBounds(map) || n.Filled(map)))
                    againstWall = true;
            }
            return againstWall;
        }

        private static bool TryRectContaining(List<CellRect> rects, IntVec3 cell, out CellRect rect)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                if (rects[i].Contains(cell))
                {
                    rect = rects[i];
                    return true;
                }
            }
            rect = default;
            return false;
        }
    }
}
