using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.AI.Civilians;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // Escape phase of LordJob_BTGShelterCivilians: every walker (caretaker + children) gets
    // BTG_EscapeWalker - hack the subroom door, carry infants into the best launchable, then
    // board. This toil owns the LIFT-OFF decision (the duty only moves pawns).
    //
    // Each tick (throttled), any launchable that has occupants and no remaining walker bound
    // for it flies off via LaunchableEscapeHelper.LiftOff (custom "vanish", not vanilla
    // world-launch). "Bound for it" excludes downed/dead walkers and walkers already aboard
    // something, so:
    //   * a transport pod leaves as soon as its lone carrier (+ the infant it ferried) is in,
    //   * the shuttle waits until every still-active walker - including the caretaker/pilot,
    //     who is himself a walker - has boarded, then leaves,
    //   * a downed walker or an un-ferryable orphaned infant never deadlocks the launch.
    public class LordToil_BTGEscape : LordToil
    {
        private const int LiftOffCheckInterval = 30;

        private IntVec3 focus;

        public override IntVec3 FlagLoc => focus;

        public LordToil_BTGEscape(IntVec3 focus)
        {
            this.focus = focus;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                pawn.mindState.duty = new PawnDuty(Duties.BTG_EscapeWalker, focus);
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();

            if (Find.TickManager.TicksGame % LiftOffCheckInterval != 0)
                return;

            Map map = lord.Map;
            if (map == null)
                return;

            List<Thing> launchables = LaunchableEscapeHelper.AllLaunchables(map);
            for (int i = 0; i < launchables.Count; i++)
            {
                Thing launchable = launchables[i];
                CompTransporter transporter = launchable.TryGetComp<CompTransporter>();
                if (transporter?.innerContainer == null)
                    continue;
                // Nothing aboard yet: don't launch an empty craft.
                if (!transporter.innerContainer.Any(t => t is Pawn))
                    continue;
                if (AnyWalkerStillBoundFor(launchable, map))
                    continue;

                LaunchableEscapeHelper.LiftOff(launchable);
            }
        }

        // A walker still "bound for" this launchable is one that is alive, not downed, not yet
        // aboard any launchable, and whose preferred launchable is this one. Those gate its
        // lift-off; everyone else (aboard, downed, or dead) does not.
        private bool AnyWalkerStillBoundFor(Thing launchable, Map map)
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn == null || pawn.Dead || pawn.Downed)
                    continue;
                if (LaunchableEscapeHelper.IsAboardAnyLaunchable(pawn, map))
                    continue;
                if (LaunchableEscapeHelper.PreferredLaunchable(pawn) == launchable)
                    return true;
            }
            return false;
        }
    }
}
