using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    // Post-defeat "given up" phase of LordJob_BTGDefendStructure: no launchable is
    // reachable, so survivors settle into a peaceful routine (tend infants, forage,
    // rest, wander) via BTG_StrandedDefender. The lord re-checks reachability and
    // re-promotes to the escape toil if a way off the station appears.
    public class LordToil_BTGStrandedDefender : LordToil
    {
        private IntVec3 focus;

        public override IntVec3 FlagLoc => focus;

        public LordToil_BTGStrandedDefender(IntVec3 focus)
        {
            this.focus = focus;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                pawn.mindState.duty = new PawnDuty(Duties.BTG_StrandedDefender, focus);
            }
        }
    }
}
