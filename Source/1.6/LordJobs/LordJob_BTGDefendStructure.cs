using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    // LordJob for TradersGuild settlement defenders. Models a "reverse siege":
    // defenders hold an entrenched position inside the settlement structure
    // and never transition to assault, never path outside the structure
    // footprint to chase intruders. This contrasts with vanilla
    // LordJob_DefendBase, which has seven independent triggers
    // (Trigger_TicksPassed, Trigger_PawnHarmed, Trigger_FractionPawnsLost,
    // Trigger_ChanceOnTickInterval, Trigger_ChanceOnPlayerHarmNPCBuilding,
    // Trigger_UrgentlyHungry, Trigger_OnClamor) that all flip defenders into
    // LordToil_AssaultColony — at which point they break through walls and
    // pursue the player into vacuum, losing the terrain advantage their base
    // was designed to provide.
    //
    // The single LordToil_BTGDefendStructure assigns Duties.BTG_DefendStructure.
    // Combat containment, hunger handling, self-tending, and idle wandering
    // all come from the duty's think tree; the state graph has no transitions.
    public class LordJob_BTGDefendStructure : LordJob
    {
        private Faction faction;
        private IntVec3 baseCenter;

        public LordJob_BTGDefendStructure() { }

        public LordJob_BTGDefendStructure(Faction faction, IntVec3 baseCenter)
        {
            this.faction = faction;
            this.baseCenter = baseCenter;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_BTGDefendStructure(baseCenter));
            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref baseCenter, "baseCenter");
        }
    }
}
