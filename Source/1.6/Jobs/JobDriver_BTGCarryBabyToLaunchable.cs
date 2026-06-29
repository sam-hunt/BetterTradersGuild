using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Escape job for a sheltering-civilian walker: pick up one infant/baby and load it into
    // a launchable's transporter container. TargetA = baby, TargetB = launchable.
    //
    // This is a stripped-down haul-into-container: unlike vanilla JobDriver_HaulToTransporter
    // it does NOT use the transporter loading group / leftToLoad machinery (lift-off is the
    // custom vanish in LaunchableEscapeHelper, not vanilla TryLaunch), so it just carries the
    // baby and drops it straight into the craft's innerContainer. Issued by
    // JobGiver_BTGCarryBabyToLaunchable; carrying lets walking children ferry babies the
    // vanilla colonist-work path would refuse them.
    public class JobDriver_BTGCarryBabyToLaunchable : JobDriver
    {
        private const TargetIndex BabyIndex = TargetIndex.A;
        private const TargetIndex LaunchableIndex = TargetIndex.B;

        private Pawn Baby => job.GetTarget(BabyIndex).Thing as Pawn;
        private Thing Launchable => job.GetTarget(LaunchableIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Baby, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Launchable must stay valid the whole job; the baby is only checked until pickup
            // (after pickup it is carried/despawned, so a global despawn-fail would misfire).
            this.FailOn(() => Launchable == null || Launchable.Destroyed
                || Launchable.TryGetComp<CompTransporter>() == null);

            yield return Toils_Goto.GotoThing(BabyIndex, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(BabyIndex);

            Toil pickUp = ToilMaker.MakeToil("BTGPickUpBaby");
            pickUp.initAction = () =>
            {
                Pawn baby = Baby;
                if (baby == null || !baby.Spawned || !pawn.carryTracker.TryStartCarry(baby))
                    EndJobWith(JobCondition.Incompletable);
            };
            pickUp.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pickUp;

            yield return Toils_Goto.GotoThing(LaunchableIndex, PathEndMode.Touch)
                .FailOnDestroyedOrNull(LaunchableIndex);

            Toil load = ToilMaker.MakeToil("BTGLoadBabyIntoLaunchable");
            load.initAction = () =>
            {
                CompTransporter transporter = Launchable?.TryGetComp<CompTransporter>();
                Thing carried = pawn.carryTracker.CarriedThing;
                if (transporter?.innerContainer == null || carried == null)
                    return;
                pawn.carryTracker.innerContainer.TryTransferToContainer(carried, transporter.innerContainer, 1, out Thing _);
            };
            load.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return load;
        }
    }
}
