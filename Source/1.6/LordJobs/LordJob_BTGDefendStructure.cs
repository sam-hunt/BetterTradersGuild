using System.Collections.Generic;
using BetterTradersGuild.AI;
using BetterTradersGuild.AI.Civilians;
using BetterTradersGuild.LordJobs.Civilians;
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
    // While the settlement stands, the single LordToil_BTGDefendStructure
    // assigns Duties.BTG_DefendStructure; combat containment, hunger handling,
    // self-tending, and idle wandering all come from the duty's think tree.
    // There is no assault state — the only transitions are the post-defeat
    // abandon-ship chain (see CreateGraph).
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

        // Transition triggers are evaluated on every Tick signal; none of the
        // conditions below needs sub-second latency, so the real checks only run
        // every N ticks (same idiom as LordJob_BTGShelterCivilians).
        private const int TransitionCheckIntervalTicks = 60;

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();

            LordToil_BTGDefendStructure defend = new LordToil_BTGDefendStructure(baseCenter);
            graph.AddToil(defend);
            graph.StartingToil = defend;

            // Post-defeat abandon-ship chain. Defeat only fires while every garrison
            // human is dead or downed (SettlementDefeatUtilityIsDefeated on settlements,
            // GenHostilityAnyHostileActiveThreatTo on the den), but downed defenders
            // stay in the lord (ShouldRemovePawn below) and the medic mech keeps caring
            // for them, so late recoveries are expected. They must not resume the fight
            // after the defeated letter promised the player safety: instead they ferry
            // reachable faction infants to a launchable and fly off (reusing the
            // civilian escape toil, whose LordToilTick drives the actual lift-off), or
            // settle into a peaceful stranded routine when no launchable is reachable.
            // Both threat patches that key on this LordJob disarm themselves at the
            // moment of defeat (the settlement patch's map-parent check fails once
            // vanilla reparents the map to DestroyedSettlement; the den patch
            // early-outs on the site's latched defeat signal), so abandon-phase pawns
            // never read as garrison.
            LordToil_BTGEscape escape = new LordToil_BTGEscape(baseCenter);
            graph.AddToil(escape);

            LordToil_BTGStrandedDefender stranded = new LordToil_BTGStrandedDefender(baseCenter);
            graph.AddToil(stranded);

            // Defeat detection is engine state, not a hook: settlements reparent the
            // map to a DestroyedSettlement in the same call that destroys the
            // Settlement, and the den's Site latches its all-enemies-defeated signal.
            // Both are scribed, so polling (TradersGuildHelper.IsPostDefeatMap) needs
            // no defeat-time event and survives a save made after the defeat.
            Transition abandon = new Transition(defend, escape);
            abandon.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck() && MapDefeated()));
            abandon.AddPostAction(new TransitionAction_WakeAll());
            abandon.AddPostAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(abandon);

            // Escape <-> stranded on launchable reachability, mirroring the civilian
            // lord (reachability can return: a door gets hacked open, a blocking fire
            // burns out), minus its shelter-door special cases - defenders start free.
            Transition toStranded = new Transition(escape, stranded);
            toStranded.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck()
                && !LaunchableEscapeHelper.AnyLaunchableReachable(lord.ownedPawns, Map)));
            toStranded.AddPostAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(toStranded);

            Transition toEscapeAgain = new Transition(stranded, escape);
            toEscapeAgain.AddTrigger(new Trigger_Custom(signal =>
                signal.type == TriggerSignalType.Tick && DueForCheck()
                && LaunchableEscapeHelper.AnyLaunchableReachable(lord.ownedPawns, Map)));
            toEscapeAgain.AddPostAction(new TransitionAction_WakeAll());
            toEscapeAgain.AddPostAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(toEscapeAgain);

            return graph;
        }

        private static bool DueForCheck()
        {
            return Find.TickManager.TicksGame % TransitionCheckIntervalTicks == 0;
        }

        private bool MapDefeated()
        {
            return TradersGuildHelper.IsPostDefeatMap(lord?.Map);
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
