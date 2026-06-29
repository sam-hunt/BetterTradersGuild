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
    //   Shelter ──[anyone urgently hungry: the locked shelter's food is gone]──▶ Escape
    //   Escape  ──[no launchable exists anywhere in the structure]────────────▶ Stranded
    //
    //   * Shelter: caretaker holds the locked subroom (fights intruders, tends babies),
    //     everyone eats/sleeps/wanders in-room.
    //   * Escape: walkers hack the door, carry infants into the best launchable (shuttle
    //     preferred, then pods), and board; LordToil_BTGEscape flies each loaded craft off.
    //     When the last walker boards, the lord empties and is cleaned up automatically.
    //   * Stranded ("given up"): no escape route, so forage the wider structure / call a
    //     resupply / wander the wider nursery.
    //
    // The escape trigger counts the autonomous babies too, matching the design that the family
    // bolts the moment ANY of them (including the infants) goes urgently hungry.
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
                signal.type == TriggerSignalType.Tick && DueForCheck() && AnyUrgentlyHungry()));
            graph.AddTransition(toEscape);

            Transition toStranded = new Transition(escape, stranded);
            toStranded.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck() && !LaunchableEscapeHelper.AnyLaunchable(Map)));
            graph.AddTransition(toStranded);

            return graph;
        }

        // Cheap tick-rate gate so the per-tick triggers only run their scans periodically.
        private static bool DueForCheck()
        {
            return Find.TickManager.TicksGame % TransitionCheckIntervalTicks == 0;
        }

        // True if any lord walker, or any sheltered (autonomous) infant of the family, is at
        // least urgently hungry - the cue that the locked shelter's food is exhausted.
        private bool AnyUrgentlyHungry()
        {
            Map map = Map;
            if (map == null)
                return false;

            List<Pawn> owned = lord.ownedPawns;
            for (int i = 0; i < owned.Count; i++)
            {
                if (IsUrgentlyHungry(owned[i]))
                    return true;
            }

            List<Pawn> facPawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < facPawns.Count; i++)
            {
                Pawn p = facPawns[i];
                if ((p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Newborn()) && IsUrgentlyHungry(p))
                    return true;
            }
            return false;
        }

        private static bool IsUrgentlyHungry(Pawn p)
        {
            Need_Food food = p?.needs?.food;
            return food != null && (int)food.CurCategory >= (int)HungerCategory.UrgentlyHungry;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref subroomCenter, "subroomCenter");
        }
    }
}
