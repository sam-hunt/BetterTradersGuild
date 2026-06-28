using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BetterTradersGuild.JobDrivers
{
    // Flicks a closed CompFlickable valve back ON without a player Flick designation.
    //
    // Vanilla JobDriver_Flick is the colonist work path: it FailsOn the target lacking a
    // DesignationDefOf.Flick designation, which non-colonist settlement defenders (no
    // work settings, no player designations) can never get. This driver mirrors
    // JobDriver_Flick's toils (goto Touch -> short wait -> flick) but drops the
    // designation gate, letting a starving defender re-open a nutrient-paste valve that
    // BTG mapgen (PipeValveHandler) closed to lock the settlement down.
    //
    // Re-opening reconnects a VNPE nutrient-paste vat to the pipe net; on a later forage
    // tick the now-fed taps can dispense (JobGiver_BTGForageInStructure step 4). Setting
    // CompFlickable.SwitchIsOn = true flips the live state and broadcasts "FlickedOn" -
    // the signal the VE valve listens for to rejoin the net, the same setter
    // PipeValveHandler used (with false) to isolate it at mapgen.
    //
    // Used by JobGiver_BTGForageInStructure (the paste-valve last resort).
    public class JobDriver_BTGOpenPasteValve : JobDriver
    {
        private const TargetIndex ValveIndex = TargetIndex.A;

        // Matches vanilla JobDriver_Flick's flick duration.
        private const int FlickTicks = 15;

        private Thing Valve => job.GetTarget(ValveIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Valve, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(ValveIndex);
            // Bail if it lost its flickable or was already opened (e.g. by another
            // defender) between selection and arrival. Null-safe: ?. short-circuits
            // before the despawn fail condition above can end the job.
            this.FailOn(() =>
            {
                CompFlickable f = Valve?.TryGetComp<CompFlickable>();
                return f == null || f.SwitchIsOn;
            });

            yield return Toils_Goto.GotoThing(ValveIndex, PathEndMode.Touch)
                .FailOnDespawnedOrNull(ValveIndex);

            yield return Toils_General.Wait(FlickTicks, ValveIndex)
                .WithProgressBarToilDelay(ValveIndex)
                .FailOnDespawnedOrNull(ValveIndex)
                .FailOnCannotTouch(ValveIndex, PathEndMode.Touch);

            yield return FlickOnToil();
        }

        // Opens the valve and plays the flick sound. Mirrors CompFlickable.DoFlick but
        // sets the state explicitly (rather than toggling) so a valve that somehow turned
        // on mid-job is never flicked back off.
        private Toil FlickOnToil()
        {
            Toil toil = ToilMaker.MakeToil("BTGFlickOnValve");
            toil.initAction = () =>
            {
                CompFlickable flickable = Valve?.TryGetComp<CompFlickable>();
                if (flickable == null || flickable.SwitchIsOn)
                    return;

                flickable.SwitchIsOn = true;
                SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(Valve.Position, Valve.Map));
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
