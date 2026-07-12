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
    // Three-phase state graph (see the LordToils and DutyDefs/ShelterCivilians.xml):
    //
    //   Shelter  ──[anyone starving: the locked shelter's food is gone]───────▶ Escape
    //   Escape   ──[no walker can reach any launchable]────────────────────────▶ Stranded
    //   Stranded ──[a walker can reach a launchable again]─────────────────────▶ Escape
    //
    //   * Shelter: caretaker holds the locked subroom (fights intruders, tends babies),
    //     everyone eats/sleeps/wanders in-room.
    //   * Escape: walkers hack the door, carry infants into the best launchable (shuttle
    //     preferred, then pods), and board; LordToil_BTGEscape flies each loaded craft off.
    //     When the last walker boards, the lord empties and is cleaned up automatically.
    //   * Stranded ("given up"): no walker can currently reach a launchable, so forage the
    //     wider structure / call a resupply / wander the wider nursery. Re-checks reachability
    //     on the same interval, so a transient blockage (fire, hostiles, a sealed corridor)
    //     re-promotes back to Escape the moment a walker can reach a launchable again, instead
    //     of permanently giving up on a rescuable family.
    //
    // The escape trigger counts the autonomous babies too, matching the design that the family
    // bolts the moment ANY of them (including the infants) starves - late enough that the
    // caretaker's feed loop (which kicks in at Hungry) reliably feeds them first, so escape
    // fires only when the food is genuinely gone rather than on routine baby hunger.
    public class LordJob_BTGShelterCivilians : LordJob
    {
        private Faction faction;
        private IntVec3 subroomCenter;

        public LordJob_BTGShelterCivilians()
        {
        }

        public LordJob_BTGShelterCivilians(Faction faction, IntVec3 subroomCenter)
        {
            this.faction = faction;
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

            Transition toEscape = new Transition(shelter, escape);
            toEscape.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck() && AnyStarving()));
            graph.AddTransition(toEscape);

            Transition toStranded = new Transition(escape, stranded);
            toStranded.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck() && !AnyWalkerCanReachLaunchable()));
            graph.AddTransition(toStranded);

            // Reachability can come back (fire burns out, a blocking hostile dies or leaves, a
            // corridor gets cleared) - re-promote rather than leaving a rescuable family stuck
            // foraging forever. Stranded's duties are just reassigned by Escape's
            // UpdateAllDuties, so re-entering Escape from Stranded is as safe as entering it the
            // first time.
            Transition toEscapeAgain = new Transition(stranded, escape);
            toEscapeAgain.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck() && AnyWalkerCanReachLaunchable()));
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

        // True if any lord walker, or any sheltered (autonomous) infant of the family, is
        // starving - the cue that the locked shelter's food is genuinely exhausted. Starving
        // (not merely urgently hungry) is the threshold on purpose: the caretaker's feed/tend
        // loop activates as soon as a baby is Hungry, so the two-category buffer (Hungry ->
        // UrgentlyHungry -> Starving) lets feeding reliably win, and only a real care breakdown
        // (no baby food, or the caretaker down/overwhelmed) lets a pawn reach Starving.
        private bool AnyStarving()
        {
            Map map = Map;
            if (map == null)
                return false;

            List<Pawn> owned = lord.ownedPawns;
            for (int i = 0; i < owned.Count; i++)
            {
                if (IsStarving(owned[i]))
                    return true;
            }

            List<Pawn> facPawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < facPawns.Count; i++)
            {
                Pawn p = facPawns[i];
                if ((p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Newborn()) && IsStarving(p))
                    return true;
            }
            return false;
        }

        private static bool IsStarving(Pawn p)
        {
            Need_Food food = p?.needs?.food;
            return food != null && (int)food.CurCategory >= (int)HungerCategory.Starving;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref subroomCenter, "subroomCenter");
        }
    }
}
