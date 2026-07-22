using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Paramedic-mech rescue driver. Vanilla JobDriver_TakeToBed reduced to the rescue
    // path - go to the downed casualty, pick them up, tuck them into the medbay bed -
    // with the guest/prisoner/arrest machinery removed.
    //
    // Why the copy exists: vanilla's driver assumes the rescuer is a player pawn. Its
    // CheckMakeTakeeGuest (pre-init on the carry toil) hardcodes Faction.OfPlayer as the
    // host when it makes the rescued pawn a guest. Run from the NPC medic on a TG
    // settlement that is a red error when the settlement is hostile to the player (the
    // assault case: "Tried to make X a guest of Y but their faction is hostile"), and
    // worse when it is not: the settlement's own defender silently becomes a guest of
    // the player faction, and GenHostility.HostileTo returns false for any pawn whose
    // HostFaction matches the queried faction - so player targeting and
    // SettlementDefeatUtility.CheckDefeated both stop counting that defender.
    //
    // Kept from vanilla: the fail conditions (casualty no longer downed, bed no longer
    // usable, someone else interacting), resume-mid-carry via the IsCarryingPawn jump
    // (the JobDef keeps the takee in the carry tracker across job restarts), the
    // drop-on-interrupt finish action, and creating playerSettings before tuck-in
    // (vanilla gives every rescued pawn one; downstream medical code expects it on
    // bed-ridden pawns).
    public class JobDriver_BTGRescue : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;

        private const TargetIndex BedIndex = TargetIndex.B;

        protected Pawn Takee => (Pawn)job.GetTarget(TakeeIndex).Thing;

        protected Building_Bed DropBed => (Building_Bed)job.GetTarget(BedIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Takee.ClearAllReservations();
            if (pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed))
                return pawn.Reserve(DropBed, job, DropBed.SleepingSlotsCount, 0, null, errorOnFailed);
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TakeeIndex);
            this.FailOnDestroyedOrNull(BedIndex);
            this.FailOn(() => DropBed.ForPrisoners != Takee.IsPrisoner);
            AddFinishAction(jobCondition =>
            {
                // If the job ends early mid-carry, put the casualty down where the medic
                // stands instead of leaving them in the carry tracker.
                if (jobCondition != JobCondition.Ongoing && pawn.carryTracker.CarriedThing != null)
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out _);
            });

            Toil goToTakee = Toils_Goto.GotoThing(TakeeIndex, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TakeeIndex)
                .FailOnDespawnedNullOrForbidden(BedIndex)
                .FailOn(() => !pawn.CanReach(DropBed, PathEndMode.OnCell, Danger.Deadly))
                .FailOn(() => !Takee.Downed)
                .FailOnSomeonePhysicallyInteracting(TakeeIndex);
            Toil startCarrying = Toils_Haul.StartCarryThing(TakeeIndex);
            startCarrying.FailOnBedNoLongerUsable(BedIndex, TakeeIndex);
            Toil goToBed = Toils_Goto.GotoThing(BedIndex, PathEndMode.Touch)
                .FailOn(() => !pawn.IsCarryingPawn(Takee));
            goToBed.FailOnBedNoLongerUsable(BedIndex, TakeeIndex);

            // A re-issued job (interrupt, save/load) skips straight to the bed leg when
            // the casualty is already in the medic's arms.
            yield return Toils_Jump.JumpIf(goToBed, () => pawn.IsCarryingPawn(Takee));
            yield return goToTakee;
            yield return startCarrying;
            yield return goToBed;
            yield return Toils_General.Do(() =>
            {
                if (Takee.playerSettings == null)
                    Takee.playerSettings = new Pawn_PlayerSettings(Takee);
            });
            yield return Toils_Reserve.Release(BedIndex);
            // rescued: false - the flag only feeds Notify_RescuedBy, which is gated on a
            // humanlike rescuer and would be inert for the mech anyway.
            yield return Toils_Bed.TuckIntoBed(DropBed, pawn, Takee, rescued: false);
        }
    }
}
