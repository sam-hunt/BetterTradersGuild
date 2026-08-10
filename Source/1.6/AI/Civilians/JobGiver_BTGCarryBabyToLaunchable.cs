using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.LordJobs.Civilians;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.AI.Civilians
{
    // Escape-phase step for a walker (caretaker or child): grab the nearest infant/baby not
    // yet aboard a launchable and carry it into the best reachable launchable. Reserving the
    // baby keeps two walkers from claiming the same one. A walker already holding a ferryable
    // baby gets the carry job for it directly (the driver skips the fetch leg); one carrying
    // anything else, or with no launchable reachable, yields no job (and falls through to
    // boarding).
    //
    // Walkers ferry freely rather than being locked to one baby each: as long as one walker
    // survives, every baby still gets loaded, which is more robust than a rigid 1:1 pairing
    // (the spawn count rule guarantees carriers >= infants so the nominal case is never
    // overloaded anyway). Issuing the carry as a BTG job lets walking children haul babies,
    // which the vanilla colonist-work path would refuse them.
    //
    // Scope: for shelter-lord walkers the faction scan is narrowed to the shelter's own
    // remembered infants (LordJob_BTGShelterCivilians.ShouldCarryInfant) - the family never
    // treks to crew quarters for unrelated babies, which the defenders' childcare duties
    // cover. Defeated defenders escaping via LordToil_BTGEscape have no shelter lord and
    // keep the full map-wide scan: they're evacuating the whole base, and nobody stays
    // behind to care for a baby they skip.
    public class JobGiver_BTGCarryBabyToLaunchable : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var shelterJob = pawn.GetLord()?.LordJob as LordJob_BTGShelterCivilians;

            // Already holding a ferryable baby (e.g. an interrupted feed's finalizer left it
            // in the caretaker's arms when the escape triggered): issue the carry job for it
            // directly. Without this both escape givers yield null while the hands are full,
            // stalling the walker - and since a carried baby is despawned, no OTHER walker's
            // scan can see it either, so nobody ever moved it to the craft.
            Thing carried = pawn.carryTracker?.CarriedThing;
            if (carried != null)
            {
                if (carried is Pawn carriedBaby
                    && (carriedBaby.DevelopmentalStage.Baby() || carriedBaby.DevelopmentalStage.Newborn())
                    && shelterJob?.ShouldCarryInfant(carriedBaby) != false)
                {
                    Thing dest = LaunchableEscapeHelper.PreferredLaunchable(pawn);
                    if (dest == null)
                        return null;

                    Job carryJob = JobMaker.MakeJob(Jobs.BTG_CarryBabyToLaunchable, carriedBaby, dest);
                    carryJob.count = 1;
                    return carryJob;
                }
                return null;
            }

            Thing launchable = LaunchableEscapeHelper.PreferredLaunchable(pawn);
            if (launchable == null)
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
                if (shelterJob?.ShouldCarryInfant(baby) == false)
                    continue;
                if (!pawn.CanReserve(baby))
                    continue;
                if (!pawn.CanReach(baby, PathEndMode.ClosestTouch, Danger.Deadly))
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

            Job job = JobMaker.MakeJob(Jobs.BTG_CarryBabyToLaunchable, best, launchable);
            job.count = 1;
            return job;
        }
    }
}
