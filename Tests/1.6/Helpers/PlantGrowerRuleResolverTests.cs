using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BetterTradersGuild.Helpers.MapGeneration;
using static BetterTradersGuild.Helpers.MapGeneration.PlantGrowerRuleResolver;

namespace BetterTradersGuild.Tests.Helpers
{
    /// <summary>
    /// Tests for the pure core of PlantGrowerRuleResolver.Plan. Growers and plants are plain
    /// strings; a grower "basin*" accepts plants starting with "crop", a grower "pot*"
    /// accepts plants starting with "flower". The chooser always picks the first candidate
    /// and growth rolls return the range minimum, so outcomes are deterministic.
    /// </summary>
    public class PlantGrowerRuleResolverTests
    {
        private static bool CanSow(string plant, string grower) =>
            (grower.StartsWith("basin") && plant.StartsWith("crop"))
            || (grower.StartsWith("pot") && plant.StartsWith("flower"));

        private static string First(IReadOnlyList<string> pool) => pool[0];

        private static float Min(float min, float max) => min;

        private static PlanRule<string, string> Rule(string growerPrefix, bool perGrower, float growth, params string[] candidates) =>
            new PlanRule<string, string>
            {
                Matches = growerPrefix == null ? null : g => g.StartsWith(growerPrefix),
                Candidates = candidates,
                GrowthMin = growth,
                GrowthMax = growth,
                PerGrower = perGrower,
                Label = growerPrefix ?? "any",
            };

        private static List<Assignment<string, string>> Run(
            IReadOnlyList<PlanRule<string, string>> rules, IReadOnlyList<string> growers,
            List<string> warnings = null, Func<IReadOnlyList<string>, string> choose = null)
        {
            return Plan(rules, growers, CanSow, choose ?? First, Min, warnings == null ? null : warnings.Add);
        }

        [Fact]
        public void SharedRule_RollsOncePerRule()
        {
            int rolls = 0;
            string Counting(IReadOnlyList<string> pool) { rolls++; return pool[0]; }

            var plan = Run(
                new[] { Rule("basin", perGrower: false, 0.5f, "cropRice", "cropPotato") },
                new[] { "basin1", "basin2", "basin3" },
                choose: Counting);

            Assert.Equal(1, rolls);
            Assert.Equal(3, plan.Count);
            Assert.All(plan, a => Assert.Equal("cropRice", a.Plant));
            Assert.All(plan, a => Assert.Equal(0.5f, a.Growth));
        }

        [Fact]
        public void PerGrowerRule_RollsPerGrower()
        {
            int rolls = 0;
            string Counting(IReadOnlyList<string> pool) { rolls++; return pool[0]; }

            var plan = Run(
                new[] { Rule("pot", perGrower: true, 1f, "flowerRose", "flowerDaylily") },
                new[] { "pot1", "pot2" },
                choose: Counting);

            Assert.Equal(2, rolls);
            Assert.Equal(2, plan.Count);
        }

        [Fact]
        public void FirstMatchingRuleWins_AndCatchAllTakesTheRest()
        {
            var plan = Run(
                new[]
                {
                    Rule("basin", perGrower: false, 0.2f, "cropHealroot"),
                    Rule(null, perGrower: false, 1f, "flowerRose"),
                },
                new[] { "basin1", "pot1", "pot2" });

            Assert.Equal("cropHealroot", plan.Single(a => a.Grower == "basin1").Plant);
            Assert.Equal(0.2f, plan.Single(a => a.Grower == "basin1").Growth);
            Assert.Equal(2, plan.Count(a => a.Plant == "flowerRose"));
            Assert.Equal(3, plan.Count);
        }

        [Fact]
        public void SharedRule_OnlyPicksPlantsEveryGrowerAccepts()
        {
            // "special" pot only grows flowerDaylily; the shared roll must not pick flowerRose.
            bool Fussy(string plant, string grower) =>
                grower == "potSpecial" ? plant == "flowerDaylily" : CanSow(plant, grower);

            var plan = Plan(
                new[] { Rule("pot", perGrower: false, 1f, "flowerRose", "flowerDaylily") },
                new[] { "pot1", "potSpecial" },
                Fussy, First, Min, null);

            Assert.Equal(2, plan.Count);
            Assert.All(plan, a => Assert.Equal("flowerDaylily", a.Plant));
        }

        [Fact]
        public void SharedRule_FallsBackPerGrower_WhenNoCommonPlant()
        {
            var warnings = new List<string>();

            // No plant fits both a basin and a pot, so the shared rule warns and rolls each.
            var plan = Run(
                new[] { Rule(null, perGrower: false, 1f, "cropRice", "flowerRose") },
                new[] { "basin1", "pot1" },
                warnings);

            Assert.Single(warnings);
            Assert.Equal("cropRice", plan.Single(a => a.Grower == "basin1").Plant);
            Assert.Equal("flowerRose", plan.Single(a => a.Grower == "pot1").Plant);
        }

        [Fact]
        public void UnplantableGrower_StaysUnassigned()
        {
            var plan = Run(
                new[] { Rule(null, perGrower: true, 1f, "flowerRose") },
                new[] { "basin1", "pot1" });

            Assert.Single(plan);
            Assert.Equal("pot1", plan[0].Grower);
        }

        [Fact]
        public void EmptyCandidates_WarnsAndSkipsRule()
        {
            var warnings = new List<string>();

            var plan = Run(
                new[]
                {
                    Rule("pot", perGrower: false, 1f),
                    Rule("pot", perGrower: false, 1f, "flowerRose"),
                },
                new[] { "pot1" },
                warnings);

            Assert.Single(warnings);
            Assert.Single(plan);
            Assert.Equal("flowerRose", plan[0].Plant);
        }

        [Fact]
        public void NoGrowers_ReturnsEmpty()
        {
            Assert.Empty(Run(new[] { Rule(null, false, 1f, "flowerRose") }, Array.Empty<string>()));
            Assert.Empty(Run(null, new[] { "pot1" }));
        }
    }
}
