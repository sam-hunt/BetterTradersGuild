using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Pursuit node for the bounded defender duty. The combat node above this one
    // (JobGiver_BTGDefendStructure) only engages targets the pawn can currently
    // see: vanilla JobGiver_AIFightEnemy target acquisition requires line of
    // sight (TargetScanFlags.NeedLOSToPawns), and with chaseTarget=false an
    // acquired target is dropped the moment the fresh scan loses sight of it.
    // Without this node, intruders looting out-of-sight rooms are simply
    // ignored while the garrison drifts back to eating, sleeping, and idling.
    //
    // This node fires when a live hostile stands inside the structure footprint
    // but nothing is visible: the defender paths toward the nearest reachable
    // one (vanilla JobGiver_AIGotoNearestHostile pattern - a Goto with
    // checkOverrideOnExpire, so the think tree re-runs as the pawn closes in
    // and the combat node takes over the moment the target comes into view).
    // Targets outside the structure rect union are invisible to this scan, so
    // defenders sweep their own halls but never pursue out into vacuum.
    //
    // Reachability is interior-only: a region BFS from the hunter, entering
    // only regions inside the rect union, decides which targets are huntable.
    // Plain CanReach would also accept routes that leave through one perimeter
    // door, cross open space, and re-enter through another - and with interior
    // sections sealed behind locked blast doors, such a route is often the
    // only one, marching the defender across vacuum without cover. If a target
    // can't be reached through the halls, the defender stays entrenched.
    // (Route SELECTION for huntable targets is handled by the lord's walk grid
    // in LordJob_BTGDefendStructure, which makes the pather strongly prefer
    // interior cells; this node decides whether to walk at all.)
    public class JobGiver_BTGHuntIntrudersInStructure : ThinkNode_JobGiver
    {
        // Regions reachable from the hunting pawn without leaving the structure
        // rect union, rebuilt per TryGiveJob call and cleared before returning.
        // Think tree evaluation is single-threaded, so a shared scratch set is safe.
        private static readonly HashSet<Region> reachableInterior = new HashSet<Region>();

        // BFS overshoot guard only: a settlement interior is a few dozen chunk
        // regions plus a door region per doorway, far below this.
        private const int MaxInteriorRegions = 2000;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
                return null;

            bool interiorBounded = TryBuildInteriorRegionSet(pawn, map);

            Thing closest = null;
            float closestDistSq = float.MaxValue;

            List<IAttackTarget> targets = map.attackTargetsCache.GetPotentialTargetsFor(pawn);
            for (int i = 0; i < targets.Count; i++)
            {
                IAttackTarget target = targets[i];
                if (target.ThreatDisabled(pawn))
                    continue;
                if (!AttackTargetFinder.IsAutoTargetable(target))
                    continue;

                Thing thing = target.Thing;
                if (!StructureBoundsCache.Contains(map, thing.Position))
                    continue;

                float distSq = thing.Position.DistanceToSquared(pawn.Position);
                if (distSq >= closestDistSq)
                    continue;
                if (interiorBounded
                    ? !ReachableThroughInterior(thing, map)
                    : !pawn.CanReach(thing, PathEndMode.OnCell, Danger.Deadly))
                    continue;

                closestDistSq = distSq;
                closest = thing;
            }

            reachableInterior.Clear();

            if (closest == null)
                return null;

            // Already on top of (or adjacent to) the target: no move to make -
            // the combat node above takes the shot on the next think tick.
            if (closest.PositionHeld == pawn.PositionHeld
                || ReachabilityImmediate.CanReachImmediate(pawn, closest, PathEndMode.Touch))
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.Goto, closest);
            // Distance-scaled expiry with override checks: the tree re-evaluates
            // as the pawn closes in, handing off to the combat node on first
            // sight instead of blindly finishing the walk.
            job.intervalScalingTarget = TargetIndex.A;
            job.checkOverrideOnExpire = true;
            job.expireRequiresEnemiesNearby = true;
            job.collideWithPawns = true;
            return job;
        }

        // Floods reachableInterior with every region the pawn can reach without
        // leaving the structure rect union. Returns false when the interior
        // bound can't be used - no layout bounds known, or the pawn is somehow
        // standing outside them (e.g. previously lured out) - in which case the
        // caller falls back to plain CanReach, matching the permissive
        // no-bounds behavior used throughout the defender AI.
        private static bool TryBuildInteriorRegionSet(Pawn pawn, Map map)
        {
            reachableInterior.Clear();

            if (StructureBoundsCache.GetRoomRects(map) == null)
                return false;

            Region root = pawn.Position.GetRegion(map);
            if (root == null || !StructureBoundsCache.Contains(map, root.AnyCell))
                return false;

            // Regions never straddle the union boundary: interior and exterior
            // walkable cells only connect through perimeter doors (VacBarriers
            // and blast doors), and doors form their own single-cell regions.
            // Testing one member cell therefore classifies the whole region.
            TraverseParms traverseParms = TraverseParms.For(pawn, Danger.Deadly);
            reachableInterior.Add(root);
            RegionTraverser.BreadthFirstTraverse(
                root,
                (Region from, Region to) => to.Allows(traverseParms, isDestination: false)
                    && StructureBoundsCache.Contains(map, to.AnyCell),
                delegate (Region region)
                {
                    reachableInterior.Add(region);
                    return false;
                },
                MaxInteriorRegions);
            return true;
        }

        private static bool ReachableThroughInterior(Thing thing, Map map)
        {
            Region region = thing.Position.GetRegion(map);
            return region != null && reachableInterior.Contains(region);
        }
    }
}
