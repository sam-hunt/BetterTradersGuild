using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.AI.Civilians
{
    // Defend-posture first step: melee the involved attacker nearest to the caretaker (the
    // pawn actively targeting a family member - see PartyThreatHelper for what qualifies).
    // Adults only, mirroring the hack giver - children never fight. When no involved attacker
    // is REACHABLE (e.g. shooting from behind cover the caretaker can't path to), this yields
    // nothing and the duty falls through to the normal escape chain, so a kiting attacker can
    // never stall the evacuation.
    //
    // The short job expiry re-picks the target as stances shift; killIncappedTarget stays
    // false because downing the attacker IS neutralizing them - a downed pawn no longer
    // targets anyone, so the threat scan clears and the lord disengages back to escaping
    // rather than executing anybody.
    public class JobGiver_BTGDefendParty : ThinkNode_JobGiver
    {
        private const int JobExpiryTicks = 200;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.DevelopmentalStage.Adult())
                return null;

            Lord lord = pawn.GetLord();
            if (lord == null)
                return null;

            Pawn attacker = PartyThreatHelper.FindAttackerOfParty(lord, pawn);
            if (attacker == null)
                return null;
            if (!pawn.CanReach(attacker, PathEndMode.Touch, Danger.Deadly))
                return null;

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, attacker);
            job.expiryInterval = JobExpiryTicks;
            job.checkOverrideOnExpire = true;
            job.killIncappedTarget = false;
            return job;
        }
    }
}
