using System.Collections.Generic;
using BetterTradersGuild.LordJobs.Civilians;
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
    // baby food is available (and for mechs, via ChildcareUtility's Humanlike gate). Used by
    // the shelter and stranded caretaker duties, and by the entrenched-defender duty so the
    // garrison keeps the base's babies fed through a siege. A shelter family's own infants
    // are exempt from everyone else's scan while the family can still care for them (see
    // LordJob_BTGShelterCivilians.ClaimedByOtherFamily).
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
                // A sheltering family's own infants are the family's job: a garrison feeder
                // would carry one in its arms for the whole bottle-feed, despawning it from
                // the escape walkers' carry scan right when they need to ferry it aboard.
                if (LordJob_BTGShelterCivilians.ClaimedByOtherFamily(baby, pawn))
                    continue;
                if (!ChildcareUtility.WantsSuckle(baby, out _))
                    continue;
                if (!ChildcareUtility.CanFeedBaby(pawn, baby, out _))
                    continue;
                // Skip babies already claimed by another caretaker. CanFeedBaby doesn't check
                // reservations, and JobDriver_FeedBaby reserves the baby (maxPawns 1) - without
                // this gate every duty holder issues the job and all but the first fail it.
                if (!pawn.CanReserve(baby))
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
