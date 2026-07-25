using System.Collections.Generic;
using BetterTradersGuild.AI.Civilians;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // Lord for the non-combatant family sheltering in the Biotech nursery's crib subroom,
    // created (gated on useEntrenchedDefenders) by CivilianLords at map generation. Members
    // are the caretaker and walking children only; infants/babies stay autonomous in their
    // cribs and are tended/carried via faction + developmental-stage scans.
    //
    // Three-phase state graph (see the LordToils and DutyDefs/Civilians/*.xml):
    //
    //   Shelter  ──[a walker starving (the locked shelter's food is gone) OR
    //               the shelter is compromised (hostile inside / breached)]───▶ Escape
    //   Escape   ──[no walker can reach any launchable, and the shelter's own
    //               door is no longer what seals them in]─────────────────────▶ Stranded
    //   Stranded ──[a walker can reach a launchable again]─────────────────────▶ Escape
    //
    //   * Shelter: caretaker holds the locked subroom (tends babies), everyone
    //     eats/sleeps/wanders in-room. NOBODY fights - these are non-combatants, and the
    //     design goal is that the player reads as the clear aggressor: when the shelter is
    //     compromised the family runs for the launchables, leaving intervening (or not) as
    //     the attacker's choice rather than handing them a self-defence justification.
    //   * Escape: walkers hack the door, carry infants into the best launchable (shuttle
    //     preferred, then pods), and board; LordToil_BTGEscape flies each loaded craft off.
    //     When the last walker boards, the lord empties and is cleaned up automatically.
    //   * Stranded ("given up"): no walker can currently reach a launchable, so forage the
    //     wider structure / call a resupply / wander the wider nursery. Re-checks reachability
    //     on the same interval, so a transient blockage (fire, hostiles, a sealed corridor)
    //     re-promotes back to Escape the moment a walker can reach a launchable again, instead
    //     of permanently giving up on a rescuable family.
    //
    // The escape trigger counts the WALKERS only, not the autonomous infants. Walkers self-feed
    // at Hungry (forage giver) and can't sleep from Fed to Starving in a single rest cycle, so
    // a starving walker reliably means the subroom's food is genuinely gone. A starving infant
    // does NOT mean that: the nursery stocks baby food in proportion to the family (so the two
    // supplies exhaust together and walker hunger tracks baby hunger), and an infant can hit
    // Starving from a mere care hiccup - the caretaker asleep through its hungry window, downed,
    // or mid-job - which used to bolt the family while the shelves were still stocked.
    public class LordJob_BTGShelterCivilians : LordJob
    {
        private IntVec3 subroomCenter;

        public LordJob_BTGShelterCivilians()
        {
        }

        public LordJob_BTGShelterCivilians(IntVec3 subroomCenter)
        {
            this.subroomCenter = subroomCenter;
        }

        // The transition triggers below are evaluated on every Tick signal (Lord.LordTick).
        // Neither condition needs sub-second latency - food exhaustion and launchable loss
        // both play out over many ticks - so we only run the actual checks every N ticks.
        private const int TransitionCheckIntervalTicks = 60;

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();

            LordToil_BTGShelter shelter = new LordToil_BTGShelter(subroomCenter);
            graph.AddToil(shelter);
            graph.StartingToil = shelter;

            LordToil_BTGEscape escape = new LordToil_BTGEscape(subroomCenter);
            graph.AddToil(escape);

            LordToil_BTGStranded stranded = new LordToil_BTGStranded(subroomCenter);
            graph.AddToil(stranded);

            // Every transition ends the walkers' in-progress jobs (and wakes sleepers) once the
            // new toil's duties are assigned - think trees only re-evaluate when the current job
            // ends, so without this a pawn mid-sleep or mid-meal carries its old phase's job deep
            // into the new phase (vanilla pairs its lord transitions with these same actions).
            Transition toEscape = new Transition(shelter, escape);
            toEscape.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck()
                && (AnyStarving() || ShelterCompromised())));
            toEscape.AddPostAction(new TransitionAction_WakeAll());
            toEscape.AddPostAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(toEscape);

            // Demote only when the escape has actually failed, not while it is still opening the
            // way out: every escape begins sealed behind the subroom's own locked blast door, and
            // a locked hackable door blocks CanReach even for its own faction, so launchable
            // reachability is ALWAYS false during the door-hack prelude. Without the door guard
            // this demoted the family within one check interval of deciding to escape, into a
            // stranded phase it could never leave - it starved in the locked subroom.
            Transition toStranded = new Transition(escape, stranded);
            toStranded.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck()
                && !AnyWalkerCanReachLaunchable() && !StillOpeningShelterDoor()));
            toStranded.AddPostAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(toStranded);

            // Reachability can come back (fire burns out, a blocking hostile dies or leaves, a
            // corridor gets cleared, the stranded duties' own hack giver opens the door) -
            // re-promote rather than leaving a rescuable family stuck foraging forever.
            // Stranded's duties are just reassigned by Escape's UpdateAllDuties, so re-entering
            // Escape from Stranded is as safe as entering it the first time.
            Transition toEscapeAgain = new Transition(stranded, escape);
            toEscapeAgain.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck() && AnyWalkerCanReachLaunchable()));
            toEscapeAgain.AddPostAction(new TransitionAction_WakeAll());
            toEscapeAgain.AddPostAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(toEscapeAgain);

            return graph;
        }

        // Cheap tick-rate gate so the per-tick triggers only run their scans periodically.
        private static bool DueForCheck()
        {
            return Find.TickManager.TicksGame % TransitionCheckIntervalTicks == 0;
        }

        // True if any living, un-downed lord walker (caretaker or child - the pawns who
        // actually board/carry) can currently reach a launchable. Gates both the escape ->
        // stranded demotion and the stranded -> escape re-promotion, so the lord only gives up
        // when nobody can physically get to a launchable, and tries again the moment someone
        // can - matching the per-pawn reachability LordToil_BTGEscape already uses to pick a
        // walker's target.
        private bool AnyWalkerCanReachLaunchable()
        {
            return LaunchableEscapeHelper.AnyLaunchableReachable(lord.ownedPawns, Map);
        }

        // True while the subroom's own locked blast door still seals the walkers in and one of
        // them could hack it open - i.e. the escape's mandatory first step is still in progress
        // (queued, pathing, or mid-hack), so unreachable launchables are expected, not a failure.
        private bool StillOpeningShelterDoor()
        {
            return ShelterDoorHelper.AnyWalkerCanOpenShelterDoor(lord.ownedPawns, subroomCenter, Map);
        }

        // True when the locked shelter no longer protects the family: a live hostile stands
        // inside the crib subroom itself, or the subroom has been breached open (its room
        // fused into something bigger than any real subroom - a wall hole or a destroyed
        // door). Hostiles merely prowling the wider nursery do NOT count: bolting then would
        // mean opening the family's own blast door into them. Escape cue alongside
        // starvation - the family runs rather than fights (see the class comment).
        private bool ShelterCompromised()
        {
            Map map = Map;
            Faction faction = lord.faction;
            if (map == null || faction == null)
                return false;

            Room room = subroomCenter.GetRoom(map);
            if (room == null)
                return false;

            if (room.CellCount > ShelterDoorHelper.MaxPlausibleSubroomCells)
                return true;

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Downed)
                    continue;
                if (!p.HostileTo(faction))
                    continue;
                if (p.Position.GetRoom(map) == room)
                    return true;
            }
            return false;
        }

        // True if any lord walker is starving - the cue that the locked shelter's food is
        // genuinely exhausted. Starving (not merely urgently hungry) is the threshold on
        // purpose: walkers forage the subroom as soon as they are Hungry, and no single job
        // (even a full sleep) spans the Hungry -> Starving window, so a walker only reaches
        // Starving when there is truly nothing left to eat. The autonomous infants are
        // deliberately not counted here - see the class comment.
        private bool AnyStarving()
        {
            List<Pawn> owned = lord.ownedPawns;
            for (int i = 0; i < owned.Count; i++)
            {
                Need_Food food = owned[i]?.needs?.food;
                if (food != null && (int)food.CurCategory >= (int)HungerCategory.Starving)
                    return true;
            }
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref subroomCenter, "subroomCenter");
        }
    }
}
