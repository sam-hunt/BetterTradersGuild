using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Escape-chain last resort: nothing above could act, usually because the route to the
    // launchable is momentarily blocked (a door mid-cycle, a transient region split) or no
    // launchable is reachable at all. Hold position with a short Wait and re-poll.
    //
    // Without this node the duty subtree returns null and the walker falls through to the
    // generic humanlike tree's JobGiver_WanderAnywhere. That is worse in two ways: a walker
    // can wander AWAY from the shuttle carrying an infant (reads as broken to the player),
    // and since think trees only re-run between jobs, a multi-hundred-tick wander stretches
    // a seconds-long blockage into a long stall before the carry giver gets another look.
    // A short Wait re-polls the chain every couple of seconds and reads as "waiting for the
    // way to clear".
    //
    // Safe while carrying: vanilla's Wait JobDef sets dropThingBeforeJob=false, so starting
    // the hold never drops a carried baby. If no launchable ever becomes reachable, the
    // lord's escape/stranded transitions re-duty the walker out of this tree entirely.
    public class JobGiver_BTGEscapeHold : ThinkNode_JobGiver
    {
        private const int HoldTicks = 120;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Wait, HoldTicks);
            return job;
        }
    }
}
