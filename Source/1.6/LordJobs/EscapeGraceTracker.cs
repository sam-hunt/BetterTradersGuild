using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.LordJobs
{
    // Hysteresis for the escape lords' launchable-reachability decisions. Both escape lords
    // (LordJob_BTGShelterCivilians, LordJob_BTGDefendStructure) used to demote Escape ->
    // Stranded the first 60-tick check that no walker could reach a launchable, and
    // LordToil_BTGEscape flew a craft the first 30-tick check its passengers' companions
    // were not bound for it. Pawn.CanReach is not stable at that granularity: a closed door
    // in mid-cycle, a transient region split or a stale reachability cache entry flips it
    // for a sample or two, and every false sample cost a phase change. The flap was the
    // caretaker's pick/drop/retuck loop: Stranded's tuck duty put the infant back in a crib
    // (dropping it first, since vanilla BringBabyToSafety drops carried things on start),
    // the next reachable sample re-promoted to Escape, whose job-ending action dropped the
    // infant again, and the carry giver re-fetched it - dozens of times over. It also sent
    // the shuttle off with a child alone while the caretaker's route was closed for one
    // lift-off sample.
    //
    // Two windows, both counted from the last time the thing WAS true, so recovery is
    // always immediate and only the give-up is delayed:
    //   * Stranded grace (lord level): demote only after reachability has been false for
    //     the whole window. Escape's own hold giver already keeps a blocked walker waiting
    //     in place with its infant in arms, so waiting costs nothing but time.
    //   * Lift-off grace (toil level): a walker whose launchable just became unreachable
    //     keeps counting as bound for the craft it was last heading to, so a craft never
    //     leaves a family member behind over a momentary blockage. Shorter than the
    //     stranded window so a genuinely cut-off walker still cannot deadlock a loaded
    //     craft: it leaves well before the lord gives up (and stops the lift-off tick).
    //
    // All methods take the current tick explicitly so the arithmetic is unit-testable
    // without a running game. Only the stranded clock is scribed: the per-walker memory
    // rebuilds within one lift-off check after a load.
    public class EscapeGraceTracker : IExposable
    {
        // ~1 in-game hour. Comfortably longer than the flicker recoveries seen in the
        // field (hundreds of ticks up to ~1500) without leaving a truly stranded family
        // holding position for long.
        public const int StrandedGraceTicks = 2500;

        // ~20 s at 1x: long enough for a door cycle or a colonist pausing in a doorway,
        // short enough that a cut-off walker doesn't hold the craft to the stranded window.
        public const int LiftOffGraceTicks = 1200;

        private int lastReachableTick = -1;

        private readonly Dictionary<Pawn, BoundMemory> lastBound = new Dictionary<Pawn, BoundMemory>();

        private struct BoundMemory
        {
            public Thing launchable;
            public int tick;
        }

        // Restart the stranded clock: the escape (re)started, or its door-hack prelude is
        // still in progress, so unreachable launchables are expected rather than a failure.
        public void Reset(int now)
        {
            lastReachableTick = now;
        }

        // Feed one reachability sample; true when the lord should give up and strand.
        // Reachable samples restart the clock; the first unreachable sample after a fresh
        // tracker starts it, so a lord that never saw a reachable launchable still waits the
        // full window from its first check rather than stranding on the spot.
        public bool ShouldStrand(bool reachableNow, int now)
        {
            if (reachableNow || lastReachableTick < 0)
            {
                lastReachableTick = now;
                return false;
            }
            return now - lastReachableTick >= StrandedGraceTicks;
        }

        // Record that this walker currently heads for this launchable.
        public void NoteBound(Pawn walker, Thing launchable, int now)
        {
            lastBound[walker] = new BoundMemory { launchable = launchable, tick = now };
        }

        // True while the walker's last known destination was this launchable and that was
        // seen within the lift-off grace window - the walker is presumed merely blocked.
        public bool RecentlyBoundFor(Pawn walker, Thing launchable, int now)
        {
            return lastBound.TryGetValue(walker, out BoundMemory memory)
                && memory.launchable == launchable
                && now - memory.tick < LiftOffGraceTicks;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref lastReachableTick, "lastReachableTick", -1);
        }
    }
}
