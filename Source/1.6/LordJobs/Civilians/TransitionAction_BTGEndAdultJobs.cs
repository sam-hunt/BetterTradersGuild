using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // Ends the in-progress jobs of ADULT lord members only. Used on the escape <-> defend
    // edges of LordJob_BTGShelterCivilians, where only the adult's duty actually changes:
    // children keep BTG_EscapeWalker through both transitions, so killing their jobs (as
    // vanilla TransitionAction_EndAllJobs does) only interrupted ferries in progress - a
    // child carrying a baby to the craft dropped nothing but lost its path and re-planned,
    // and under sustained fire the harm-signal defend edge re-fired often enough that a
    // carry job (much longer than one 60-tick check interval) could never complete.
    //
    // Drops a mid-feed baby before the end: the into-defend edge also fires from stranded,
    // whose duties feed, and ending a feed with the baby in arms starts vanilla's
    // bring-baby-to-safety finalizer over the new duty (see
    // TransitionAction_BTGDropMidFeedBabies).
    public class TransitionAction_BTGEndAdultJobs : TransitionAction
    {
        public override void DoAction(Transition trans)
        {
            List<Pawn> ownedPawns = trans.target.lord.ownedPawns;
            for (int i = 0; i < ownedPawns.Count; i++)
            {
                Pawn pawn = ownedPawns[i];
                if (pawn?.jobs?.curJob != null && pawn.DevelopmentalStage.Adult())
                {
                    TransitionAction_BTGDropMidFeedBabies.DropBabyIfMidFeed(pawn);
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
        }
    }
}
