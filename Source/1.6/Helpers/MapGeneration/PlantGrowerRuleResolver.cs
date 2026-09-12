using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    // Turns an ordered list of planting rules plus a set of growers into one plant/growth
    // assignment per grower. The core (Plan) is pure and generic so xunit can drive it with
    // plain objects; the Verse adapter (PlanForGrowers) binds it to ThingDefs, the
    // DefDatabase sow-tag lookup and PlantUtility.CanSowOnGrower.
    public static class PlantGrowerRuleResolver
    {
        // A rule reduced to what the planner needs.
        public sealed class PlanRule<TGrower, TPlant>
        {
            public Func<TGrower, bool> Matches;
            public IReadOnlyList<TPlant> Candidates;
            public float GrowthMin;
            public float GrowthMax;
            public bool PerGrower;
            public string Label;
        }

        public readonly struct Assignment<TGrower, TPlant>
        {
            public readonly TGrower Grower;
            public readonly TPlant Plant;
            public readonly float Growth;

            public Assignment(TGrower grower, TPlant plant, float growth)
            {
                Grower = grower;
                Plant = plant;
                Growth = growth;
            }
        }

        // Rules are taken in order; each grower is assigned by the first rule that matches
        // it and has a plant it can grow. A shared (non-perGrower) rule rolls one plant and
        // one growth for all its growers, choosing only among plants every one of them
        // accepts; if no plant satisfies them all it warns and falls back to independent
        // per-grower rolls so nothing is left bare for want of a common crop. Growers no
        // rule can plant stay unassigned (the caller may run a later rule set over them).
        // canSow: whether a plant may be sown in a grower
        // choose: picks one candidate from a non-empty list (random in production)
        // rollGrowth: picks a growth from an inclusive [min, max] range
        // warn: receives human-readable notes about rules that could not be honoured
        public static List<Assignment<TGrower, TPlant>> Plan<TGrower, TPlant>(
            IReadOnlyList<PlanRule<TGrower, TPlant>> rules,
            IReadOnlyList<TGrower> growers,
            Func<TPlant, TGrower, bool> canSow,
            Func<IReadOnlyList<TPlant>, TPlant> choose,
            Func<float, float, float> rollGrowth,
            Action<string> warn)
        {
            var result = new List<Assignment<TGrower, TPlant>>();
            if (rules == null || growers == null || growers.Count == 0)
                return result;

            var unassigned = new List<TGrower>(growers);
            var matched = new List<TGrower>();
            var pool = new List<TPlant>();

            foreach (PlanRule<TGrower, TPlant> rule in rules)
            {
                if (rule == null || unassigned.Count == 0)
                    break;

                matched.Clear();
                foreach (TGrower grower in unassigned)
                {
                    if (rule.Matches == null || rule.Matches(grower))
                        matched.Add(grower);
                }
                if (matched.Count == 0)
                    continue;

                if (rule.Candidates == null || rule.Candidates.Count == 0)
                {
                    warn?.Invoke($"rule '{rule.Label}' has no candidate plants");
                    continue;
                }

                bool perGrower = rule.PerGrower;
                if (!perGrower)
                {
                    pool.Clear();
                    foreach (TPlant plant in rule.Candidates)
                    {
                        bool everyone = true;
                        foreach (TGrower grower in matched)
                        {
                            if (!canSow(plant, grower))
                            {
                                everyone = false;
                                break;
                            }
                        }
                        if (everyone)
                            pool.Add(plant);
                    }

                    if (pool.Count == 0)
                    {
                        warn?.Invoke($"rule '{rule.Label}' has no plant every matching grower can grow; rolling per grower instead");
                        perGrower = true;
                    }
                    else
                    {
                        TPlant plant = choose(pool);
                        float growth = rollGrowth(rule.GrowthMin, rule.GrowthMax);
                        foreach (TGrower grower in matched)
                        {
                            result.Add(new Assignment<TGrower, TPlant>(grower, plant, growth));
                            unassigned.Remove(grower);
                        }
                    }
                }

                if (perGrower)
                {
                    foreach (TGrower grower in matched)
                    {
                        pool.Clear();
                        foreach (TPlant plant in rule.Candidates)
                        {
                            if (canSow(plant, grower))
                                pool.Add(plant);
                        }
                        if (pool.Count == 0)
                            continue; // leave for a later rule

                        result.Add(new Assignment<TGrower, TPlant>(grower, choose(pool), rollGrowth(rule.GrowthMin, rule.GrowthMax)));
                        unassigned.Remove(grower);
                    }
                }
            }

            return result;
        }

        // Verse binding: XML rules over live growers, random choices, sow legality via
        // PlantUtility.CanSowOnGrower (the vanilla "set plant to grow" check: matches the
        // plant's sowTags to the grower's building.sowTag, so basins only get Hydroponic
        // crops and pots only Decorative plants, modded or not).
        // label: context for warnings (the def the rules came from)
        public static List<Assignment<Building_PlantGrower, ThingDef>> PlanForGrowers(
            IReadOnlyList<PlantGrowerRule> rules,
            IReadOnlyList<Building_PlantGrower> growers,
            string label)
        {
            if (rules == null || growers == null || growers.Count == 0)
                return new List<Assignment<Building_PlantGrower, ThingDef>>();

            var planRules = new List<PlanRule<Building_PlantGrower, ThingDef>>(rules.Count);
            for (int i = 0; i < rules.Count; i++)
            {
                PlantGrowerRule rule = rules[i];
                if (rule == null)
                    continue;

                ThingDef growerDef = rule.grower;
                planRules.Add(new PlanRule<Building_PlantGrower, ThingDef>
                {
                    Matches = growerDef == null ? null : g => g.def == growerDef,
                    Candidates = Candidates(rule),
                    GrowthMin = rule.growthRange.min,
                    GrowthMax = rule.growthRange.max,
                    PerGrower = rule.perGrower,
                    Label = $"{label} rule {i + 1}",
                });
            }

            return Plan(
                planRules,
                growers,
                canSow: (plant, grower) => plant?.plant != null && PlantUtility.CanSowOnGrower(plant, grower),
                choose: pool => pool[Rand.Range(0, pool.Count)],
                rollGrowth: Rand.Range,
                warn: msg => Log.Warning($"[Better Traders Guild] Plant growers: {msg}"));
        }

        // Explicit plants plus every sowable plant carrying the rule's sow tag, de-duplicated.
        private static List<ThingDef> Candidates(PlantGrowerRule rule)
        {
            var candidates = new List<ThingDef>();
            if (rule.plants != null)
            {
                foreach (ThingDef plant in rule.plants)
                {
                    if (plant != null && !candidates.Contains(plant))
                        candidates.Add(plant);
                }
            }

            if (!string.IsNullOrEmpty(rule.sowTag))
            {
                List<ThingDef> all = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int i = 0; i < all.Count; i++)
                {
                    ThingDef def = all[i];
                    if (def.plant?.Sowable == true
                        && def.plant.sowTags.Contains(rule.sowTag)
                        && !candidates.Contains(def))
                        candidates.Add(def);
                }
            }

            return candidates;
        }
    }
}
