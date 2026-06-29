using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // "Given up" phase of LordJob_BTGShelterCivilians: reached only when no launchable exists
    // to escape on. The subroom door is open by now, so the family forages the wider structure
    // and wanders the wider nursery instead of staying boxed in. Assigns BTG_StrandedAdult to
    // the caretaker (still tends babies, then forages / calls a resupply when starving) and
    // BTG_StrandedChild to walking children (forage / eat / sleep / wander).
    //
    // The forage/resupply nodes reuse the entrenched-defender hunger chain
    // (JobGiver_BTGForageInStructure / JobGiver_BTGCallResupply), which bound themselves to the
    // structure footprint via StructureBoundsCache, so no duty radius is needed here.
    public class LordToil_BTGStranded : LordToil
    {
        private IntVec3 focus;

        public override IntVec3 FlagLoc => focus;

        public LordToil_BTGStranded(IntVec3 focus)
        {
            this.focus = focus;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                DutyDef def = pawn.DevelopmentalStage.Adult() ? Duties.BTG_StrandedAdult : Duties.BTG_StrandedChild;
                pawn.mindState.duty = new PawnDuty(def, focus);
            }
        }
    }
}
