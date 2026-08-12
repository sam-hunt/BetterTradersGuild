using System.Collections.Generic;
using BetterTradersGuild.AI;
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
    // (the JobDef keeps the takee in the carry tracker across job restarts), and the
    // drop-on-interrupt finish action.
    //
    // Deliberately NOT kept: vanilla's create-playerSettings-before-tuck step. Vanilla
    // pairs it with guesting the pawn to the player; created standalone it seeds medCare
    // from the player's medical-defaults screen for the takee's faction relation, and a
    // no-care default there made RestUtility.TuckIntoBed's CanUseBedNow check silently
    // refuse the tuck (casualty dumped beside the bed, tended on the floor) and every
    // later rescue of that pawn insta-fail on FailOnBedNoLongerUsable. NPC pawns pass
    // those gates precisely by having playerSettings == null; NpcMedicalCare repairs
    // pawns that already acquired one.
    public class JobDriver_BTGRescue : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;

        private const TargetIndex BedIndex = TargetIndex.B;

        // The weapon the casualty dropped when it went down. Pawn.DeSpawn wipes
        // mindState.droppedWeapon the instant StartCarryThing picks the casualty up,
        // which is what used to strand recovered defenders unarmed: the memory their
        // re-arm node (JobGiver_BTGRecoverWeapon) reads was gone. Stashed here before
        // the carry and restored once the casualty is spawned again. Scribed so a
        // save/load mid-carry keeps it; a job restarted mid-carry (new driver) loses
        // it, and the re-arm node's nearest-weapon fallback covers that.
        private Thing droppedWeapon;

        protected Pawn Takee => (Pawn)job.GetTarget(TakeeIndex).Thing;

        protected Building_Bed DropBed => (Building_Bed)job.GetTarget(BedIndex).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref droppedWeapon, "droppedWeapon");
        }

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
            AddFinishAction(_ =>
            {
                // Runs after the drop action above, so the casualty is spawned again on
                // every exit path (tucked into bed or put down early) - give it back the
                // dropped-weapon memory the carry despawn wiped.
                RestoreDroppedWeaponMemory();
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

            // Repair a takee already carrying playerSettings with medCare == NoCare
            // (created by a pre-fix version of this driver, or by player hosting) before
            // the FailOnBedNoLongerUsable conditions and the tuck's own CanUseBedNow
            // check can see it - both read NoCare as "may not lie in a medical bed".
            yield return Toils_General.Do(() => NpcMedicalCare.EnsureBedRestAllowed(Takee));
            // A re-issued job (interrupt, save/load) skips straight to the bed leg when
            // the casualty is already in the medic's arms.
            yield return Toils_Jump.JumpIf(goToBed, () => pawn.IsCarryingPawn(Takee));
            yield return goToTakee;
            yield return Toils_General.Do(() => droppedWeapon = Takee.mindState?.droppedWeapon);
            yield return startCarrying;
            yield return goToBed;
            yield return Toils_Reserve.Release(BedIndex);
            // rescued: false - the flag only feeds Notify_RescuedBy, which is gated on a
            // humanlike rescuer and would be inert for the mech anyway.
            yield return Toils_Bed.TuckIntoBed(DropBed, pawn, Takee, rescued: false);
        }

        private void RestoreDroppedWeaponMemory()
        {
            Pawn takee = Takee;
            Pawn_MindState mind = takee?.mindState;
            if (mind == null || mind.droppedWeapon != null || droppedWeapon?.Spawned != true)
                return;
            if (takee.Dead || !takee.Spawned || takee.Map != droppedWeapon.Map)
                return;
            mind.droppedWeapon = droppedWeapon;
        }
    }
}
