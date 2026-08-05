using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Paramedic-mech overflow rescue driver: carry a downed casualty (TargetA) to a
    // clear wall-adjacent medbay floor cell (TargetB) when every medical bed is
    // taken. JobDriver_BTGRescue with the bed leg replaced by a cell leg - go to the
    // casualty, pick them up, walk to the drop cell, lay them down. Once on the
    // medbay floor the tend/feed nodes cover them and the bed-rescue node promotes
    // them into a bed when one frees up.
    //
    // Same NPC-safety rationale as BTG_Rescue (no guest machinery, no
    // Faction.OfPlayer assumptions), and the same resume-mid-carry setup: the JobDef
    // keeps the takee in the carry tracker across job restarts, the JumpIf skips
    // straight to the drop leg when already carrying, and the finish action puts the
    // casualty down where the medic stands if the job ends early.
    public class JobDriver_BTGCarryToMedbayFloor : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;

        private const TargetIndex CellIndex = TargetIndex.B;

        protected Pawn Takee => (Pawn)job.GetTarget(TakeeIndex).Thing;

        protected IntVec3 DropCell => job.GetTarget(CellIndex).Cell;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Takee.ClearAllReservations();
            if (pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed))
                return pawn.Reserve(DropCell, job, 1, -1, null, errorOnFailed);
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TakeeIndex);
            AddFinishAction(jobCondition =>
            {
                // If the job ends early mid-carry, put the casualty down where the medic
                // stands instead of leaving them in the carry tracker.
                if (jobCondition != JobCondition.Ongoing && pawn.carryTracker.CarriedThing != null)
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out _);
            });

            Toil goToTakee = Toils_Goto.GotoThing(TakeeIndex, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TakeeIndex)
                .FailOn(() => !pawn.CanReach(DropCell, PathEndMode.OnCell, Danger.Deadly))
                .FailOn(() => !Takee.Downed)
                .FailOnSomeonePhysicallyInteracting(TakeeIndex);
            Toil startCarrying = Toils_Haul.StartCarryThing(TakeeIndex);
            Toil goToCell = Toils_Goto.GotoCell(CellIndex, PathEndMode.OnCell)
                .FailOn(() => !pawn.IsCarryingPawn(Takee));

            // A re-issued job (interrupt, save/load) skips straight to the drop leg when
            // the casualty is already in the medic's arms.
            yield return Toils_Jump.JumpIf(goToCell, () => pawn.IsCarryingPawn(Takee));
            yield return goToTakee;
            yield return startCarrying;
            yield return goToCell;
            yield return Toils_General.Do(() =>
            {
                // Direct should always succeed for a pawn on a standable cell; the Near
                // fallback only guards the degenerate case so the casualty is never left
                // stuck in the carry tracker with the job complete.
                if (!pawn.carryTracker.TryDropCarriedThing(DropCell, ThingPlaceMode.Direct, out _))
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            });
        }
    }
}
