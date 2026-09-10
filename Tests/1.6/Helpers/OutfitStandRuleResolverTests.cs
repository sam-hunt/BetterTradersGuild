using System.Collections.Generic;
using System.Linq;
using Xunit;
using static BetterTradersGuild.Helpers.MapGeneration.OutfitStandRuleResolver;

namespace BetterTradersGuild.Tests.Helpers
{
    /// <summary>
    /// Tests for the pure core of OutfitStandRuleResolver.Plan. Stands and sets are plain
    /// strings; the chance roll and the chooser are scripted so outcomes are deterministic.
    /// </summary>
    public class OutfitStandRuleResolverTests
    {
        private static readonly string[] Sets = { "vacsuit", "marine", "recon" };
        private static readonly string[] Stands = { "s1", "s2", "s3", "s4" };

        /// <summary>A chooser that cycles through the candidates in order on each call.</summary>
        private static System.Func<IReadOnlyList<string>, string> Cycling()
        {
            int i = 0;
            return pool => pool[i++ % pool.Count];
        }

        [Fact]
        public void PerStand_EveryStandPassingChance_GetsItsOwnRoll()
        {
            var plan = Plan(Sets, perStand: true, Stands, passesChance: () => true, choose: Cycling());

            Assert.Equal(new[] { "s1", "s2", "s3", "s4" }, plan.Select(a => a.Stand));
            Assert.Equal(new[] { "vacsuit", "marine", "recon", "vacsuit" }, plan.Select(a => a.Set));
        }

        [Fact]
        public void Shared_RollsOnceAndReusesTheSet()
        {
            var plan = Plan(Sets, perStand: false, Stands, passesChance: () => true, choose: Cycling());

            Assert.Equal(4, plan.Count);
            Assert.All(plan, a => Assert.Equal("vacsuit", a.Set));
        }

        [Fact]
        public void Chance_IsRolledPerStand_AndFailedStandsAreLeftOut()
        {
            var rolls = new Queue<bool>(new[] { true, false, false, true });
            var plan = Plan(Sets, perStand: true, Stands, passesChance: rolls.Dequeue, choose: Cycling());

            Assert.Equal(new[] { "s1", "s4" }, plan.Select(a => a.Stand));
            Assert.Empty(rolls);
        }

        [Fact]
        public void Shared_SetIsNotRolledUntilAStandPassesChance()
        {
            var rolls = new Queue<bool>(new[] { false, false, true, true });
            int chooses = 0;
            var plan = Plan(Sets, perStand: false, Stands, passesChance: rolls.Dequeue, choose: pool => { chooses++; return pool[1]; });

            Assert.Equal(1, chooses);
            Assert.Equal(new[] { "s3", "s4" }, plan.Select(a => a.Stand));
            Assert.All(plan, a => Assert.Equal("marine", a.Set));
        }

        [Fact]
        public void NoSetsOrNoStands_YieldsNothing()
        {
            int chooses = 0;
            System.Func<IReadOnlyList<string>, string> counting = pool => { chooses++; return pool[0]; };

            Assert.Empty(Plan(new string[0], true, Stands, () => true, counting));
            Assert.Empty(Plan<string, string>(null, true, Stands, () => true, counting));
            Assert.Empty(Plan(Sets, true, new string[0], () => true, counting));
            Assert.Empty(Plan<string, string>(Sets, true, null, () => true, counting));
            Assert.Equal(0, chooses);
        }
    }
}
