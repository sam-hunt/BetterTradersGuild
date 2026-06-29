using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Escape job for a sheltering-civilian walker: board a launchable, entering its
    // transporter container. TargetA = launchable.
    //
    // Mirrors the entering half of vanilla JobDriver_EnterTransporter (despawn, then add to
    // innerContainer) without the loading-group dependency. No reservation on the launchable
    // so several walkers can board the same shuttle. Issued by JobGiver_BTGBoardLaunchable
    // once a walker has no infant left to ferry; lift-off is driven by LordToil_BTGEscape.
    public class JobDriver_BTGBoardLaunchable : JobDriver
    {
        private const TargetIndex LaunchableIndex = TargetIndex.A;

        private Thing Launchable => job.GetTarget(LaunchableIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Launchable == null || Launchable.Destroyed
                || Launchable.TryGetComp<CompTransporter>() == null);

            yield return Toils_Goto.GotoThing(LaunchableIndex, PathEndMode.Touch)
                .FailOnDestroyedOrNull(LaunchableIndex);

            Toil board = ToilMaker.MakeToil("BTGBoardLaunchable");
            board.initAction = () =>
            {
                CompTransporter transporter = Launchable?.TryGetComp<CompTransporter>();
                if (transporter?.innerContainer == null)
                    return;

                IntVec3 cell = Launchable.Position;
                Map map = Launchable.Map;
                pawn.DeSpawnOrDeselect();
                if (!transporter.innerContainer.TryAdd(pawn))
                {
                    // Couldn't board (e.g. craft became invalid mid-toil): re-spawn so the
                    // pawn isn't lost into limbo.
                    GenSpawn.Spawn(pawn, cell, map);
                }
            };
            board.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return board;
        }
    }
}
