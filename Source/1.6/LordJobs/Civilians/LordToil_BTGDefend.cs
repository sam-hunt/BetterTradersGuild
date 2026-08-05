using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // Reactive self-defense posture of LordJob_BTGShelterCivilians: entered from Escape or
    // Stranded while someone is actively attacking the family AND a child walker still needs
    // covering. Only the adult's duty changes - BTG_DefendWalker melees the nearest involved
    // attacker and falls through to the normal escape chain when none is reachable - while
    // children keep plain BTG_EscapeWalker and keep boarding, so the caretaker is covering a
    // retreat, not holding ground. Subclasses LordToil_BTGEscape so the lift-off tick keeps
    // running: pods and the shuttle continue leaving as their passengers complete boarding
    // mid-fight (the shuttle still waits for the caretaker - an active walker is always
    // "bound for" it).
    public class LordToil_BTGDefend : LordToil_BTGEscape
    {
        public LordToil_BTGDefend(IntVec3 focus) : base(focus)
        {
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                DutyDef def = pawn.DevelopmentalStage.Adult() ? Duties.BTG_DefendWalker : Duties.BTG_EscapeWalker;
                pawn.mindState.duty = new PawnDuty(def, focus);
            }
        }
    }
}
