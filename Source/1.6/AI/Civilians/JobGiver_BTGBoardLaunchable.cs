using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI.Civilians
{
    // Escape-phase step for a walker: once it has no infant left to ferry (carry node above
    // returned null), board the best reachable launchable itself. PreferredLaunchable picks
    // the shuttle when one exists (it seats the whole family) and only falls back to pods when
    // none remains; it returns null when nothing is reachable, in which case the walker yields
    // no job. Lift-off is decided by LordToil_BTGEscape, not here.
    public class JobGiver_BTGBoardLaunchable : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.carryTracker?.CarriedThing != null)
                return null;

            Thing launchable = LaunchableEscapeHelper.PreferredLaunchable(pawn);
            if (launchable == null)
                return null;

            return JobMaker.MakeJob(Jobs.BTG_BoardLaunchable, launchable);
        }
    }
}
