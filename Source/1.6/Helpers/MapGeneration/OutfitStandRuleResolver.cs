using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    // Turns one stocking rule plus a set of stands into a set-per-stand assignment. The core
    // (Plan) is pure and generic so xunit can drive it with plain objects; the Verse adapter
    // (PlanForStands) binds it to live stands and Rand.
    public static class OutfitStandRuleResolver
    {
        public readonly struct Assignment<TStand, TSet>
        {
            public readonly TStand Stand;
            public readonly TSet Set;

            public Assignment(TStand stand, TSet set)
            {
                Stand = stand;
                Set = set;
            }
        }

        // Every stand passes the chance roll independently; stands that fail are left empty.
        // perStand picks a set for each stocked stand; otherwise one set is rolled once and
        // shared by all of them (so an armory's stands match). Empty or null candidate lists
        // yield nothing.
        // passesChance: rolls chancePerStand for one stand
        // choose: picks one candidate from a non-empty list (random in production)
        public static List<Assignment<TStand, TSet>> Plan<TStand, TSet>(
            IReadOnlyList<TSet> sets,
            bool perStand,
            IReadOnlyList<TStand> stands,
            Func<bool> passesChance,
            Func<IReadOnlyList<TSet>, TSet> choose)
        {
            var result = new List<Assignment<TStand, TSet>>();
            if (sets == null || sets.Count == 0 || stands == null || stands.Count == 0)
                return result;

            TSet shared = default;
            bool sharedRolled = false;
            foreach (TStand stand in stands)
            {
                if (!passesChance())
                    continue;

                TSet set;
                if (perStand)
                {
                    set = choose(sets);
                }
                else
                {
                    if (!sharedRolled)
                    {
                        shared = choose(sets);
                        sharedRolled = true;
                    }
                    set = shared;
                }
                result.Add(new Assignment<TStand, TSet>(stand, set));
            }
            return result;
        }

        // Verse binding: an XML rule over live stands with random rolls. Sets with no apparel
        // are dropped before planning so a stocked stand is never handed an empty outfit.
        public static List<Assignment<Building_OutfitStand, OutfitStandApparelSet>> PlanForStands(
            OutfitStandRule rule,
            IReadOnlyList<Building_OutfitStand> stands)
        {
            if (rule?.sets == null || stands == null || stands.Count == 0)
                return new List<Assignment<Building_OutfitStand, OutfitStandApparelSet>>();

            var sets = new List<OutfitStandApparelSet>(rule.sets.Count);
            foreach (OutfitStandApparelSet set in rule.sets)
            {
                if (set?.apparel != null && set.apparel.Count > 0)
                    sets.Add(set);
            }

            return Plan(
                sets,
                rule.perStand,
                stands,
                passesChance: () => Rand.Chance(rule.chancePerStand),
                choose: pool => pool[Rand.Range(0, pool.Count)]);
        }
    }
}
