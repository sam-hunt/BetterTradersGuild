using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Caretaker behaviour: put any loose or downed infant/baby back into a free crib. Scans
    // the caretaker's own faction for spawned babies/newborns that are NOT currently in a bed
    // (crawled out, or knocked down on the floor), confirms a crib is actually available
    // (ChildcareUtility.SafePlaceForBaby resolves to a Building_Bed), and issues the vanilla
    // bring-to-safety job, which carries the baby and tucks it in correctly.
    //
    // Only acts when a crib is free, so it never hauls a baby to a random floor spot; no-op
    // when the caretaker can't manipulate or can't haul the baby. Used by the shelter and
    // stranded caretaker duties.
    public class JobGiver_BTGTuckBabyInCrib : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            Pawn best = null;
            float bestDistSq = float.MaxValue;
            List<Pawn> facPawns = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
            for (int i = 0; i < facPawns.Count; i++)
            {
                Pawn baby = facPawns[i];
                if (baby == pawn)
                    continue;
                if (!(baby.DevelopmentalStage.Baby() || baby.DevelopmentalStage.Newborn()))
                    continue;
                // Already resting in a bed/crib: leave it be.
                if (baby.CurrentBed() != null)
                    continue;
                if (!ChildcareUtility.CanHaulBabyNow(pawn, baby, ignoreOtherReservations: false, out _))
                    continue;
                // Only tuck when an actual crib is free; otherwise don't haul it nowhere useful.
                LocalTargetInfo safe = ChildcareUtility.SafePlaceForBaby(baby, pawn);
                if (!safe.HasThing || !(safe.Thing is Building_Bed))
                    continue;

                float distSq = (pawn.Position - baby.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = baby;
                }
            }

            return best != null ? ChildcareUtility.MakeBringBabyToSafetyJob(pawn, best) : null;
        }
    }
}
