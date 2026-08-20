using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Agrihand-mech decorative planting: fill the settlement's empty plant pots with a
    // random ornamental plant. Unlike the harvest/haul/sow nodes above it - which are
    // confined to the mech's own greenhouse room (FarmArea) - this ranges across the whole
    // structure footprint (StructureBoundsCache rect union, the cached room-rect union, not
    // the structure bounding box), so a single agrihand keeps the flower pots dotted through
    // the crew quarters, mess hall, rec room, etc. in bloom, not just the ones in the
    // greenhouse. It sits below the food work so crops always come first.
    //
    // Emits a plain vanilla JobDefOf.Sow job (one pot at a time - vanilla sowing has no
    // target queue) for the nearest reachable empty pot. The sown species is a random plant
    // the pot can actually grow: PlantUtility.CanSowOnGrower matches the plant's sowTags to
    // the pot's sowTag, the same check the vanilla "set plant to grow" gizmo uses, so it
    // covers modded pots and modded flowers, not just vanilla roses/daylilies. Pre-filters
    // with the exact gates JobDriver_PlantSow fails on (CanNowPlantAt, no adjacent sow
    // blocker) so the job can never spawn only to instantly abort. Pots in rooms with any
    // vacuum are skipped outright - the sown plant would die immediately, trapping the mech
    // in a replant loop.
    //
    // Returns null when no reachable empty pot remains, letting the return-home node walk
    // the mech back to its greenhouse before it self-charges.
    public class JobGiver_BTGAgrihandPlantPots : ThinkNode_JobGiver
    {
        // Every sowable plant def, resolved once per play-data load (the def instances are
        // replaced by an in-process reload, so BTGStartup.Run invalidates this). Filtered
        // down to what a given pot can grow per-pot via CanSowOnGrower.
        private static List<ThingDef> sowablePlants;

        // Drops the cached ThingDef instances so the next use rebuilds from the current
        // DefDatabase. Called once per play-data load from BTGStartup.Run().
        public static void InvalidateCache()
        {
            sowablePlants = null;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
                return null;

            List<CellRect> rects = StructureBoundsCache.GetRoomRects(map);
            if (rects == null)
                return null; // no layout bounds known - don't run an unbounded pot search

            List<Thing> pots = map.listerThings.ThingsOfDef(Things.PlantPot);
            if (pots.Count == 0)
                return null;

            IntVec3 pos = pawn.Position;
            IntVec3 bestCell = IntVec3.Invalid;
            ThingDef bestPlant = null;
            int bestDistSq = int.MaxValue;

            for (int i = 0; i < pots.Count; i++)
            {
                if (!(pots[i] is Building_PlantGrower pot) || !pot.CanAcceptSowNow())
                    continue;
                if (!StructureBoundsCache.Contains(map, pot.Position))
                    continue;
                if (pot.Position.GetVacuum(map) > 0f)
                    continue; // any vacuum kills the plant on spawn - sowing here just loops plant-die-replant

                foreach (IntVec3 c in pot.OccupiedRect())
                {
                    if (c.GetPlant(map) != null)
                        continue; // pot already has a plant
                    int distSq = (c - pos).LengthHorizontalSquared;
                    if (distSq >= bestDistSq)
                        continue; // a nearer pot already wins - skip the expensive checks
                    if (!pawn.CanReserveAndReach(c, PathEndMode.Touch, Danger.Deadly))
                        continue;

                    ThingDef plant = ChoosePlant(pot, c, map);
                    if (plant == null)
                        continue;

                    bestDistSq = distSq;
                    bestCell = c;
                    bestPlant = plant;
                }
            }

            if (!bestCell.IsValid)
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.Sow, bestCell);
            job.plantDefToSow = bestPlant;
            return job;
        }

        // A random plant this pot can grow that can also be planted in the cell right now.
        // Filtered by the same gates JobDriver_PlantSow enforces so the job never aborts.
        private static ThingDef ChoosePlant(Building_PlantGrower pot, IntVec3 cell, Map map)
        {
            List<ThingDef> candidates = null;
            List<ThingDef> plants = SowablePlants;
            for (int i = 0; i < plants.Count; i++)
            {
                ThingDef plant = plants[i];
                if (!PlantUtility.CanSowOnGrower(plant, pot))
                    continue;
                if (!plant.CanNowPlantAt(cell, map))
                    continue;
                if (PlantUtility.AdjacentSowBlocker(plant, cell, map) != null)
                    continue;
                (candidates ?? (candidates = new List<ThingDef>())).Add(plant);
            }
            return candidates?.RandomElement();
        }

        private static List<ThingDef> SowablePlants
        {
            get
            {
                if (sowablePlants == null)
                {
                    sowablePlants = new List<ThingDef>();
                    List<ThingDef> all = DefDatabase<ThingDef>.AllDefsListForReading;
                    for (int i = 0; i < all.Count; i++)
                    {
                        ThingDef def = all[i];
                        if (def.plant?.Sowable == true)
                            sowablePlants.Add(def);
                    }
                }
                return sowablePlants;
            }
        }
    }
}
