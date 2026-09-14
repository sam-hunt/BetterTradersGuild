using System;
using BetterTradersGuild.RoomContents.CargoVault;
using Xunit;

namespace BetterTradersGuild.Tests.RoomContents
{
    /// <summary>
    /// Pins the pure arithmetic behind CargoLimitAllocator.Allot: a single scale
    /// factor f = min(1, stackBudget/totalStacks, wealthBudget/totalWealth) is
    /// applied to every group's stack count, rounded by largest remainder so the
    /// total lands on floor(f * totalStacks), then trimmed if rounding pushed the
    /// wealth estimate or the stack sum over budget. Also covers Remaining, which
    /// clamps a reserved amount off a budget while keeping Unlimited (-1) sticky.
    /// </summary>
    public class CargoLimitAllocatorTests
    {
        [Fact]
        public void Allot_UnlimitedBudgets_ReturnsFullStacks()
        {
            var stacks = new[] { 4, 5, 6 };
            var wealth = new[] { 1f, 2f, 3f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth,
                CargoLimitAllocator.Unlimited, CargoLimitAllocator.Unlimited);

            Assert.Equal(stacks, quota);
        }

        [Theory]
        [InlineData(60, 600)]   // exactly the totals
        [InlineData(100, 1000)] // comfortably above the totals
        public void Allot_BudgetsAtOrAboveTotals_ReturnsFullStacks(int stackBudget, float wealthBudget)
        {
            var stacks = new[] { 10, 20, 30 };
            var wealth = new[] { 100f, 200f, 300f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, stackBudget, wealthBudget);

            Assert.Equal(stacks, quota);
        }

        [Fact]
        public void Allot_ZeroStackBudget_ReturnsAllZero()
        {
            var stacks = new[] { 5, 5 };
            var wealth = new[] { 10f, 10f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, 0, CargoLimitAllocator.Unlimited);

            Assert.Equal(new[] { 0, 0 }, quota);
        }

        [Fact]
        public void Allot_ZeroWealthBudget_ReturnsAllZero()
        {
            var stacks = new[] { 5, 5 };
            var wealth = new[] { 10f, 10f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, CargoLimitAllocator.Unlimited, 0f);

            Assert.Equal(new[] { 0, 0 }, quota);
        }

        [Fact]
        public void Allot_StackCapOnly_ScalesProportionally()
        {
            var stacks = new[] { 100, 50, 50 };
            var wealth = new[] { 0f, 0f, 0f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, 100, CargoLimitAllocator.Unlimited);

            Assert.Equal(new[] { 50, 25, 25 }, quota);

            int sum = 0;
            for (int g = 0; g < quota.Length; g++)
            {
                Assert.InRange(quota[g], 0, stacks[g]);
                sum += quota[g];
            }
            Assert.True(sum <= 100);
        }

        [Fact]
        public void Allot_WealthCapOnly_ScalesProportionally()
        {
            var stacks = new[] { 10, 10 };
            var wealth = new[] { 1000f, 9000f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, CargoLimitAllocator.Unlimited, 5000f);

            Assert.Equal(new[] { 5, 5 }, quota);
            Assert.True(CargoLimitAllocator.EstimatedWealth(quota, stacks, wealth) <= 5000.0 + 1e-3);
        }

        [Fact]
        public void Allot_BothCaps_TighterCapWins()
        {
            // Stack cap alone (f=0.75) would allow 15 stacks total; the wealth cap
            // (f=0.25) is tighter and must be the one that binds.
            var stacks = new[] { 10, 10 };
            var wealth = new[] { 100000f, 100000f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, 15, 50000f);

            int sum = quota[0] + quota[1];
            Assert.Equal(5, sum);
            Assert.True(sum < 15, "the looser stack-only cap would have allowed more than the tighter wealth cap");
            Assert.True(CargoLimitAllocator.EstimatedWealth(quota, stacks, wealth) <= 50000.0 + 1e-3);
        }

        [Fact]
        public void Allot_LargestRemainder_EqualGroupsBreakTieByEarlierIndex()
        {
            // f = 5/9; every group's exact share (1.667) has the same remainder, so
            // the two leftover stacks go to the earliest indices, in order.
            var stacks = new[] { 3, 3, 3 };
            var wealth = new[] { 0f, 0f, 0f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, 5, CargoLimitAllocator.Unlimited);

            Assert.Equal(new[] { 2, 2, 1 }, quota);
            Assert.Equal(5, quota[0] + quota[1] + quota[2]);
        }

