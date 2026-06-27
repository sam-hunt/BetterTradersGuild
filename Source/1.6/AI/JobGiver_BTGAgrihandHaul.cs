using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Agrihand-mech hauling: move freshly-harvested produce off the greenhouse floor
    // onto the nearest in-range shelf. Both the produce and the destination shelf must
    // lie inside the farm area (moderate radius around the anchor AND the structure
    // footprint - see FarmArea).
    //
    // Emits a plain vanilla JobDefOf.HaulToCell job. The harvest yield is already
    // forbidden (the harvest giver runs as a non-player faction, which forbids on drop),
    // and JobDriver_HaulToCell preserves that: it records forbiddenInitially in
    // Notify_Starting, skips its FailOnForbidden(A) for an initially-forbidden item, and
    // places it with ThingPlaceMode.Direct without ever clearing the flag - so the produce
    // arrives on the shelf still forbidden, no custom driver required.
    //
    // The two searches use different centres on purpose. Pickup is centred tightly on the
    // mech (FarmArea.HaulPickupRadius): the harvest toil drops yield on the mech's own
    // cell, so the mech is always standing on its fresh produce, and a small radius keeps
    // the haul local and cheap. Delivery searches the full farm area for the nearest shelf,
    // so produce can always reach storage somewhere in the greenhouse. Any pile dropped
    // outside the pickup radius (e.g. earlier in a multi-basin harvest) is not stranded:
    // the sow phase revisits those cells and haul, being higher priority, sweeps it then.
    //
    // If no shelf with a free, accepting slot can be found in range, this returns null and
    // the produce is simply left where it fell (already forbidden) - exactly the requested
    // fallback. Already-stored produce is skipped, so the mech never re-hauls a full shelf.
    public class JobGiver_BTGAgrihandHaul : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map == null)
                return null;

            IntVec3 anchor = FarmArea.GetAnchor(pawn);
            if (!anchor.IsValid)
                return null;

            Thing produce = FindLooseProduce(pawn, map);
            if (produce == null)
                return null;

            if (!TryFindShelfCell(pawn, map, anchor, produce, out IntVec3 storeCell))
                return null; // No shelf in range: leave it on the floor (already forbidden).

            Job job = JobMaker.MakeJob(JobDefOf.HaulToCell, produce, storeCell);
            job.count = produce.stackCount;
            job.haulMode = HaulMode.ToCellStorage;
            return job;
        }

        // Nearest reachable raw plant produce sitting on the floor (not already shelved)
        // within the tight pickup radius of the mech. Restricting to the PlantFoodRaw
        // category (rather than "any nutrition-giving ingestible") keeps it to harvested
        // crops - raw rice/potatoes/corn - and never matches corpses (Corpses), butchered
        // meat (MeatRaw), or packaged meals (FoodMeals) that a downed pawn might drop
        // nearby, since those live in other food categories. IsWithinCategory walks the
        // ancestor chain, so any future raw crop is covered automatically.
        private static Thing FindLooseProduce(Pawn pawn, Map map)
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
                if (!FarmArea.WithinRadius(map, pos, t.Position, FarmArea.HaulPickupRadius))
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

        // Nearest in-range shelf slot cell that can accept and be reserved for the produce.
        private static bool TryFindShelfCell(Pawn pawn, Map map, IntVec3 anchor, Thing produce, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            IntVec3 pos = pawn.Position;
            int bestDistSq = int.MaxValue;

            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                if (!(buildings[i] is Building_Storage shelf))
                    continue;
                if (!FarmArea.Contains(map, anchor, shelf.Position))
                    continue;

                foreach (IntVec3 c in shelf.AllSlotCellsList())
                {
                    if (!FarmArea.Contains(map, anchor, c))
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
