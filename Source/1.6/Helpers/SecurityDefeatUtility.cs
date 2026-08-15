using System.Collections.Generic;
using BetterTradersGuild.LordJobs;
using BetterTradersGuild.MapComponents;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild
{
    // Decides when a BTG-managed garrison (TradersGuild settlement or smugglers den)
    // counts as defeated: securityDefeatFraction of the map's original active
    // security incapacitated (default 80%).
    //
    // "Active security" is the mobile fighting force only - entrenched garrison
    // humans (LordJob_BTGDefendStructure members) plus roaming sentry drones
    // (CompSentryDrone, lordless). Static and room-bound defenses are deliberately
    // excluded, mirroring vanilla settlement defeat
    // (SettlementDefeatUtility.IsDefeated), which ignores everything but humanlike
    // pawns: turrets, hunter/wasp drone traps, worker mechs, and the room-bound
    // security mechs (vanilla LordJob_DefendPoint militors) neither block defeat nor
    // count toward the threshold. A counted pawn stops being active when it is dead,
    // despawned, downed, captured, or in a mental state.
    //
    // Vanilla reference for the 80% default: ground settlement defenders rout at a
    // seeded 40-70% of the lord lost (Lord.SetJob injects a panic-flee toil gated on
    // FactionDef.attackersDownPercentageRangeForAutoFlee), and fleeing pawns stop
    // blocking defeat. Space maps never get that toil (the Space biome's
    // canExitMap=false makes Map.CanEverExit false), so this threshold is the
    // give-up mechanism on BTG maps; it sits above vanilla's range because the
    // guild conducts an orderly evacuation, not a rout. Static rather than seeded:
    // defender spawn positions are already random (some land behind locked subroom
    // doors), so the player cannot game an exact head-count anyway.
    //
    // The fraction is a player setting (securityDefeatFraction, 0.5-1.0), read live
    // on every query rather than latched at map generation, so a mid-visit change
    // applies immediately.
    public static class SecurityDefeatUtility
    {
        public const float DefaultDefeatedFraction = 0.8f;

        public static float DefeatedFraction =>
            BetterTradersGuildMod.Settings.securityDefeatFraction;

        // Whether the map's garrison has collapsed past the threshold. owner is the
        // garrison faction (settlement faction / den site faction). Callers gate on
        // map identity; this only does the arithmetic.
        public static bool IsSecurityDefeated(Map map, Faction owner)
        {
            if (map == null || owner == null)
                return false;

            SecurityCensus census = map.GetComponent<SecurityCensus>();
            int initial = census != null ? census.InitialSecurity : SecurityCensus.Unknown;
            return MeetsDefeatThreshold(initial, CountActiveSecurity(map, owner), DefeatedFraction);
        }

        // Pure threshold arithmetic, split out for unit tests (which cannot read mod
        // settings, hence the explicit fraction). Vanilla float-compare idiom
        // (Trigger_FractionPawnsLost): defeated once incapacitated count reaches
        // initial * fraction, so small garrisons round toward requiring more, never
        // fewer (initial 4 at 0.8 -> 4 incapacitated, not 3). initial <= 0 means the
        // census is missing (map predates the component, or no security ever
        // generated): fall back to requiring zero active security.
        public static bool MeetsDefeatThreshold(int initial, int active, float fraction)
        {
            if (initial <= 0)
                return active == 0;

            return initial - active >= initial * fraction;
        }

        // Counts the currently active security on the map. Also used by
        // SecurityCensus at map generation to latch the denominator, so the two
        // sides of the fraction can never disagree about who counts.
        public static int CountActiveSecurity(Map map, Faction owner)
        {
            int count = 0;
            List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(owner);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.Downed != false)
                    continue;

                if (p.RaceProps.Humanlike)
                {
                    // Entrenched garrison only - never the sheltering civilians
                    // (LordJob_BTGShelterCivilians) or crib infants. Asleep still
                    // counts: the garrison rests in shifts and wakes in seconds.
                    if (!p.InMentalState && p.GetLord()?.LordJob is LordJob_BTGDefendStructure)
                        count++;
                }
                else if (p.TryGetComp<CompSentryDrone>() != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
