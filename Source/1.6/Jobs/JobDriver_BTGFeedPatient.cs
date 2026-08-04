using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Paramedic-mech patient feeding driver. Vanilla JobDriver_FoodFeedPatient with one
    // change: the global fail condition. Vanilla opens MakeNewToils with
    // FailOn(!FoodUtility.ShouldBeFedBySomeone(Deliveree)), and both branches of that
    // check assume a player-side patient - FeedPatientUtility.ShouldBeFed requires
    // Faction.OfPlayer or HostFaction == player (plus InBed), WardenFeedUtility.ShouldBeFed
    // requires IsPrisonerOfColony. A downed TG defender fails all of them unconditionally,
    // so the vanilla job ends Incompletable on its first tick, releases its reservations,
    // and the medic duty's think tree immediately re-issues it - the "started 10 jobs in
    // one tick" loop, on every paramedic in the medbay at once. Same bug class as
    // BTG_Rescue vs vanilla TakeToBed: the player assumption hides in a fail condition,
    // not the toils.
    //
    // The replacement condition keeps only the faction-neutral parts that can change
    // mid-job: the patient must still be downed (a recovered defender stands up and
    // self-feeds via their duty's forage node) and must still have a food need. Death and
    // despawn are covered by FailOnDespawnedNullOrForbidden(B). Patients on the medbay
    // floor are deliberately fed too - vanilla's InBed gate is part of what we're removing.
    //
    // Kept from vanilla (inherited): TryMakePreToilReservations - patient reserved with
    // maxPawns 1 (the guard against two paramedics feeding the same patient) and the food
    // stack-reserved unless it is a dispenser or already in the feeder's inventory - and
    // GetReport. The toil sequence below is vanilla's verbatim, including the
    // food-in-another-pawn's-inventory detour (CheckItemCarriedByOtherPawn fills TargetC
    // with the holder; TakeFromOtherInventory transfers it), which is what lets the medic
    // feed a patient their own carried rations. Only the anomaly metalhorror finish action
    // is dropped: it is gated on the FEEDER being infected, which a mech never is.
    public class JobDriver_BTGFeedPatient : JobDriver_FoodFeedPatient
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => !Deliveree.Downed || Deliveree.needs?.food == null);
            Toil carryFoodFromInventory = Toils_Misc.TakeItemFromInventoryToCarrier(pawn, TargetIndex.A);
            Toil goToNutrientDispenser = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnForbidden(TargetIndex.A);
            Toil goToFoodHolder = Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch).FailOn(() => FoodHolder != FoodHolderInventory?.pawn || FoodHolder.IsForbidden(pawn));
            Toil carryFoodToPatient = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_Jump.JumpIf(carryFoodFromInventory, () => pawn.inventory?.Contains(TargetThingA) == true);
            yield return Toils_Haul.CheckItemCarriedByOtherPawn(Food, TargetIndex.C, goToFoodHolder);
            yield return Toils_Jump.JumpIf(goToNutrientDispenser, () => TargetThingA is Building_NutrientPasteDispenser);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnForbidden(TargetIndex.A);
            yield return Toils_Ingest.PickupIngestible(TargetIndex.A, Deliveree);
            yield return Toils_Jump.Jump(carryFoodToPatient);
            yield return goToFoodHolder;
            yield return Toils_General.Wait(25).WithProgressBarToilDelay(TargetIndex.C);
            yield return Toils_Haul.TakeFromOtherInventory(Food, pawn.inventory.innerContainer, FoodHolderInventory?.innerContainer, job.count, TargetIndex.A);
            yield return carryFoodFromInventory;
            yield return Toils_Jump.Jump(carryFoodToPatient);
            yield return goToNutrientDispenser;
            yield return Toils_Ingest.TakeMealFromDispenser(TargetIndex.A, pawn);
            yield return carryFoodToPatient;
            yield return Toils_Ingest.ChewIngestible(Deliveree, 1.5f, TargetIndex.A).FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_Ingest.FinalizeIngest(Deliveree, TargetIndex.A);
        }
    }
}
