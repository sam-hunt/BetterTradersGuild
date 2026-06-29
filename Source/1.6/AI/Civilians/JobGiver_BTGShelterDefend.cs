using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Shelter-phase combat for the caretaker: engage hostiles, but only ones inside the crib
    // subroom. Subclasses vanilla JobGiver_AIDefendPoint (which holds firing/melee position
    // near duty.focus rather than pursuing) and rejects any target beyond the duty radius of
    // the focus, so the armed caretaker fights intruders who breach the shelter but never
    // chases them out into the wider settlement.
    //
    // The locked blast door is the real containment (a melee caretaker can only reach what is
    // already inside); the radius bound is a cheap belt-and-braces filter so target
    // acquisition doesn't drag the caretaker to the doorway after something just outside.
    public class JobGiver_BTGShelterDefend : JobGiver_AIDefendPoint
    {
        protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            if (!base.ExtraTargetValidator(pawn, target))
                return false;

            PawnDuty duty = pawn.mindState?.duty;
            if (duty == null || !duty.focus.IsValid)
                return false;

            float radius = duty.radius > 0f ? duty.radius : 8f;
            return (target.Position - duty.focus.Cell).LengthHorizontal <= radius;
        }
    }
}
