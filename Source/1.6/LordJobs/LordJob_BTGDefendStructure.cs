using System.Collections.Generic;
using BetterTradersGuild.AI;
using RimWorld;
using Unity.Collections;
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
    //
    // The lord also exposes the structure rect union as its walk grid. The 1.6
    // pathfinder adds costOffLordWalkGrid (+70) to every cell outside a lord's
    // walk grid, so garrison pawns weight the ROUTE of every job they take
    // (combat repositioning, hunting, foraging, rest) hard toward the interior.
    // The duty's JobGivers only bound destinations; without this, the pather
    // happily routes a defender out one perimeter door, across open space, and
    // in another whenever that beats the interior maze — surrendering all cover
    // on the way. Soft (a cost, not a wall) by design: a defender that somehow
    // ends up outside can always path back in.
    public class LordJob_BTGDefendStructure : LordJob
    {
        private Faction faction;
        private IntVec3 baseCenter;

        // Built lazily on first path request and freed in Dispose. Not saved:
        // rebuilt from StructureBoundsCache after load. walkGridBuilt (rather
        // than IsCreated) marks completion so the no-bounds fallback — grid
        // left uncreated, meaning no path penalty — isn't recomputed per call.
        private NativeBitArray walkGrid;
        private bool walkGridBuilt;

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

        // Keep downed defenders in the lord (vanilla default evicts them, permanently:
        // Lord.RemovePawn nulls the duty and MakeUndowned's lord notification then finds
        // no lord, so a recovered defender degrades into a lordless wanderer that never
        // re-arms or fights). Kept, recovery flows natively: MakeUndowned ->
        // Lord.Notify_PawnUndowned -> base LordJob re-runs UpdateAllDuties, which
        // re-issues BTG_DefendStructure. Same pattern as vanilla
        // LordJob_DefendAndExpandHive. Downed pawns can't hold the base open: the
        // defeat/threat patches (SettlementDefeatUtilityIsDefeated,
        // GenHostilityAnyHostileActiveThreatTo) both filter Downed explicitly.
        public override bool ShouldRemovePawn(Pawn p, PawnLostCondition reason)
        {
            if (reason == PawnLostCondition.Incapped)
                return false;
            return base.ShouldRemovePawn(p, reason);
        }

        public override NativeBitArray GetWalkGrid(Pawn pawn)
        {
            if (!walkGridBuilt)
            {
                BuildWalkGrid();
                walkGridBuilt = true;
            }
            return walkGrid;
        }

        private void BuildWalkGrid()
        {
            Map map = lord?.Map;
            if (map == null)
                return;

            List<CellRect> rects = StructureBoundsCache.GetRoomRects(map);
            if (rects == null)
                return; // no bounds known — leave the grid uncreated (no penalty)

            walkGrid = new NativeBitArray(map.cellIndices.NumGridCells, Allocator.Persistent);
            CellIndices indices = map.cellIndices;
            for (int i = 0; i < rects.Count; i++)
            {
                foreach (IntVec3 cell in rects[i].ClipInsideMap(map))
                    walkGrid.Set(indices.CellToIndex(cell), true);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (walkGrid.IsCreated)
                walkGrid.Dispose();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref baseCenter, "baseCenter");
        }
    }
}
