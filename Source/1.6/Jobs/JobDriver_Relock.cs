using System.Collections.Generic;
using BetterTradersGuild.Comps;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    /// <summary>
    /// JobDriver for relocking a cargo vault hatch.
    /// Similar to vanilla's JobDriver_Seal but allows the hatch to be hacked again.
    ///
    /// The pawn goes to the hatch and performs work, then the hatch is relocked.
    /// This mirrors the vanilla sealing behavior for consistency.
    /// </summary>
    public class JobDriver_Relock : JobDriver
    {
        /// <summary>
        /// Work amount required to relock the hatch.
        /// Similar to vanilla seal work amount.
        /// </summary>
        private const float WorkAmount = 300f;

        /// <summary>
        /// Target index for the hatch being relocked.
        /// </summary>
        private const TargetIndex HatchIndex = TargetIndex.A;

        private Thing Hatch => job.GetTarget(HatchIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Hatch, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail if the hatch is destroyed or no longer has CompRelockable
            this.FailOnDespawnedNullOrForbidden(HatchIndex);
            this.FailOn(() => Hatch.TryGetComp<CompRelockable>() == null);

            // Go to the hatch
            yield return Toils_Goto.GotoThing(HatchIndex, PathEndMode.Touch);

            // Perform work
            Toil workToil = ToilMaker.MakeToil("MakeNewToils");
            workToil.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Hatch);
            };
            workToil.defaultCompleteMode = ToilCompleteMode.Delay;
            workToil.defaultDuration = (int)WorkAmount;
            workToil.WithProgressBarToilDelay(HatchIndex);
            yield return workToil;

            // Complete: trigger the relock
            Toil completeToil = ToilMaker.MakeToil("MakeNewToils");
            completeToil.initAction = delegate
            {
                CompRelockable relockable = Hatch.TryGetComp<CompRelockable>();
                relockable?.Relock();
            };
            completeToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return completeToil;
        }
    }
}
