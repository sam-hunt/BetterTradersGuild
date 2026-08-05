using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Paramedic-mech tending driver. Vanilla JobDriver_TendPatient (whose toils are
    // NPC-safe - every player assumption in it is explicitly gated on
    // Faction.OfPlayer) plus one finish action: stash leftover medicine into the
    // mech's inventory instead of letting job cleanup drop it on the floor.
    //
    // Why: the tend giver sets endAfterTendedOnce, so every single tend ends the job,
    // and Pawn_JobTracker.CleanupCurrentJob drops whatever is in the carry tracker
    // (Toils_Tend.PickupMedicine carries the whole amount needed to fully heal the
    // patient, so there is usually a leftover). The medic then walked back to the
    // shelf for a fresh stack each tend, littering the medbay floor with part-used
    // stacks. Finish actions run in JobDriver.Cleanup BEFORE CleanupCurrentJob's
    // drop check, so moving the medicine to the inventory here prevents the drop
    // without touching the JobDef's carry flags. The giver checks the inventory
    // first on the next pass, and the driver's own CollectMedicineToils jumps
    // straight to the patient for inventory medicine - no shelf round-trip.
    public class JobDriver_BTGTendPatient : JobDriver_TendPatient
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(jobCondition =>
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried?.def.IsMedicine == true)
                    pawn.carryTracker.innerContainer.TryTransferToContainer(carried, pawn.inventory.innerContainer);
            });
            foreach (Toil toil in base.MakeNewToils())
                yield return toil;
        }
    }
}
