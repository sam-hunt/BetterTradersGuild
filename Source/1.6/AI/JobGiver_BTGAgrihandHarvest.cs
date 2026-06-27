using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Agrihand-mech harvesting: cut every mature food plant growing in a hydroponics
    // basin within the mech's farm area (moderate radius around its anchor AND inside
    // the settlement structure footprint - see FarmArea).
    //
    // Builds a single vanilla JobDefOf.Harvest job whose TargetA queue is the nearest
    // in-range reachable plant plus the cluster around it (capped, nearest-first) - the
    // same shape JobDriver_PlantHarvest consumes, so one job clears a batch before the
    // duty tree re-evaluates. The vanilla harvest toil drops the yield Near the mech and
    // - because the mech is not the player faction - forbids it on drop automatically
    // (JobDriver_PlantWork), so the haul giver can shelve it without any extra forbid step.
    //
    // Only food plants are harvested (harvestedThingDef is a nutrition-giving ingestible),
    // so a decorative pot plant or a medical-bay healroot basin that happens to sit inside
    // the radius is left alone. When no qualifying plant remains this returns null and the
    // haul / sow / standby nodes take over.
    public class JobGiver_BTGAgrihandHarvest : ThinkNode_JobGiver
    {
        // Same per-job cap idea as vanilla WorkGiver_GrowerHarvest / our clean giver:
        // clear a batch, then re-walk the tree rather than queueing an unbounded sweep.
        private const int MaxQueuedPlants = 15;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
                return null;

            IntVec3 anchor = FarmArea.GetAnchor(pawn);
            if (!anchor.IsValid)
                return null;

            List<Thing> basins = map.listerThings.ThingsOfDef(Things.HydroponicsBasin);
            if (basins.Count == 0)
                return null;

            // Cheap first pass: gather mature, harvestable food plants whose cell is in
            // range. No reservation/reachability cost yet. Basin counts are small (a
            // handful of clusters), and each basin holds at most a few plant cells.
            List<Plant> candidates = null;
            for (int i = 0; i < basins.Count; i++)
            {
                if (!(basins[i] is Building_PlantGrower grower))
                    continue;

                foreach (Plant plant in grower.PlantsOnMe)
                {
                    if (plant.LifeStage != PlantLifeStage.Mature || !plant.HarvestableNow || !plant.CanYieldNow())
                        continue;
                    ThingDef yield = plant.def.plant.harvestedThingDef;
                    if (yield == null || !yield.IsNutritionGivingIngestible)
                        continue;
                    if (!FarmArea.Contains(map, anchor, plant.Position))
                        continue;
                    (candidates ?? (candidates = new List<Plant>())).Add(plant);
                }
            }
            if (candidates == null)
                return null;

            // Nearest-first: the closest reachable plant becomes the primary target and
            // the queued batch stays tight around the mech.
            IntVec3 pos = pawn.Position;
            candidates.Sort((a, b) => a.Position.DistanceToSquared(pos).CompareTo(b.Position.DistanceToSquared(pos)));

            Job job = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                Plant plant = candidates[i];
                if (!pawn.CanReserveAndReach(plant, PathEndMode.Touch, Danger.Deadly))
                    continue;

                if (job == null)
                    job = JobMaker.MakeJob(JobDefOf.Harvest);
                job.AddQueuedTarget(TargetIndex.A, plant);

                if (job.GetTargetQueue(TargetIndex.A).Count >= MaxQueuedPlants)
                    break;
            }
            return job;
        }
    }
}
