using System.Collections.Generic;
using BetterTradersGuild.AI;
using BetterTradersGuild.MapComponents;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.JobDrivers
{
    // Drives a starving defender to a powered in-structure comms console and, on
    // completion, calls in a survival-meal cargo-pod drop (ResupplyDropUtility) and,
    // optionally, a drop-pod reinforcement raid (ResupplyRaidUtility).
    //
    // Mirrors JobDriver_BTGOpenContainer's designation-free goto -> wait -> effect shape.
    // We can't reuse vanilla JobDriver_UseCommsConsole: its toil opens the player comms
    // dialog (and its float-menu entry needs comm targets + the Talking capacity), none of
    // which an NPC defender has. Here the console is purely a powered waypoint - the real
    // work is spawning the drop and calling in reinforcements.
    //
    // Concurrency: several consoles can exist (the ControlCenter alone often spawns more
    // than one; Armory/CrewQuarters can too), so multiple starving defenders may call in
    // parallel on different consoles. That's fine and first-come-first-served by COMPLETION,
    // not initiation: only the first caller to finish the call re-checks the cooldown, finds
    // it clear, attempts the drop and/or raid, and records the cooldown regardless of
    // whether either actually fired - the call itself completed, and that's what the
    // cooldown tracks (see the completion toil below). Later finishers (possibly closer
    // pawns that started later) re-check, find the cooldown now active, and abort silently.
    // The check-then-record is safe without locking because toil initActions never run
    // concurrently (single-threaded tick loop) - the first to execute claims the slot.
    //
    // TargetA = the console. TargetB = the drop cell scouted by the JobGiver, re-validated
    // at drop time (and re-found if it was blocked while the defender walked over) - or
    // IntVec3.Invalid if the JobGiver found none and only the raid made the call worthwhile.
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
            this.FailOn(() => Console?.CanUseCommsNow != true);

            yield return Toils_Goto.GotoThing(ConsoleIndex, PathEndMode.InteractionCell)
                .FailOnDespawnedOrNull(ConsoleIndex);

            yield return Toils_General.Wait(CallTicks, ConsoleIndex)
                .WithProgressBarToilDelay(ConsoleIndex)
                .FailOnDespawnedOrNull(ConsoleIndex)
                .FailOnCannotTouch(ConsoleIndex, PathEndMode.InteractionCell)
                .FailOn(() => Console?.CanUseCommsNow != true);

            Toil drop = ToilMaker.MakeToil();
            drop.initAction = () =>
            {
                Map map = pawn.Map;

                // Authoritative first-to-complete gate: a parallel caller that finished first
                // will already have recorded the cooldown, so this later finisher aborts here.
                ResupplyDropTracker tracker = map.GetComponent<ResupplyDropTracker>();
                if (tracker?.CanResupplyNow != true)
                    return;

                // The garrison's off-map network (guild settlements or, at the smugglers
                // den, the Salvagers faction) may have been wiped out while the defender
                // walked over: no one is left to answer, so nothing fires and no cooldown
                // is burned (the JobGiver stops re-issuing the call anyway).
                if (!TradersGuildHelper.ResupplyNetworkExists(map))
                    return;

                // The meal drop and the reinforcement raid are independent outcomes of the
                // one call: neither failing (or being unavailable) skips the other. The
                // cooldown is recorded unconditionally once the call completes - the fiction
                // is that the call itself went out and the guild's logistics answer on their
                // own schedule, so even a double no-op (blocked cell, raid disabled) still
                // burns the cooldown rather than leaving the still-starving defender to
                // re-acquire this same job forever.
                bool mealsDropped = TryDropMeals(map, out IntVec3 dropCell);
                bool raidTriggered = ResupplyRaidUtility.TryTriggerReinforcementRaid(map, pawn.Faction);

                // One notification for whichever effect(s) actually fired, anchored on the
                // drop pod if there is one, or the console the call was made from otherwise.
                if (mealsDropped || raidTriggered)
                {
                    TargetInfo target = mealsDropped ? new TargetInfo(dropCell, map) : new TargetInfo(Console.Position, map);
                    Messages.Message("BTG_ResupplyDropArrived".Translate(), target, MessageTypeDefOf.NeutralEvent);
                }

                tracker.RecordResupply();
            };
            drop.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return drop;
        }

        // Drops the survival-meal cargo pod for the current garrison, if there are meals to
        // send and somewhere left to land them. Returns false (and leaves dropCell invalid)
        // otherwise - the raid outcome and the cooldown (recorded by the caller regardless)
        // don't depend on this succeeding.
        private bool TryDropMeals(Map map, out IntVec3 dropCell)
        {
            dropCell = IntVec3.Invalid;

            int mealCount = ResupplyDropUtility.MealCountForDefenders(pawn);
            if (mealCount <= 0)
                return false;

            IntVec3 cell = job.GetTarget(DropCellIndex).Cell;
            if (!ResupplyDropUtility.IsCellStillLandable(cell, map)
                && !ResupplyDropUtility.TryFindDropCell(map, out cell))
                return false; // nowhere to land it now

            ResupplyDropUtility.SpawnResupplyDrop(map, cell, mealCount, pawn.Faction);
            dropCell = cell;
            return true;
        }
    }
}
