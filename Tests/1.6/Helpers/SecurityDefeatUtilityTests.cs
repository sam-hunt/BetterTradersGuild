using Xunit;

namespace BetterTradersGuild.Tests.Helpers
{
    /// <summary>
    /// Pins the arithmetic of the security-collapse defeat threshold
    /// (SecurityDefeatUtility.MeetsDefeatThreshold): a BTG garrison is defeated once
    /// the configured fraction of its original active security is incapacitated,
    /// using vanilla's float-compare idiom so small garrisons round toward requiring
    /// more downed, never fewer. initial &lt;= 0 (census unknown or empty) falls back
    /// to requiring zero active security. The fraction is the securityDefeatFraction
    /// mod setting at runtime; passed explicitly here since tests cannot read
    /// settings.
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
            Assert.Equal(expected, SecurityDefeatUtility.MeetsDefeatThreshold(
                initial, active, SecurityDefeatUtility.DefaultDefeatedFraction));
        }

        [Theory]
        // Slider floor (50%): half the garrison down concedes the map.
        [InlineData(10, 5, 0.5f, true)]
        [InlineData(10, 6, 0.5f, false)]
        // Odd counts round toward requiring more: 50% of 5 is 2.5, so 2 downed is
        // not enough.
        [InlineData(5, 3, 0.5f, false)]
        [InlineData(5, 2, 0.5f, true)]
        // Slider ceiling (100%): every last defender must go down.
        [InlineData(10, 1, 1.0f, false)]
        [InlineData(10, 0, 1.0f, true)]
        [InlineData(1, 1, 1.0f, false)]
        [InlineData(1, 0, 1.0f, true)]
        // Census fallback ignores the fraction entirely.
        [InlineData(0, 0, 0.5f, true)]
        [InlineData(-1, 1, 0.5f, false)]
        public void MeetsDefeatThreshold_HonoursConfiguredFraction(
            int initial, int active, float fraction, bool expected)
        {
            Assert.Equal(expected,
                SecurityDefeatUtility.MeetsDefeatThreshold(initial, active, fraction));
        }
    }
}
