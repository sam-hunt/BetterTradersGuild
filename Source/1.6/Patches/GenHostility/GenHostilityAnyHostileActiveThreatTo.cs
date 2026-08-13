using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.LordJobs;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.Patches.GenHostilityPatches
{
    // Harmony patch: GenHostility.AnyHostileActiveThreatTo
    //
    // Fixes the smugglers den quest succeeding while defenders are merely asleep. The
    // den quest completes on the site.AllEnemiesDefeated signal, which vanilla
    // Site.CheckAllEnemiesDefeated fires when AnyHostileActiveThreatToPlayer returns
    // false. That check rejects any pawn that is not RestUtility.Awake (its
    // countDormantPawnsAsHostile fallback only rescues CompCanBeDormant mechs, never
    // humans resting in beds). The entrenched-defender duty (BTG_DefendStructure)
    // folds rest into its think tree, so during a combat lull the whole garrison can
    // be asleep at once - and the quest then completes as a Success with the garrison
    // alive and waking in seconds.
    //
    // This is the Site-flavored twin of SettlementDefeatUtilityIsDefeated: settlements
    // decide defeat via SettlementDefeatUtility.IsDefeated (patched there and never
    // called for Sites), while Sites decide it here. Mechs need no carve-out in this
    // path - AnyHostileActiveThreatTo iterates the attack-target cache with no race
    // filter, so hostile mechs already count while awake.
    //
    // Like that twin, this gate disarms at defeat - defeat is final. The settlement
    // patch disarms structurally (its map-parent check fails once the map reparents to
    // DestroyedSettlement); the den map keeps its Site parent forever, so the site's
    // latched allEnemiesDefeatedSignalSent is the equivalent finality signal. Without
    // the early-out, a medic-recovered defender would re-arm this override after the
    // quest already resolved as a Success: post-defeat survivors are still members of
    // LordJob_BTGDefendStructure (the abandon-ship phase is a toil, not a different
    // LordJob), so the lord-membership filter below cannot tell them apart.
    //
    // Postfix only: when vanilla concludes "no active threat", override back to
    // threatened if a genuine garrison member remains - alive, not downed, not in a
    // mental break, member of the entrenched-defender lord (which excludes sheltering
    // civilians), and actually hostile to the asking faction (so a query about threats
    // to the defenders' own faction is never flipped). Scoped to smugglers den site
    // maps; every other map keeps vanilla behavior.
    [HarmonyPatch(typeof(GenHostility), nameof(GenHostility.AnyHostileActiveThreatTo),
        new[] { typeof(Map), typeof(Faction), typeof(IAttackTarget), typeof(bool), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
    public static class GenHostilityAnyHostileActiveThreatTo
    {
        [HarmonyPostfix]
        public static void Postfix(Map map, Faction faction, ref IAttackTarget threat, bool canBeFogged, ref bool __result)
        {
            // Only act when vanilla already believes no active threat remains.
            if (__result || map == null || faction == null)
                return;

            // Only smugglers den site maps; the settlement equivalent is handled by
            // the SettlementDefeatUtility.IsDefeated patch.
            if (!(map.Parent is Site site) || site.def != WorldObjects.BTG_SmugglersDenSite)
                return;

            // Defeat is final: once the site latches all-enemies-defeated, survivors
            // (including late medic recoveries) are on the abandon-ship chain and must
            // never read as garrison again (see class comment).
            if (SiteReflection.AllEnemiesDefeatedSent(site))
                return;

            Faction owner = map.ParentFaction;
            if (owner == null || owner == faction)
                return;

            List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(owner);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.Downed != false || p.InMentalState)
                    continue;

                // Entrenched garrison only - never the sheltering civilians. Asleep is
                // fine; that is the gap being closed.
                if (!(p.GetLord()?.LordJob is LordJob_BTGDefendStructure))
                    continue;

                // Honor the caller's contract: never report a pawn the asking faction
                // isn't actually hostile to, nor one hidden by fog it can't see through.
                if (!p.HostileTo(faction))
                    continue;
                if (!canBeFogged && p.Fogged())
                    continue;

                threat = p;
                __result = true;
                return;
            }
        }
    }
}
