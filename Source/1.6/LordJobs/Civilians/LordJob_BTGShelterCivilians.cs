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
    //               the shelter is compromised (hostile inside / breached /
    //               vacuum / door unsealed) OR a walker can already reach a
    //               launchable (the way out is open, whoever opened it)]──────▶ Escape
    //   Escape   ──[no walker can reach any launchable, and the shelter's own
    //               door is no longer what seals them in]─────────────────────▶ Stranded
    //   Stranded ──[a walker can reach a launchable again]─────────────────────▶ Escape
    //   Escape/Stranded ──[family member actively attacked AND a child walker
    //               still needs covering AND an adult can fight]──────────────▶ Defend
    //   Defend   ──[nobody targets the family anymore, or no child walker is
    //               left to cover: bolt for the craft, even under fire]───────▶ Escape
    //
    //   * Shelter: caretaker holds the locked subroom (tends babies), everyone
    //     eats/sleeps/wanders in-room. Nobody INITIATES combat - these are non-combatants,
    //     and the design goal is that the player reads as the clear aggressor: when the
    //     shelter is compromised the family runs for the launchables, leaving intervening
    //     (or not) as the attacker's choice rather than handing them a self-defence
    //     justification. The one exception is Defend below: strictly reactive cover fire.
    //   * Escape: walkers hack the door, carry infants into the best launchable (shuttle
    //     preferred, then pods), and board; LordToil_BTGEscape flies each loaded craft off.
    //     When the last walker boards, the lord empties and is cleaned up automatically.
    //   * Stranded ("given up"): no walker can currently reach a launchable, so forage the
    //     wider structure / call a resupply / wander the wider nursery. Re-checks reachability
    //     on the same interval, so a transient blockage (fire, hostiles, a sealed corridor)
    //     re-promotes back to Escape the moment a walker can reach a launchable again, instead
    //     of permanently giving up on a rescuable family.
    //   * Defend: reactive self-defense while covering the retreat. Only the adult's duty
    //     changes (melee the nearest pawn actively attacking a family member - see
    //     PartyThreatHelper for the strict involvement rules); children keep escaping and
    //     boarding, and the lift-off tick keeps running. Exits back to Escape the moment
    //     nobody targets the family (attacker neutralized or aggression aborted) or every
    //     child walker is aboard/lost - the caretaker then runs for the craft himself,
    //     collecting any infant he dropped when the fight interrupted a ferry (the carry
    //     giver's map-wide infant scan re-finds it automatically). Defend always exits to
    //     Escape; if the launchables are gone the normal demotion re-sorts to Stranded.
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

            LordToil_BTGDefend defend = new LordToil_BTGDefend(subroomCenter);
            graph.AddToil(defend);

            // Every transition ends the walkers' in-progress jobs (and wakes sleepers) once the
            // new toil's duties are assigned - think trees only re-evaluate when the current job
            // ends, so without this a pawn mid-sleep or mid-meal carries its old phase's job deep
            // into the new phase (vanilla pairs its lord transitions with these same actions).
            // Harm signals bypass the periodic gate: a walker taking external violence IS the
            // shelter failing, whatever the compromise scan thinks - covers e.g. being shot
            // through an open doorway by an attacker who never sets foot in the room.
            // AnyWalkerCanReachLaunchable is the "way out is open" cue: the subroom's own
            // locked blast door blocks CanReach even for its own faction, so while properly
            // sealed in this is ALWAYS false - it flips true precisely when a route to a
            // launchable opens (external door hack, breach elsewhere), whoever opened it.
            Transition toEscape = new Transition(shelter, escape);
            toEscape.AddTrigger(new Trigger_Custom(signal =>
                Trigger_PawnHarmed.SignalIsHarm(signal)
                || (signal.type == TriggerSignalType.Tick && DueForCheck()
                    && (AnyStarving() || ShelterCompromised() || AnyWalkerCanReachLaunchable()))));
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

            // Reactive self-defense. Fires on the periodic scan AND instantly on any harm
            // signal (a lord walker damaged/killed/arrested - SignalIsHarm is vanilla's own
            // harm filter), so the caretaker reacts to the first shot, not up to a second
            // later. ShouldDefend gates both signal paths on the same predicate, and its
            // mirror below is the exit condition, so the posture can't ping-pong: each check
            // interval has exactly one truth.
            Transition toDefend = new Transition(escape, defend);
            toDefend.AddSource(stranded);
            toDefend.AddTrigger(new Trigger_Custom(signal =>
                (Trigger_PawnHarmed.SignalIsHarm(signal)
                    || (signal.type == TriggerSignalType.Tick && DueForCheck()))
                && ShouldDefend()));
            toDefend.AddPostAction(new TransitionAction_WakeAll());
            toDefend.AddPostAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(toDefend);

            // Disengage the moment defense stops making sense: nobody targets the family
            // anymore (neutralized or aborted), no child walker is left to cover (everyone
            // else aboard or lost - bolt for the craft, even under fire), or no adult can
            // fight. Always exits to Escape; if the launchables are gone the normal
            // escape -> stranded demotion re-sorts from there.
            Transition fromDefend = new Transition(defend, escape);
            fromDefend.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck() && !ShouldDefend()));
            fromDefend.AddPostAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(fromDefend);

            return graph;
        }

        // Cheap tick-rate gate so the per-tick triggers only run their scans periodically.
        private static bool DueForCheck()
        {
            return Find.TickManager.TicksGame % TransitionCheckIntervalTicks == 0;
        }

        // True if any living, un-downed lord walker (caretaker or child - the pawns who
        // actually board/carry) can currently reach a launchable. Gates the escape ->
        // stranded demotion and the stranded -> escape re-promotion, so the lord only gives up
        // when nobody can physically get to a launchable, and tries again the moment someone
        // can - matching the per-pawn reachability LordToil_BTGEscape already uses to pick a
        // walker's target. Also an escape cue for the shelter phase itself (see CreateGraph):
        // sealed in, it's always false, so it firing there means the way out has opened.
        private bool AnyWalkerCanReachLaunchable()
        {
            return LaunchableEscapeHelper.AnyLaunchableReachable(lord.ownedPawns, Map);
        }

        // True while fighting back is both warranted and useful: someone is actively attacking
        // a family member, a child walker still needs covering, and an adult walker is up to
        // do the covering. Shared (with its negation) by the enter- and exit-defend
        // transitions so the two can never disagree.
        private bool ShouldDefend()
        {
            return AnyActiveAdultWalker() && AnyChildWalkerStillEvacuating()
                && PartyThreatHelper.AnyPartyMemberTargeted(lord);
        }

        private bool AnyActiveAdultWalker()
        {
            List<Pawn> owned = lord.ownedPawns;
            for (int i = 0; i < owned.Count; i++)
            {
                Pawn p = owned[i];
                if (p?.Dead == false && !p.Downed && p.Spawned && p.DevelopmentalStage.Adult())
                    return true;
            }
            return false;
        }

        // True while some child walker is alive, un-downed, and still on the map (a walker
        // aboard a launchable is despawned into its container, so Spawned doubles as the
        // not-yet-boarded test). Downed children deliberately don't count - craft never wait
        // for downed walkers, so the caretaker shouldn't hold a defense for one either.
        private bool AnyChildWalkerStillEvacuating()
        {
            List<Pawn> owned = lord.ownedPawns;
            for (int i = 0; i < owned.Count; i++)
            {
                Pawn p = owned[i];
                if (p?.Dead == false && !p.Downed && p.Spawned && !p.DevelopmentalStage.Adult())
                    return true;
            }
            return false;
        }

        // True while the subroom's own locked blast door still seals the walkers in and one of
        // them could hack it open - i.e. the escape's mandatory first step is still in progress
        // (queued, pathing, or mid-hack), so unreachable launchables are expected, not a failure.
        private bool StillOpeningShelterDoor()
        {
            return ShelterDoorHelper.AnyWalkerCanOpenShelterDoor(lord.ownedPawns, subroomCenter, Map);
        }

        // True when the locked shelter no longer protects the family: a live hostile stands
        // inside the crib subroom itself, the subroom has been breached open (its room
        // fused into something bigger than any real subroom - a wall hole or a destroyed
        // door), or someone is actively drawing a bead on a family member (catches an
        // attacker aiming in through an open doorway before the first shot lands; the
        // harm-signal trigger covers anything this scan misses). Hostiles merely prowling
        // the wider nursery do NOT count: bolting then would mean opening the family's own
        // blast door into them. Escape cue alongside starvation - the family runs rather
        // than fights (see the class comment).
        private bool ShelterCompromised()
        {
            if (PartyThreatHelper.AnyPartyMemberTargeted(lord))
                return true;

            Map map = Map;
            Faction faction = lord.faction;
            if (map == null || faction == null)
                return false;

            Room room = subroomCenter.GetRoom(map);
            if (room == null)
                return false;

            if (room.CellCount > ShelterDoorHelper.MaxPlausibleSubroomCells)
                return true;

            // Any vacuum at all means the pressure seal has failed (a hull nick too small to
            // fuse rooms still leaks). The infants have no vacuum resistance, so waiting out
            // the leak kills them long before the walkers feel it - bolt immediately.
            if (room.Vacuum > 0f)
                return true;

            // An unlocked shelter door is always an external actor's doing (no shelter-phase
            // duty touches the door), and an unsealed shelter protects nobody - the family
            // runs rather than sitting behind an open door until starvation.
            if (ShelterDoorHelper.AnyShelterDoorUnlocked(subroomCenter, map))
                return true;

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.Downed != false)
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
