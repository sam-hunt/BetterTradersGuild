using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    /// <summary>
    /// Opens an IOpenable container (e.g. an Odyssey survival-meal pallet) WITHOUT
    /// requiring a player Open designation.
    ///
    /// Vanilla JobDriver_Open is the colonist work path: its goto toil
    /// FailsOnThingMissingDesignation(DesignationDefOf.Open), so it only runs on
    /// containers the player has flagged. Settlement defenders are non-colonist NPCs
    /// with no work settings and no player designations, so they can never use that
    /// path. This driver mirrors JobDriver_Open's toils (goto -> wait OpenTicks ->
    /// Toils_General.Open) but drops the designation gates, letting a foraging
    /// defender crack open an in-structure meal pallet when no other food remains.
    /// Toils_General.Open still no-ops the (absent) designation delete and calls
    /// IOpenable.Open(), which ejects the crate contents onto the floor for the
    /// defender to eat on its next forage think tick.
    ///
    /// Used by JobGiver_BTGForageInStructure (the survival-meal-pallet fallback).
    /// </summary>
    public class JobDriver_BTGOpenContainer : JobDriver
    {
        private const TargetIndex ContainerIndex = TargetIndex.A;

        private Thing Container => job.GetTarget(ContainerIndex).Thing;
        private IOpenable Openable => (IOpenable)Container;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Container, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(ContainerIndex);
            // Bail if it became empty / locked between selection and arrival.
            // Null-safe: don't cast a despawned (null) target before the despawn
            // fail condition above gets a chance to end the job.
            this.FailOn(() => !(Container is IOpenable o) || !o.CanOpen);

            yield return Toils_Goto.GotoThing(ContainerIndex, PathEndMode.InteractionCell)
                .FailOnDespawnedOrNull(ContainerIndex);

            Toil wait = Toils_General.Wait(Openable.OpenTicks, ContainerIndex)
                .WithProgressBarToilDelay(ContainerIndex)
                .FailOnDespawnedOrNull(ContainerIndex)
                .FailOnCannotTouch(ContainerIndex, PathEndMode.InteractionCell);
            if (Container.def.building?.openingStartedSound != null)
                wait.PlaySoundAtStart(Container.def.building.openingStartedSound);
            yield return wait;

            yield return Toils_General.Open(ContainerIndex);
        }
    }
}
