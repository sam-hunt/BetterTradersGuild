using Xunit;

namespace BetterTradersGuild.Tests.Helpers
{
    /// <summary>
    /// Pins the arithmetic of the security-collapse defeat threshold
    /// (SecurityDefeatUtility.MeetsDefeatThreshold): a BTG garrison is defeated once
    /// 80% of its original active security is incapacitated, using vanilla's
    /// float-compare idiom so small garrisons round toward requiring more downed,
    /// never fewer. initial &lt;= 0 (census unknown or empty) falls back to
    /// requiring zero active security.
    /// </summary>
    public class SecurityDefeatUtilityTests
    {
        [Theory]
        // Exactly 80% incapacitated fires; one short does not.
        [InlineData(10, 2, true)]
        [InlineData(10, 3, false)]
        [InlineData(10, 0, true)]
        [InlineData(5, 1, true)]
        [InlineData(5, 2, false)]
        // Small garrisons round up: 80% of 4 is 3.2, so 3 downed is not enough.
        [InlineData(4, 1, false)]
        [InlineData(4, 0, true)]
        [InlineData(2, 1, false)]
        [InlineData(2, 0, true)]
        [InlineData(1, 1, false)]
        [InlineData(1, 0, true)]
        // Active exceeding the census (late reinforcement edge) never reads defeated.
        [InlineData(10, 12, false)]
        // Census fallback: unknown (-1) or empty (0) requires zero active security.
        [InlineData(0, 0, true)]
        [InlineData(-1, 0, true)]
        [InlineData(-1, 3, false)]
        public void MeetsDefeatThreshold_MatchesEightyPercentRule(
            int initial, int active, bool expected)
        {
            Assert.Equal(expected,
                SecurityDefeatUtility.MeetsDefeatThreshold(initial, active));
        }
    }
}
