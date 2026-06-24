using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    /// <summary>
    /// LordToil for the bounded defender state. Assigns Duties.BTG_DefendStructure
    /// to every pawn in the lord with focus = baseCenter and a generous radius.
    /// PawnDuty.radius becomes the "flag radius" in JobGiver_AIDefendPoint — targets
    /// must lie within 60 cells of baseCenter. The per-pawn acquire/keep distances
    /// come from JobGiver_AIFightEnemy's defaults (56 acquire, 65 keep), which our
    /// duty XML leaves unchanged. None of these radii are the real geographic
    /// constraint: that comes from JobGiver_BTGDefendStructure filtering targets to
    /// the union of structure room rects.
    /// </summary>
    public class LordToil_BTGDefendStructure : LordToil
    {
        private const float DefendRadius = 60f;

        private IntVec3 baseCenter;

        public IntVec3 BaseCenter => baseCenter;

        public override IntVec3 FlagLoc => baseCenter;

        public LordToil_BTGDefendStructure(IntVec3 baseCenter)
        {
            this.baseCenter = baseCenter;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                pawn.mindState.duty = new PawnDuty(Duties.BTG_DefendStructure, baseCenter, DefendRadius);
            }
        }
    }
}
