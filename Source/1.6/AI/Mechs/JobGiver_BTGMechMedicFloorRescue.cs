using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Mechs
{
    // Paramedic-mech overflow rescue: when every medical bed in the medbay is taken
    // but defenders are still downed elsewhere in the structure, carry them in anyway
    // and lay them on a clear wall-adjacent floor cell in the medbay. There the tend
    // node treats them, the feed node keeps them from starving, and the bed-rescue
    // node (JobGiver_BTGMechMedicRescue) promotes them into a bed as soon as one
    // frees up - instead of leaving them to bleed out wherever they fell.
    //
    // Sits directly below the bed-rescue node, so it only runs when that node gave no
    // job; it still re-checks bed availability itself, because bed-rescue also
    // returns null for reasons that must not divert a casualty to the floor (e.g. the
    // bed exists but is reserved by the other medic). Casualty selection matches
    // bed-rescue (worst-bleed first, structure footprint only) except pawns already
    // inside the medbay are excluded - floor-to-floor shuffling helps nobody.
    public class JobGiver_BTGMechMedicFloorRescue : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            List<CellRect> rects = MedicRoomBounds.GetRects(pawn);
            if (rects == null)
                return null;

            if (JobGiver_BTGMechMedicRescue.FindRoomMedicalBed(pawn, rects) != null)
                return null;

            Pawn patient = JobGiver_BTGMechMedicRescue.FindDownedNeedingBed(pawn, excludeRects: rects);
            if (patient == null)
                return null;

            IntVec3 dropCell = FindDropCell(pawn, rects);
            if (!dropCell.IsValid)
                return null;

            Job job = JobMaker.MakeJob(Jobs.BTG_CarryToMedbayFloor, patient, dropCell);
            job.count = 1;
            return job;
        }

        // A clear wall-adjacent floor cell inside the medbay: MechIdlePark's wall-spot
        // test (standable, no building, against a wall, not flanking a door) plus the
        // casualty-specific vetoes - nobody already lying or standing there, not some
        // building's interaction cell (a body on the bed or charger access spot blocks
        // work on that building), and reservable/reachable by this medic. Nearest to
        // the medic's current position wins - deterministic, and it fills wall spots
        // from one side of the room outward instead of scattering casualties.
        private static IntVec3 FindDropCell(Pawn medic, List<CellRect> rects)
        {
            Map map = medic.Map;
            IntVec3 best = IntVec3.Invalid;
            int bestDist = int.MaxValue;
            for (int i = 0; i < rects.Count; i++)
            {
                foreach (IntVec3 c in rects[i])
                {
                    int dist = (c - medic.Position).LengthHorizontalSquared;
                    if (dist >= bestDist)
                        continue;
                    if (!MechIdlePark.IsWallParkSpot(c, map))
                        continue;
                    if (c.GetFirstPawn(map) != null)
                        continue;
                    if (IsBuildingInteractionCell(c, map))
                        continue;
                    // Most expensive check last, and only for cells that would win.
                    if (!medic.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly))
                        continue;

                    best = c;
                    bestDist = dist;
                }
            }
            return best;
        }

        // Mirrors the interaction-cell veto in vanilla RCellFinder.CanSelfShutdown:
        // reject c when a cardinally adjacent building declares c as its interaction
        // cell (interaction cells always touch their building, so scanning the four
        // neighbours finds every claimant).
        private static bool IsBuildingInteractionCell(IntVec3 c, Map map)
        {
            IntVec3[] dirs = GenAdj.CardinalDirections;
            for (int i = 0; i < dirs.Length; i++)
            {
                IntVec3 n = c + dirs[i];
                if (!n.InBounds(map))
                    continue;
                List<Thing> things = n.GetThingList(map);
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j].def.hasInteractionCell && things[j].InteractionCell == c)
                        return true;
                }
            }
            return false;
        }
    }
}
