using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Agrihand-mech sowing: replant empty hydroponics cells in range once their crop has
    // been harvested. Runs below harvest and haul, so the mech only sows after it has
    // cleared and shelved the mature crop.
    //
    // Emits a plain vanilla JobDefOf.Sow job (one empty cell at a time - vanilla sowing
    // has no target queue) for the nearest reachable empty cell of an in-range basin. The
    // sown species is whatever the basin is configured to grow (GetPlantDefToGrow); the
    // BTG greenhouse sets every basin to rice, whose 3-day grow cycle keeps the
    // harvest/sow loop visibly busy. Pre-filters with the exact gates JobDriver_PlantSow
    // fails on (CanNowPlantAt, no adjacent sow blocker, cell empty of plants) so the job
    // can never spawn only to instantly abort.
    //
    // Returns null when no in-range basin has an empty, plantable, reservable cell, letting
    // the standby node send the mech home to dormant self-charge.
    public class JobGiver_BTGAgrihandSow : ThinkNode_JobGiver
    {
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

            IntVec3 pos = pawn.Position;
            IntVec3 bestCell = IntVec3.Invalid;
            ThingDef bestPlant = null;
            int bestDistSq = int.MaxValue;

            for (int i = 0; i < basins.Count; i++)
            {
                if (!(basins[i] is Building_PlantGrower grower) || !grower.CanAcceptSowNow())
                    continue;
                ThingDef toSow = grower.GetPlantDefToGrow();
                if (toSow == null)
                    continue;

                foreach (IntVec3 c in grower.OccupiedRect())
                {
                    if (!FarmArea.Contains(map, anchor, c))
                        continue;
                    if (c.GetPlant(map) != null)
                        continue; // cell already has a (growing) plant
                    if (!toSow.CanNowPlantAt(c, map))
                        continue;
                    if (PlantUtility.AdjacentSowBlocker(toSow, c, map) != null)
                        continue;
                    if (!pawn.CanReserveAndReach(c, PathEndMode.Touch, Danger.Deadly))
                        continue;

                    int distSq = (c - pos).LengthHorizontalSquared;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestCell = c;
                        bestPlant = toSow;
                    }
                }
            }

            if (!bestCell.IsValid)
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.Sow, bestCell);
            job.plantDefToSow = bestPlant;
            return job;
        }
    }
}
