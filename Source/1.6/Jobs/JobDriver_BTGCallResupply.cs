using System.Collections.Generic;
using BetterTradersGuild.AI;
using BetterTradersGuild.MapComponents;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Drives a starving defender to a powered in-structure comms console and, on
    // completion, calls in a survival-meal cargo-pod drop (ResupplyDropUtility).
    //
    // Mirrors JobDriver_BTGOpenContainer's designation-free goto -> wait -> effect shape.
    // We can't reuse vanilla JobDriver_UseCommsConsole: its toil opens the player comms
    // dialog (and its float-menu entry needs comm targets + the Talking capacity), none of
    // which an NPC defender has. Here the console is purely a powered waypoint - the real
    // work is spawning the drop.
    //
    // Concurrency: several consoles can exist (the ControlCenter alone often spawns more
    // than one; Armory/CrewQuarters can too), so multiple starving defenders may call in
    // parallel on different consoles. That's fine and first-come-first-served by COMPLETION,
    // not initiation: only the first caller to finish the call re-checks the cooldown, finds
    // it clear, spawns the drop, and records the cooldown. Later finishers (possibly closer
    // pawns that started later) re-check, find the cooldown now active, and abort silently.
    // The check-then-record is safe without locking because toil initActions never run
    // concurrently (single-threaded tick loop) - the first to execute claims the slot.
    //
    // TargetA = the console. TargetB = the drop cell scouted by the JobGiver, re-validated
    // at drop time (and re-found if it was blocked while the defender walked over).
    public class JobDriver_BTGCallResupply : JobDriver
    {
        private const TargetIndex ConsoleIndex = TargetIndex.A;
        private const TargetIndex DropCellIndex = TargetIndex.B;
        private const int CallTicks = 600; // ~10s of "radioing for resupply"

        private Building_CommsConsole Console => job.GetTarget(ConsoleIndex).Thing as Building_CommsConsole;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(ConsoleIndex), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(ConsoleIndex);
            this.FailOn(() => Console == null || !Console.CanUseCommsNow);

            yield return Toils_Goto.GotoThing(ConsoleIndex, PathEndMode.InteractionCell)
                .FailOnDespawnedOrNull(ConsoleIndex);

            yield return Toils_General.Wait(CallTicks, ConsoleIndex)
                .WithProgressBarToilDelay(ConsoleIndex)
                .FailOnDespawnedOrNull(ConsoleIndex)
                .FailOnCannotTouch(ConsoleIndex, PathEndMode.InteractionCell)
                .FailOn(() => Console == null || !Console.CanUseCommsNow);

            Toil drop = ToilMaker.MakeToil();
            drop.initAction = () =>
            {
                Map map = pawn.Map;

                // Authoritative first-to-complete gate: a parallel caller that finished first
                // will already have recorded the cooldown, so this later finisher aborts here.
                ResupplyDropTracker tracker = map.GetComponent<ResupplyDropTracker>();
                if (tracker == null || !tracker.CanResupplyNow)
                    return;

                int mealCount = ResupplyDropUtility.MealCountForDefenders(pawn);
                if (mealCount <= 0)
                    return;

                IntVec3 cell = job.GetTarget(DropCellIndex).Cell;
                if (!ResupplyDropUtility.IsCellStillLandable(cell, map)
                    && !ResupplyDropUtility.TryFindDropCell(map, out cell))
                    return; // nowhere to land it now - don't burn the cooldown

                ResupplyDropUtility.SpawnResupplyDrop(map, cell, mealCount, pawn.Faction);
                tracker.RecordResupply();
                Messages.Message("BTG_ResupplyDropArrived".Translate(), new TargetInfo(cell, map), MessageTypeDefOf.NeutralEvent);
            };
            drop.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return drop;
        }
    }
}
