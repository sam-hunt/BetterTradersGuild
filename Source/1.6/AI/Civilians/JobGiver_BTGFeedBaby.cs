using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Caretaker behaviour: bottle-feed a hungry infant/baby with the nursery's baby food.
    // Scans the caretaker's own faction for spawned babies/newborns that want to suckle
    // (ChildcareUtility.WantsSuckle = can suckle AND is hungry), picks the nearest one the
    // caretaker can feed and reach, finds a suitable food source, and issues the vanilla
    // bottle-feed job. No-op when the caretaker can't manipulate, no baby is hungry, or no
    // baby food is available. Used by the shelter and stranded caretaker duties.
    public class JobGiver_BTGFeedBaby : ThinkNode_JobGiver
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
                if (!ChildcareUtility.WantsSuckle(baby, out _))
                    continue;
                if (!ChildcareUtility.CanFeedBaby(pawn, baby, out _))
                    continue;
                if (!pawn.CanReach(baby, PathEndMode.Touch, Danger.Deadly))
                    continue;

                float distSq = (pawn.Position - baby.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = baby;
                }
            }

            if (best == null)
                return null;

            Thing food = ChildcareUtility.FindBabyFoodForBaby(pawn, best);
            if (food == null)
                return null;

            return ChildcareUtility.MakeBottlefeedJob(best, food);
        }
    }
}
