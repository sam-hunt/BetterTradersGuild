using BetterTradersGuild.LordJobs;
using Xunit;

namespace BetterTradersGuild.Tests.LordJobs
{
    /// <summary>
    /// Pins the stranded-grace arithmetic of EscapeGraceTracker.ShouldStrand: the lord
    /// gives up only after launchable reachability has been false for a whole
    /// StrandedGraceTicks window, measured from the last reachable sample (or from the
    /// first sample / last Reset when none was ever reachable), and any reachable sample
    /// restarts the clock. The per-walker lift-off memory needs live Pawn/Thing instances
    /// and is not covered here.
    /// </summary>
    public class EscapeGraceTrackerTests
    {
        private const int Grace = EscapeGraceTracker.StrandedGraceTicks;

        [Fact]
        public void ReachableSamplesNeverStrand()
        {
            var tracker = new EscapeGraceTracker();
            for (int now = 0; now < Grace * 3; now += 60)
                Assert.False(tracker.ShouldStrand(reachableNow: true, now));
        }

        [Fact]
        public void FirstUnreachableSampleStartsTheClockInsteadOfStranding()
        {
            var tracker = new EscapeGraceTracker();
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: 100_000));
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: 100_000 + Grace - 1));
            Assert.True(tracker.ShouldStrand(reachableNow: false, now: 100_000 + Grace));
        }

        [Fact]
        public void StrandsOnlyAfterAFullWindowSinceTheLastReachableSample()
        {
            var tracker = new EscapeGraceTracker();
            Assert.False(tracker.ShouldStrand(reachableNow: true, now: 1000));
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: 1000 + Grace - 60));
            Assert.True(tracker.ShouldStrand(reachableNow: false, now: 1000 + Grace));
        }

        [Fact]
        public void AReachableSampleMidWindowRestartsTheClock()
        {
            var tracker = new EscapeGraceTracker();
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: 0));
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: Grace - 120));
            // The flicker recovers for a single sample...
            Assert.False(tracker.ShouldStrand(reachableNow: true, now: Grace - 60));
            // ...so the old window no longer counts.
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: Grace));
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: Grace - 60 + Grace - 1));
            Assert.True(tracker.ShouldStrand(reachableNow: false, now: Grace - 60 + Grace));
        }

        [Fact]
        public void ResetRestartsTheClock()
        {
            var tracker = new EscapeGraceTracker();
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: 0));
            tracker.Reset(now: Grace - 1);
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: Grace));
            Assert.False(tracker.ShouldStrand(reachableNow: false, now: Grace - 1 + Grace - 1));
            Assert.True(tracker.ShouldStrand(reachableNow: false, now: Grace - 1 + Grace));
        }
    }
}
