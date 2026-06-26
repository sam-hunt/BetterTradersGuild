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
    /// open) but drops the designation gates, letting a foraging defender crack
    /// open an in-structure meal pallet when no other food remains.
    ///
    /// The final toil opens via IOpenable.Open() (which ejects the crate contents
    /// onto the floor) and then forbids the ejected items. Forbidding keeps player
    /// pawns from being handed haul jobs for the meals while hostile defenders are
    /// still active inside the structure; it does NOT stop the defenders from
    /// eating them, because ForbidUtility only honours the forbidden flag against
    /// Faction.OfPlayer - a hostile defender's IsForbidden() check (including the
    /// one in JobGiver_BTGForageInStructure step 2) returns false regardless.
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

            yield return OpenAndForbidToil();
        }

        // Replaces vanilla Toils_General.Open: opens the container, then forbids the
        // meals it just ejected so player pawns won't haul them mid-fight. The skipped
        // record/designation bookkeeping in the vanilla toil is colonist-only and
        // irrelevant to an NPC defender.
        private Toil OpenAndForbidToil()
        {
            Toil toil = ToilMaker.MakeToil("BTGOpenAndForbid");
            toil.initAction = () =>
            {
                if (!(Container is IOpenable openable) || !openable.CanOpen)
                    return;

                // Snapshot the contents before opening. Open() ejects them via
                // ThingOwner.TryDropAll(.., Near), which spawns these same Thing
                // references unless one stacks into an adjacent pile (then it is
                // destroyed and the surviving pile - already forbidden from an
                // earlier open in this settlement - keeps the flag).
                List<Thing> ejected = null;
                if (Container is IThingHolder holder)
                {
                    ThingOwner owner = holder.GetDirectlyHeldThings();
                    if (owner != null && owner.Count > 0)
                        ejected = new List<Thing>(owner);
                }

                openable.Open();

                if (ejected != null)
                {
                    for (int i = 0; i < ejected.Count; i++)
                    {
                        Thing thing = ejected[i];
                        if (thing != null && thing.Spawned)
                            thing.SetForbidden(true, warnOnFail: false);
                    }
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
