using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    /// <summary>
    /// LordToil for the bounded defender state. Assigns Duties.BTG_DefendStructure
    /// to every pawn in the lord with focus = baseCenter and a generous
    /// radius. The radius governs vanilla JobGiver_AIDefendPoint's target
    /// acquire/keep distances (60 cells acquire, 90 cells keep); the actual
    /// geographic constraint comes from JobGiver_BTGDefendStructure filtering
    /// targets to the union of structure room rects.
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
