using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // Initial phase of LordJob_BTGShelterCivilians: the family is locked in the crib subroom.
    // Assigns role-specific duties around the subroom centre - the caretaker gets
    // BTG_ShelterAdult (defend the subroom, tend babies, eat/sleep/wander), walking children
    // get BTG_ShelterChild (eat/sleep, wander a tight area near the caretaker).
    //
    // Babies/newborns are not lord members (they stay autonomous in their cribs and are
    // located by the caretaker's jobgivers via a faction + developmental-stage scan), so only
    // the caretaker and children are iterated here.
    public class LordToil_BTGShelter : LordToil
    {
        // Doubles as the caretaker's combat flag radius (subroom-local: a melee guard pinned
        // by the locked door can only reach intruders who are already inside anyway).
        private const float AdultRadius = 6f;
        private const float ChildRadius = 3f;

        private IntVec3 focus;

        public override IntVec3 FlagLoc => focus;

        public LordToil_BTGShelter(IntVec3 focus)
        {
            this.focus = focus;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                if (pawn.DevelopmentalStage.Adult())
                    pawn.mindState.duty = new PawnDuty(Duties.BTG_ShelterAdult, focus, AdultRadius);
                else
                    pawn.mindState.duty = new PawnDuty(Duties.BTG_ShelterChild, focus, ChildRadius);
            }
        }
    }
}