        [Fact]
        public void Allot_TinyGroupUnderSmallFraction_TotalStillLandsOnBudget()
        {
            // A 1-stack group next to a 100-stack group under a budget of 1: the
            // exact per-group outcome depends on remainder comparison, but the
            // total must land on the budget and every group must stay in bounds.
            var stacks = new[] { 1, 100 };
            var wealth = new[] { 0f, 0f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, 1, CargoLimitAllocator.Unlimited);

            Assert.Equal(1, quota[0] + quota[1]);
            Assert.InRange(quota[0], 0, 1);
            Assert.InRange(quota[1], 0, 100);
        }

        [Fact]
        public void Allot_WealthOvershootFromRounding_IsTrimmedFromThePriciestGroup()
        {
            // f = 0.75 exactly. Largest-remainder hands the leftover stack to the
            // expensive group (remainder .75 vs .25), which would push estimated
            // wealth to 102 against a 77.25 budget; the trim loop must claw that
            // stack back off the priciest-per-stack group.
            var stacks = new[] { 3, 1 };
            var wealth = new[] { 3f, 100f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, 3, 77.25f);

            Assert.Equal(new[] { 2, 0 }, quota);
            Assert.True(CargoLimitAllocator.EstimatedWealth(quota, stacks, wealth) <= 77.25 + 1e-3);
            Assert.True(quota[0] + quota[1] <= 3);
        }

        [Fact]
        public void Allot_NegativeGroupEntries_TreatedAsZero_WhenUnlimited()
        {
            var stacks = new[] { -5, 10 };
            var wealth = new[] { -100f, 50f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth,
                CargoLimitAllocator.Unlimited, CargoLimitAllocator.Unlimited);

            Assert.Equal(new[] { 0, 10 }, quota);
        }

        [Fact]
        public void Allot_NegativeGroupEntries_TreatedAsZero_WhenLimited()
        {
            var stacks = new[] { -5, 10 };
            var wealth = new[] { -100f, 50f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, 5, CargoLimitAllocator.Unlimited);

            Assert.Equal(new[] { 0, 5 }, quota);
        }

        [Theory]
        [InlineData(CargoLimitAllocator.Unlimited, 5, CargoLimitAllocator.Unlimited)]
        [InlineData(10, 15, 0)]
        [InlineData(10, 3, 7)]
        public void Remaining_Int_HandlesUnlimitedAndClamping(int budget, int reserved, int expected)
        {
            Assert.Equal(expected, CargoLimitAllocator.Remaining(budget, reserved));
        }

        [Theory]
        [InlineData(-1f, 5f, -1f)]
        [InlineData(10f, 15f, 0f)]
        [InlineData(10f, 3f, 7f)]
        public void Remaining_Float_HandlesUnlimitedAndClamping(float budget, float reserved, float expected)
        {
            Assert.Equal(expected, CargoLimitAllocator.Remaining(budget, reserved));
        }

        [Fact]
        public void Allot_MismatchedLengths_ThrowsArgumentException()
        {
            var stacks = new[] { 1, 2 };
            var wealth = new[] { 1f };

            Assert.Throws<ArgumentException>(() =>
                CargoLimitAllocator.Allot(stacks, wealth, 10, 10f));
        }

        [Fact]
        public void Allot_NullGroupStacks_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                CargoLimitAllocator.Allot(null, new[] { 1f }, 10, 10f));
        }

        [Fact]
        public void Allot_NullGroupWealth_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                CargoLimitAllocator.Allot(new[] { 1 }, null, 10, 10f));
        }

        [Theory]
        [InlineData(CargoLimitAllocator.Unlimited, -1f)]
        [InlineData(10, CargoLimitAllocator.Unlimited)]
        [InlineData(CargoLimitAllocator.Unlimited, 30f)]
        [InlineData(0, CargoLimitAllocator.Unlimited)]
        [InlineData(CargoLimitAllocator.Unlimited, 0f)]
        [InlineData(10, 30f)]
        [InlineData(1000, 1000f)]
        [InlineData(3, 5f)]
        public void Allot_Invariants_HoldAcrossBudgetSweep(int stackBudget, float wealthBudget)
        {
            var stacks = new[] { 5, 20, 1, 50 };
            var wealth = new[] { 50f, 20f, 100f, 10f };

            var quota = CargoLimitAllocator.Allot(stacks, wealth, stackBudget, wealthBudget);

            int sum = 0;
            for (int g = 0; g < quota.Length; g++)
            {
                Assert.InRange(quota[g], 0, stacks[g]);
                sum += quota[g];
            }

            if (CargoLimitAllocator.IsLimited(stackBudget))
                Assert.True(sum <= stackBudget);

            if (CargoLimitAllocator.IsLimited(wealthBudget) && wealthBudget > 0f)
            {
                Assert.True(CargoLimitAllocator.EstimatedWealth(quota, stacks, wealth)
                    <= wealthBudget + 1e-3);
            }
        }
    }
}
