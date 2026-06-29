using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Shelter-phase rest giver: the structure-bounded rest giver (JobGiver_BTGRestInStructure)
    // additionally confined to the crib subroom via the duty's focus + radius. Keeps a
    // sheltering caretaker/child sleeping only in the subroom even if the player breaches the
    // locked door mid-shelter. The stranded phase keeps the plain structure-wide rest giver.
    public class JobGiver_BTGRestInSubroom : JobGiver_BTGRestInStructure
    {
        protected override bool WithinBounds(Pawn pawn, IntVec3 pos)
        {
            if (!base.WithinBounds(pawn, pos))
                return false;

            PawnDuty duty = pawn.mindState?.duty;
            if (duty == null || !duty.focus.IsValid || duty.radius <= 0f)
                return true;

            return (pos - duty.focus.Cell).LengthHorizontalSquared <= duty.radius * duty.radius;
        }
    }
}
