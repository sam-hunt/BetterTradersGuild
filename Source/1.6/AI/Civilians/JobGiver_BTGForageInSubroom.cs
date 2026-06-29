using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Shelter-phase forager: the structure-bounded forager (JobGiver_BTGForageInStructure)
    // additionally confined to the crib subroom via the duty's focus + radius. Keeps a
    // sheltering caretaker/child foraging only the subroom even if the player breaches the
    // locked door mid-shelter. (The locked door is the primary confinement; this is the
    // backstop.) The stranded phase keeps the plain structure-wide forager.
    public class JobGiver_BTGForageInSubroom : JobGiver_BTGForageInStructure
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
