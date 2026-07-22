using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BetterTradersGuild.JobDrivers
{
    // NPC hacking driver. Vanilla JobDriver_Hack copied verbatim except for two changes:
    //
    // 1. Hack() is called with suppressMessages: true. Vanilla assumes a player-side
    //    hacker: CompHackable.ProcessHacked sends the def's hackedMessage to the PLAYER
    //    as a PositiveEvent, and AncientBlastDoor defines one - so every time a TG
    //    civilian or defender hacked open one of their own settlement doors, the player
    //    got a "finished hacking" notification about NPC business. Known leak that
    //    suppressMessages cannot reach: a failed stealth roll calls CompHackable.LockOut,
    //    whose MessageHackerLockedOut is unconditional. It is rare (MTB roll against
    //    HackingStealth) and the mechanic itself is wanted - the hack givers gate on
    //    CanHackNow, so a locked-out pawn just retries hours later. Passing a null hacker
    //    would silence it but also delete the lockout mechanic (and skill XP) entirely.
    //
    // 2. The Intellectual-skill references are null-guarded. Vanilla dereferences
    //    pawn.skills in a fail condition and the tick action, which would NRE for a
    //    skill-less pawn (mech). No BTG mech hacks today; this is cheap insurance so the
    //    driver doesn't bake in the assumption. A skill-less pawn counts as level 0
    //    against intellectualSkillPrerequisite.
    //
    // Used by: JobGiver_BTGHackShelterDoor, JobGiver_BTGHackDoorForFood.
    public class JobDriver_BTGHack : JobDriver
    {
        private Thing HackTarget => TargetThingA;

        private CompHackable CompHacking => HackTarget.TryGetComp<CompHackable>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(HackTarget, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => CompHacking.Props.intellectualSkillPrerequisite > 0
                && (pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0) < CompHacking.Props.intellectualSkillPrerequisite);
            PathEndMode pathEndMode = TargetThingA.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.ClosestTouch;
            yield return Toils_Goto.GotoThing(TargetIndex.A, pathEndMode);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.handlingFacing = true;
            toil.tickAction = delegate
            {
                float statValue = pawn.GetStatValue(StatDefOf.HackingSpeed);
                CompHacking.Hack(statValue, pawn, suppressMessages: true);
                pawn.skills?.Learn(SkillDefOf.Intellectual, 0.1f);
                pawn.rotationTracker.FaceTarget(HackTarget);
            };
            toil.WithEffect(EffecterDefOf.Hacking, TargetIndex.A);
            if (CompHacking.Props.effectHacking != null)
            {
                toil.WithEffect(() => CompHacking.Props.effectHacking, () => HackTarget.OccupiedRect().ClosestCellTo(pawn.Position));
            }
            toil.WithProgressBar(TargetIndex.A, () => CompHacking.ProgressPercent, interpolateBetweenActorAndTarget: false, -0.5f, alwaysShow: true);
            toil.PlaySoundAtStart(SoundDefOf.Hacking_Started);
            toil.PlaySustainerOrSound(SoundDefOf.Hacking_InProgress);
            toil.AddFinishAction(delegate
            {
                if (CompHacking.IsHacked)
                {
                    SoundDefOf.Hacking_Completed.PlayOneShot(HackTarget);
                    CompHacking.Props.hackingCompletedSound?.PlayOneShot(HackTarget);
                }
                else
                {
                    SoundDefOf.Hacking_Suspended.PlayOneShot(HackTarget);
                }
            });
            toil.FailOnCannotTouch(TargetIndex.A, pathEndMode);
            toil.FailOn(() => CompHacking.IsHacked || CompHacking.LockedOut);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.activeSkill = () => SkillDefOf.Intellectual;
            yield return toil;
        }
    }
}
