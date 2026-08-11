using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Aimless idle wandering for post-defeat survivors, bounded to the structure
    // footprint. Unlike JobGiver_WanderNearDutyLocation (fixed root = the duty focus,
    // so survivors regroup and loiter at the old defense point), the wander root is
    // usually the pawn's own position and occasionally a random room anywhere in the
    // structure: survivors drift apart and roam the halls instead of clustering like a
    // garrison still holding a rally point.
    //
    // Containment is belt and braces: roam roots must be reachable structure cells
    // (stranded pawns start inside, so local roots are too), and the dest validator
    // rejects any cell outside the footprint, so a wander step can never pick a cell
    // out in vacuum. No layout bounds known -> mill around in place only, matching the
    // "no bounds, don't roam" fallback of the other in-structure givers.
    public class JobGiver_BTGWanderInStructure : JobGiver_Wander
    {
        // How often a wander cycle relocates toward a random room instead of milling
        // around the current spot. Low: mostly local shuffling, occasional aimless
        // treks through the halls.
        private const float RoamChance = 0.3f;
        private const int RoamRootAttempts = 8;

        public JobGiver_BTGWanderInStructure()
        {
            wanderRadius = 7f;
            // Longer pauses than the vanilla default (20-100): weary survivors stand
            // around more than they pace.
            ticksBetweenWandersRange = new IntRange(125, 300);
            wanderDestValidator = (pawn, dest, root) => StructureBoundsCache.Contains(pawn.Map, dest);
        }

        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            if (Rand.Chance(RoamChance))
            {
                List<CellRect> rects = StructureBoundsCache.GetRoomRects(pawn.Map);
                if (rects != null)
                {
                    // Weight by area so a big hall draws proportionally more visits
                    // than a closet-sized subroom.
                    for (int i = 0; i < RoamRootAttempts; i++)
                    {
                        IntVec3 cell = rects.RandomElementByWeight(r => r.Area).RandomCell;
                        if (cell.InBounds(pawn.Map) && cell.Standable(pawn.Map)
                            && pawn.CanReach(cell, PathEndMode.OnCell, Danger.None))
                            return cell;
                    }
                }
            }
            return pawn.Position;
        }
    }
}
