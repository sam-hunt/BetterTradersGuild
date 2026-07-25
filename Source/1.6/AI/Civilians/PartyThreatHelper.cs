using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.AI.Civilians
{
    // Detects who is actively attacking the sheltering-civilian family, for the reactive
    // self-defense posture (LordToil_BTGDefend). The design goal is that the aggressor -
    // typically the player - stays the clear aggressor: the caretaker only ever fights a pawn
    // whose CURRENT aggression points at a family member, so bystanders, non-combatants, and
    // pawns fighting someone else are ignored completely.
    //
    // "Party" = the lord's walkers (caretaker + children, downed included - they still need
    // protecting) plus the family's autonomous infants (spawned faction babies, matching the
    // faction + developmental-stage scan the feed/carry givers use).
    //
    // An "involved attacker" is a live, un-downed pawn hostile to the lord's faction whose
    // stance focus (covers ranged aim warmup and cooldown between shots) or current attack
    // job targets a party member. Deliberately pawns-only: turrets are excluded - charging an
    // autocannon with a knife is not posturing, and fleeing remains the answer to static
    // defences. And deliberately not player-specific: keyed on hostility like
    // ShelterCompromised, so raiders attacking the family get the same reaction.
    public static class PartyThreatHelper
    {
        public static bool AnyPartyMemberTargeted(Lord lord)
        {
            return FindAttackerOfParty(lord, null) != null;
        }

        // The involved attacker nearest to seeker, or any involved attacker when seeker is
        // null (cheap existence check for the lord's transitions).
        public static Pawn FindAttackerOfParty(Lord lord, Pawn seeker)
        {
            Map map = lord?.Map;
            Faction faction = lord?.faction;
            if (map == null || faction == null)
                return null;

            HashSet<Thing> party = BuildParty(lord, map, faction);
            if (party.Count == 0)
                return null;

            Pawn best = null;
            float bestDistSq = float.MaxValue;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Downed)
                    continue;
                if (!p.HostileTo(faction))
                    continue;
                if (!IsTargetingAnyOf(p, party))
                    continue;

                if (seeker == null)
                    return p;

                float distSq = (seeker.Position - p.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = p;
                }
            }
            return best;
        }

        private static HashSet<Thing> BuildParty(Lord lord, Map map, Faction faction)
        {
            var party = new HashSet<Thing>();

            List<Pawn> walkers = lord.ownedPawns;
            for (int i = 0; i < walkers.Count; i++)
            {
                if (walkers[i] != null && !walkers[i].Dead)
                    party.Add(walkers[i]);
            }

            List<Pawn> facPawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < facPawns.Count; i++)
            {
                Pawn p = facPawns[i];
                if (p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Newborn())
                    party.Add(p);
            }
            return party;
        }

        private static bool IsTargetingAnyOf(Pawn attacker, HashSet<Thing> party)
        {
            if (attacker.stances?.curStance is Stance_Busy busy
                && busy.focusTarg.HasThing && party.Contains(busy.focusTarg.Thing))
            {
                return true;
            }

            Job job = attacker.CurJob;
            if (job != null && (job.def == JobDefOf.AttackMelee || job.def == JobDefOf.AttackStatic))
            {
                LocalTargetInfo target = job.targetA;
                if (target.HasThing && party.Contains(target.Thing))
                    return true;
            }
            return false;
        }
    }
}
