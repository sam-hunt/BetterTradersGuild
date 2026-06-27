using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Critical-condition withdrawal for bounded defenders. When a defender is so
    // badly hurt that staying in the firing line risks immediate death, it breaks
    // off combat, ducks out of the enemy's line of sight (still inside the
    // structure), tends its most critical wounds, then returns to the fight once
    // stable.
    //
    // This sits ABOVE the combat job-giver, so it deliberately preempts
    // seek-and-destroy even when acquirable targets exist. It is NOT the
    // tend-when-idle case (that is handled by the lower-priority vanilla
    // JobGiver_SelfTend, which only runs once combat finds no target). The intent
    // here is the opposite: there ARE hostiles, possibly in line of sight, but
    // this pawn can no longer meaningfully contribute without dying, so it
    // temporarily withdraws to patch itself up.
    //
    // Withdraw-cell selection (every candidate is constrained to the structure
    // rect union via StructureBoundsCache, so a critically wounded defender never
    // flees into vacuum), in order of preference:
    //   1. nearest reachable cell that breaks line of sight from every hostile
    //      AND lies away from the nearest hostile (retreat, don't sidestep into it);
    //   2. else the nearest line-of-sight-breaking cell in any direction;
    //   3. else the cell with the most ranged cover from all hostiles, if it
    //      clears coverFallbackThreshold (partial cover beats none);
    //   4. else tend where standing rather than keep fighting while dying.
    //
    // Cheap-gate ordering: the humanlike / needs-tend / critical checks run first
    // and bail on the overwhelming majority of think ticks, so the hostile scan
    // and cover search only run for the rare critically wounded defender. Inert
    // for mechs (Humanlike-gated), like vanilla self-tend.
    public class JobGiver_BTGWithdrawAndTend : ThinkNode_JobGiver
    {
        // "Critical condition" thresholds (user choice: bleeding OR low health).
        public float criticalHealthThreshold = 0.4f;
        // Bleed rate stacks quickly from several minor wounds without being
        // immediately life-threatening, so this is set high deliberately.
        public float criticalBleedRateThreshold = 0.8f;

        // How far out to look for hostiles to break line of sight from.
        public float dangerScanRadius = 60f;
        // How far the pawn is willing to move to reach cover before tending.
        public float withdrawSearchRadius = 16f;
        // Fallback when no reachable cell fully breaks line of sight: accept a cell
        // that at least provides this much ranged cover (overall block chance) from
        // every nearby hostile, rather than tend fully exposed.
        public float coverFallbackThreshold = 0.3f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_BTGWithdrawAndTend)base.DeepCopy(resolve);
            copy.criticalHealthThreshold = criticalHealthThreshold;
            copy.criticalBleedRateThreshold = criticalBleedRateThreshold;
            copy.dangerScanRadius = dangerScanRadius;
            copy.withdrawSearchRadius = withdrawSearchRadius;
            copy.coverFallbackThreshold = coverFallbackThreshold;
            return copy;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            // Mirror JobGiver_SelfTend's eligibility gate: only humanlikes that
            // actually have wounds to tend, can manipulate, aren't berserk, and can
            // reserve themselves. Mechs fail Humanlike and bail here.
            if (!pawn.RaceProps.Humanlike
                || !pawn.health.HasHediffsNeedingTend()
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                || pawn.InAggroMentalState)
                return null;

            if (!IsCritical(pawn))
                return null;

            if (!pawn.Map.reservationManager.CanReserve(pawn, pawn))
                return null;

            List<Thing> hostiles = GatherHostiles(pawn);

            // No one to hide from: just tend in place (cheaper than a cover search).
            if (hostiles.Count == 0)
                return TendJob(pawn);

            // Already out of every hostile's line of sight: tend where we stand.
            if (CellOutOfSight(pawn.Position, pawn.Map, hostiles))
                return TendJob(pawn);

            // Otherwise pull back to the nearest reachable in-structure cell that
            // breaks line of sight, then re-evaluate (and tend) on arrival.
            if (TryFindWithdrawCell(pawn, hostiles, out IntVec3 cell))
            {
                Job goto_ = JobMaker.MakeJob(JobDefOf.Goto, cell);
                goto_.locomotionUrgency = LocomotionUrgency.Sprint;
                goto_.expiryInterval = 100;
                goto_.checkOverrideOnExpire = true;
                return goto_;
            }

            // Nowhere safer to go inside the structure: tend in place rather than
            // keep fighting while critically wounded.
            return TendJob(pawn);
        }

        private bool IsCritical(Pawn pawn)
        {
            Pawn_HealthTracker health = pawn.health;
            if (health.summaryHealth.SummaryHealthPercent <= criticalHealthThreshold)
                return true;
            if (health.hediffSet.BleedRateTotal >= criticalBleedRateThreshold)
                return true;
            return false;
        }

        private static Job TendJob(Pawn pawn)
        {
            Job job = JobMaker.MakeJob(JobDefOf.TendPatient, pawn);
            job.endAfterTendedOnce = true;
            return job;
        }

        private List<Thing> GatherHostiles(Pawn pawn)
        {
            var result = new List<Thing>();
            IReadOnlyList<Pawn> all = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < all.Count; i++)
            {
                Pawn other = all[i];
                if (other == pawn || other.Downed || other.Dead)
                    continue;
                if (!other.HostileTo(pawn))
                    continue;
                if (!other.Position.InHorDistOf(pawn.Position, dangerScanRadius))
                    continue;
                result.Add(other);
            }
            return result;
        }

        private static bool CellOutOfSight(IntVec3 cell, Map map, List<Thing> hostiles)
        {
            for (int i = 0; i < hostiles.Count; i++)
            {
                if (GenSight.LineOfSight(hostiles[i].Position, cell, map, skipFirstCell: true))
                    return false;
            }
            return true;
        }

        private bool TryFindWithdrawCell(Pawn pawn, List<Thing> hostiles, out IntVec3 cell)
        {
            Map map = pawn.Map;
            Thing nearest = NearestHostile(pawn, hostiles);
            // Direction pointing away from the nearest hostile; biases the search
            // toward cells on the far side of the pawn rather than ones that
            // sidestep toward the threat.
            IntVec3 awayVec = pawn.Position - nearest.Position;

            IntVec3 bestSightBreak = IntVec3.Invalid; // nearest LoS break, any direction
            IntVec3 bestCover = IntVec3.Invalid;       // best partial-cover fallback
            float bestCoverScore = -1f;

            // RadialCellsAround yields nearest-first, so the first away-facing
            // LoS break we hit is the closest one — a short withdrawal.
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(pawn.Position, withdrawSearchRadius, useCenter: false))
            {
                if (!candidate.InBounds(map))
                    continue;
                if (!StructureBoundsCache.Contains(map, candidate))
                    continue;
                if (!candidate.Standable(map))
                    continue;
                if (!pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Deadly))
                    continue;

                if (CellOutOfSight(candidate, map, hostiles))
                {
                    if (IsAwayFromHostile(candidate - pawn.Position, awayVec))
                    {
                        cell = candidate; // nearest break that also retreats — best.
                        return true;
                    }
                    if (!bestSightBreak.IsValid)
                        bestSightBreak = candidate;
                }
                else if (!bestSightBreak.IsValid)
                {
                    // Any LoS break beats partial cover, so only score cover while
                    // none has been found yet (stops once one is).
                    float score = MinBlockChance(candidate, map, hostiles);
                    if (score > bestCoverScore)
                    {
                        bestCoverScore = score;
                        bestCover = candidate;
                    }
                }
            }

            if (bestSightBreak.IsValid)
            {
                cell = bestSightBreak;
                return true;
            }

            if (bestCover.IsValid && bestCoverScore >= coverFallbackThreshold)
            {
                cell = bestCover;
                return true;
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private static Thing NearestHostile(Pawn pawn, List<Thing> hostiles)
        {
            Thing nearest = hostiles[0];
            float bestSq = pawn.Position.DistanceToSquared(nearest.Position);
            for (int i = 1; i < hostiles.Count; i++)
            {
                float sq = pawn.Position.DistanceToSquared(hostiles[i].Position);
                if (sq < bestSq)
                {
                    bestSq = sq;
                    nearest = hostiles[i];
                }
            }
            return nearest;
        }

        // Non-negative dot product => the move has no component toward the hostile
        // (away or lateral). A degenerate awayVec (hostile on the pawn's cell)
        // accepts everything.
        private static bool IsAwayFromHostile(IntVec3 moveDir, IntVec3 awayVec)
        {
            return moveDir.x * awayVec.x + moveDir.z * awayVec.z >= 0;
        }

        // Worst-case ranged cover the cell offers across all hostiles: a cell only
        // counts as "behind cover" if it is shielded from every threat, not just one.
        private static float MinBlockChance(IntVec3 cell, Map map, List<Thing> hostiles)
        {
            float min = 1f;
            for (int i = 0; i < hostiles.Count; i++)
            {
                float chance = CoverUtility.CalculateOverallBlockChance(cell, hostiles[i].Position, map);
                if (chance < min)
                    min = chance;
            }
            return min;
        }
    }
}
