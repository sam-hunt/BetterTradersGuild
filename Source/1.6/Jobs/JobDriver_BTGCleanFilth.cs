using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Cleansweeper-mech clean driver. A faithful copy of vanilla JobDriver_CleanFilth -
    // same queue handling, same thin-filth-until-destroyed work loop, same effecter /
    // progress bar / sound - with ONLY the two JumpIfOutsideHomeArea guards removed.
    //
    // Why the copy exists: vanilla cleaning (both WorkGiver_CleanFilth and
    // JobDriver_CleanFilth) is gated entirely on the player's painted Home area
    // (areaManager.Home). A player cleansweeper works because the colony has a Home area;
    // a BTG orbital settlement never does, so every queued filth fails the home-area jump,
    // the job drains its queue without the mech ever moving or cleaning, "succeeds" having
    // done nothing, and the duty tree just re-issues it forever (the stuck-cleansweeper
    // bug). The agrihand harvest driver has no such gate, which is why farming worked.
    //
    // The work area is instead defined by JobGiver_BTGMechCleanFilth (radius around the
    // anchor + StructureBoundsCache) - exactly the role Home area plays for the player - so
    // dropping the home-area jumps loses no confinement: the driver only ever receives
    // in-range, in-bounds, reachable targets the giver already vetted.
    public class JobDriver_BTGCleanFilth : JobDriver
    {
        private float cleaningWorkDone;

        private float totalCleaningWorkDone;

        private float totalCleaningWorkRequired;

        private const TargetIndex FilthInd = TargetIndex.A;

        private Filth Filth => (Filth)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil initExtractTargetFromQueue = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets(TargetIndex.A);
            yield return initExtractTargetFromQueue;
            yield return Toils_JobTransforms.SucceedOnNoTargetInQueue(TargetIndex.A);
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).JumpIfDespawnedOrNullOrForbidden(TargetIndex.A, initExtractTargetFromQueue);
            Toil clean = ToilMaker.MakeToil("MakeNewToils");
            clean.initAction = delegate
            {
                cleaningWorkDone = 0f;
                totalCleaningWorkDone = 0f;
                totalCleaningWorkRequired = Filth.def.filth.cleaningWorkToReduceThickness * (float)Filth.thickness;
            };
            clean.tickIntervalAction = delegate(int delta)
            {
                Filth filth = Filth;
                float statValueAbstract = filth.Position.GetTerrain(filth.Map).GetStatValueAbstract(StatDefOf.CleaningTimeFactor);
                float num = pawn.GetStatValue(StatDefOf.CleaningSpeed) * (float)delta;
                if (statValueAbstract != 0f)
                {
                    num /= statValueAbstract;
                }
                cleaningWorkDone += num;
                totalCleaningWorkDone += num;
                if (cleaningWorkDone > filth.def.filth.cleaningWorkToReduceThickness)
                {
                    filth.ThinFilth();
                    cleaningWorkDone = 0f;
                    if (filth.Destroyed)
                    {
                        clean.actor.records.Increment(RecordDefOf.MessesCleaned);
                        ReadyForNextToil();
                    }
                }
            };
            clean.defaultCompleteMode = ToilCompleteMode.Never;
            clean.WithEffect(EffecterDefOf.Clean, TargetIndex.A);
            clean.WithProgressBar(TargetIndex.A, () => totalCleaningWorkDone / totalCleaningWorkRequired, interpolateBetweenActorAndTarget: true);
            clean.PlaySustainerOrSound(delegate
            {
                ThingDef def = Filth.def;
                return (!def.filth.cleaningSound.NullOrUndefined()) ? def.filth.cleaningSound : SoundDefOf.Interact_CleanFilth;
            });
            clean.JumpIfDespawnedOrNullOrForbidden(TargetIndex.A, initExtractTargetFromQueue);
            clean.JumpIf(() => clean.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing?.Destroyed ?? false, initExtractTargetFromQueue);
            yield return clean;
            yield return Toils_Jump.Jump(initExtractTargetFromQueue);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cleaningWorkDone, "cleaningWorkDone", 0f);
            Scribe_Values.Look(ref totalCleaningWorkDone, "totalCleaningWorkDone", 0f);
            Scribe_Values.Look(ref totalCleaningWorkRequired, "totalCleaningWorkRequired", 0f);
        }
    }
}
