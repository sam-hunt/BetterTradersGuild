using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Shared destination-picking for the "walk back to your home room before winding
    // down" think nodes (agrihand greenhouse, paramedic medbay). The standby givers
    // only park a mech against a wall of the rect it is already standing in, so any
    // duty that legitimately sends its mech out of its room (structure-wide rescue,
    // pot planting) needs a return-home node above standby - this holds the one
    // piece of that logic worth sharing.
    internal static class MechReturnHome
    {
        // Prefer the anchor (the lord's room centre); if it is unstandable or
        // unreachable, fall back to the nearest standable, reachable cell in the
        // home rects.
        public static bool TryFindHomeCell(Pawn pawn, IntVec3 anchor, List<CellRect> rects, out IntVec3 dest)
        {
            Map map = pawn.Map;
            if (anchor.IsValid && anchor.Standable(map)
                && pawn.CanReach(anchor, PathEndMode.OnCell, Danger.Deadly))
            {
                dest = anchor;
                return true;
            }

            IntVec3 from = anchor.IsValid ? anchor : pawn.Position;
            IntVec3 best = IntVec3.Invalid;
            int bestDistSq = int.MaxValue;
            for (int i = 0; i < rects.Count; i++)
            {
                foreach (IntVec3 c in rects[i])
                {
                    if (!c.Standable(map))
                        continue;
                    int distSq = (c - from).LengthHorizontalSquared;
                    if (distSq >= bestDistSq)
                        continue;
                    if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly))
                        continue;
                    bestDistSq = distSq;
                    best = c;
                }
            }

            dest = best;
            return best.IsValid;
        }
    }
}
