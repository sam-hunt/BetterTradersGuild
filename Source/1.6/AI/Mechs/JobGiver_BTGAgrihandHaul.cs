using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Agrihand-mech hauling: move freshly-harvested produce off the greenhouse floor onto a
    // shelf. The produce is picked up only from the mech's own room (the greenhouse - see
    // FarmArea), but the destination shelf may be anywhere reachable in the settlement
    // structure (StructureBoundsCache), not just the greenhouse. A greenhouse with no shelf
    // of its own would otherwise leave produce stranded on the floor and the haul giver
    // would rescan it fruitlessly on every standby recheck; storing it in a shelf elsewhere
    // in the structure is an acceptable outcome.
    //
    // Emits a plain vanilla JobDefOf.HaulToCell job. The harvest yield is already
    // forbidden (the harvest giver runs as a non-player faction, which forbids on drop),
    // and JobDriver_HaulToCell preserves that: it records forbiddenInitially in
    // Notify_Starting, skips its FailOnForbidden(A) for an initially-forbidden item, and
    // places it with ThingPlaceMode.Direct without ever clearing the flag - so the produce
    // arrives on the shelf still forbidden, no custom driver required.
    //
    // Both searches are nearest-first: the closest loose produce in the room is carried to
    // the closest accepting shelf slot in the structure. (The harvest toil drops yield Near
    // the mech, so the produce is usually right where the mech is already standing.)
    // Already-stored produce is skipped (IsInValidStorage), so once a stack is shelved - in
    // the greenhouse or elsewhere - it is never re-selected and the job stops recurring on
    // it: a shelved stack is both in valid storage and (if shelved in another room) outside
    // the pickup room, so it fails the candidate scan twice over.
    //
    // If no shelf with a free, accepting slot can be found anywhere reachable in the
    // structure, this returns null and the produce is left where it fell (already forbidden).
    public class JobGiver_BTGAgrihandHaul : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
                return null;

            List<CellRect> rects = FarmArea.GetRects(pawn);
            if (rects == null)
                return null;

            Thing produce = FindLooseProduce(pawn, map, rects);
            if (produce == null)
                return null;

            if (!TryFindShelfCell(pawn, map, produce, out IntVec3 storeCell))
                return null; // No accepting shelf anywhere in the structure: leave it on the floor (already forbidden).

            Job job = JobMaker.MakeJob(JobDefOf.HaulToCell, produce, storeCell);
            job.count = produce.stackCount;
            job.haulMode = HaulMode.ToCellStorage;
            return job;
        }

        // Nearest reachable raw plant produce sitting on the floor (not already shelved)
        // anywhere in the mech's room. Restricting to the PlantFoodRaw category (rather
        // than "any nutrition-giving ingestible") keeps it to harvested crops - raw
        // rice/potatoes/corn - and never matches corpses (Corpses), butchered meat
        // (MeatRaw), or packaged meals (FoodMeals) that a downed pawn might drop nearby,
        // since those live in other food categories. IsWithinCategory walks the ancestor
        // chain, so any future raw crop is covered automatically. IsInValidStorage prunes
        // anything already sitting in an accepting shelf slot, so produce the mech (or
        // anyone) has already shelved is never picked back up.
        private static Thing FindLooseProduce(Pawn pawn, Map map, List<CellRect> rects)
        {
            List<Thing> haulables = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
            IntVec3 pos = pawn.Position;
            Thing best = null;
            int bestDistSq = int.MaxValue;
            for (int i = 0; i < haulables.Count; i++)
            {
                Thing t = haulables[i];
                if (!t.def.IsWithinCategory(ThingCategoryDefOf.PlantFoodRaw))
                    continue;
                if (!FarmArea.Contains(rects, t.Position))
                    continue;
                if (t.IsInValidStorage())
                    continue;
                if (!pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                    continue;

                int distSq = (t.Position - pos).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = t;
                }
            }
            return best;
        }

        // Nearest shelf slot cell anywhere in the structure that can accept and be reserved
        // for the produce. Bounded to the structure footprint (not the pickup room) so the
        // mech can always find storage somewhere in the settlement rather than stranding
        // produce when its own greenhouse has no free shelf; CanReach gates whether a far
        // shelf is actually walkable from here.
        private static bool TryFindShelfCell(Pawn pawn, Map map, Thing produce, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            IntVec3 pos = pawn.Position;
            int bestDistSq = int.MaxValue;

            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                if (!(buildings[i] is Building_Storage shelf))
                    continue;
                if (!StructureBoundsCache.Contains(map, shelf.Position))
                    continue;

                foreach (IntVec3 c in shelf.AllSlotCellsList())
                {
                    if (!StructureBoundsCache.Contains(map, c))
                        continue;
                    if (!StoreUtility.IsValidStorageFor(c, map, produce))
                        continue;
                    if (!pawn.CanReserve(c) || !pawn.CanReach(c, PathEndMode.ClosestTouch, Danger.Deadly))
                        continue;

                    int distSq = (c - pos).LengthHorizontalSquared;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        cell = c;
                    }
                }
            }
            return cell.IsValid;
        }
    }
}
