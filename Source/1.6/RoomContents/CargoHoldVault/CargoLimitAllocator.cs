using System;
using System.Collections.Generic;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    // Pure arithmetic behind the cargo vault spawn caps (no Verse types, so it is
    // unit-testable). Given the stock grouped by item type, decides how many stacks
    // of each type may spawn so that the total stays within a stack budget and a
    // wealth budget while every type keeps roughly its share of the whole inventory.
    //
    // The caps exist because some modded orbital traders carry enough stock to crash
    // the game when it is all spawned into the vault at once; they are a safety valve,
    // not an economy knob, so best-effort proportionality is all that is needed.
    public static class CargoLimitAllocator
    {
        // Budget value meaning "no cap".
        public const int Unlimited = -1;

        public static bool IsLimited(int budget) => budget >= 0;

        public static bool IsLimited(float budget) => budget >= 0f;

        // groupStacks[g] and groupWealth[g] describe the g-th item type: how many
        // spawnable stacks it has and their combined market value. stackBudget and
        // wealthBudget are the remaining caps (Unlimited / negative for none).
        // Returns the number of stacks each type may take.
        //
        // One fraction f = min(1, stackBudget/totalStacks, wealthBudget/totalWealth)
        // scales every type at once: taking f of each type's stacks takes ~f of its
        // wealth too, so a single ratio honours both caps and preserves composition.
        // Quotas are rounded by largest remainder so the total lands on floor(f*S), then
        // a greedy trim removes any leftover overshoot (largest-quota type for stacks,
        // priciest-per-stack type for wealth).
        public static int[] Allot(IReadOnlyList<int> groupStacks, IReadOnlyList<float> groupWealth,
            int stackBudget, float wealthBudget)
        {
            if (groupStacks == null)
                throw new ArgumentNullException(nameof(groupStacks));
            if (groupWealth == null)
                throw new ArgumentNullException(nameof(groupWealth));
            if (groupStacks.Count != groupWealth.Count)
                throw new ArgumentException("groupStacks and groupWealth must have the same length");

            int groups = groupStacks.Count;
            var quota = new int[groups];

            int totalStacks = 0;
            float totalWealth = 0f;
            for (int g = 0; g < groups; g++)
            {
                totalStacks += Math.Max(0, groupStacks[g]);
                totalWealth += Math.Max(0f, groupWealth[g]);
            }

            bool stacksLimited = IsLimited(stackBudget);
            bool wealthLimited = IsLimited(wealthBudget);

            if (totalStacks == 0)
                return quota;

            // Nothing binds: take everything.
            if ((!stacksLimited || stackBudget >= totalStacks) && (!wealthLimited || wealthBudget >= totalWealth))
            {
                for (int g = 0; g < groups; g++)
                    quota[g] = Math.Max(0, groupStacks[g]);
                return quota;
            }

            // A zero budget on either axis means nothing may spawn (a zero-wealth
            // inventory is only excluded by the wealth cap when it is exactly 0).
            if ((stacksLimited && stackBudget == 0) || (wealthLimited && wealthBudget <= 0f))
                return quota;

            double f = 1.0;
            if (stacksLimited)
                f = Math.Min(f, (double)stackBudget / totalStacks);
            if (wealthLimited && totalWealth > 0f)
                f = Math.Min(f, wealthBudget / (double)totalWealth);

            // Largest-remainder rounding toward floor(f * totalStacks).
            int target = (int)Math.Floor(f * totalStacks + 1e-9);
            if (stacksLimited)
                target = Math.Min(target, stackBudget);

            var remainders = new double[groups];
            int assigned = 0;
            for (int g = 0; g < groups; g++)
            {
                int stacks = Math.Max(0, groupStacks[g]);
                double exact = f * stacks;
                quota[g] = (int)Math.Floor(exact + 1e-9);
                remainders[g] = exact - quota[g];
                assigned += quota[g];
            }

            // Hand out the leftover one stack at a time to the largest remainders
            // (ties break toward the larger type, then the earlier index).
            int leftover = target - assigned;
            while (leftover > 0)
            {
                int best = -1;
                for (int g = 0; g < groups; g++)
                {
                    if (quota[g] >= groupStacks[g])
                        continue;
                    if (best < 0
                        || remainders[g] > remainders[best]
                        || (remainders[g] == remainders[best] && groupStacks[g] > groupStacks[best]))
                        best = g;
                }
                if (best < 0)
                    break;
                quota[best]++;
                remainders[best] = -1.0;
                leftover--;
            }

            // Greedy trim for any overshoot left by rounding.
            if (wealthLimited)
            {
                while (EstimatedWealth(quota, groupStacks, groupWealth) > wealthBudget + 1e-3)
                {
                    int priciest = -1;
                    double priciestAvg = -1.0;
                    for (int g = 0; g < groups; g++)
                    {
                        if (quota[g] <= 0)
                            continue;
                        double avg = groupWealth[g] / (double)groupStacks[g];
                        if (avg > priciestAvg)
                        {
                            priciestAvg = avg;
                            priciest = g;
                        }
                    }
                    if (priciest < 0)
                        break;
                    quota[priciest]--;
                }
            }

            if (stacksLimited)
            {
                int sum = 0;
                for (int g = 0; g < groups; g++)
                    sum += quota[g];
                while (sum > stackBudget)
                {
                    int largest = -1;
                    for (int g = 0; g < groups; g++)
                    {
                        if (quota[g] > 0 && (largest < 0 || quota[g] > quota[largest]))
                            largest = g;
                    }
                    if (largest < 0)
                        break;
                    quota[largest]--;
                    sum--;
                }
            }

            return quota;
        }

        // Wealth a quota is expected to spawn, assuming each type's stacks are of
        // roughly equal value.
        public static double EstimatedWealth(IReadOnlyList<int> quota, IReadOnlyList<int> groupStacks,
            IReadOnlyList<float> groupWealth)
        {
            double total = 0.0;
            for (int g = 0; g < quota.Count; g++)
            {
                if (quota[g] <= 0 || groupStacks[g] <= 0)
                    continue;
                total += quota[g] * (groupWealth[g] / (double)groupStacks[g]);
            }
            return total;
        }

        // Remaining budget once a reserved amount (the always-spawned pawns) is
        // taken off the top; never below zero, and Unlimited stays Unlimited.
        public static int Remaining(int budget, int reserved)
        {
            return IsLimited(budget) ? Math.Max(0, budget - reserved) : Unlimited;
        }

        public static float Remaining(float budget, float reserved)
        {
            return IsLimited(budget) ? Math.Max(0f, budget - reserved) : Unlimited;
        }
    }
}
