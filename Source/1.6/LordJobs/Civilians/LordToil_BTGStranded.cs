using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // "Given up" phase of LordJob_BTGShelterCivilians: reached when no walker can currently
    // reach a launchable (the lord re-checks this and re-promotes back to Escape if
    // reachability returns). The subroom door is usually open by now, but stranding can also
    // fire while it is still locked (e.g. no launchable exists at all), so both stranded duties
    // keep the hack giver as a sealed-in safety net. Assigns BTG_StrandedAdult to the caretaker
    // (still tends babies, then forages / calls a resupply when starving) and BTG_StrandedChild
    // to walking children (forage / eat / sleep / wander).
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
