using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    // Harmony patch: SettlementDefeatUtility.IsDefeated
    //
    // Replaces the defeat verdict on TradersGuild settlement maps with BTG's own
    // rule: defeated once 80% of the map's original active security (entrenched
    // garrison humans + roaming sentry drones) is incapacitated. Full predicate and
    // rationale live in SecurityDefeatUtility; the den twin is
    // SiteCheckAllEnemiesDefeated.
    //
    // A full override in both directions, because vanilla's test mis-scores BTG
    // settlements both ways:
    //
    //   - Too eager: IsDefeated -> IsActiveThreatToPlayer -> IsPotentialThreat
    //     rejects any pawn that is not RestUtility.Awake. The entrenched-defender
    //     duty (BTG_DefendStructure) has a rest subnode, so during a combat lull
    //     every human guard can be asleep at once and vanilla reports the base
    //     defeated though the garrison would wake within ~3.5s.
    //
    //   - Too strict: vanilla requires every last defender out, where BTG wants the
    //     80% collapse threshold (the space-map replacement for vanilla's ground
    //     rout at 40-70% lost; see SecurityDefeatUtility).
    //
    // Mechs never enter vanilla's verdict (it gates on RaceProps.Humanlike) and BTG
    // now agrees for all but sentry drones: worker mechs and room-bound security
    // militors are excluded like the turrets they functionally are, while the
    // roaming drones count toward the threshold.
    //
    // The override disarms structurally at defeat: CheckDefeated reparents the map
    // to a DestroyedSettlement in the same call that destroys the Settlement, so
    // the map-parent check below fails on every later query and abandon-phase
    // survivors (still LordJob_BTGDefendStructure members) never read as garrison.
    [HarmonyPatch(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.IsDefeated))]
    public static class SettlementDefeatUtilityIsDefeated
    {
        [HarmonyPostfix]
        public static void Postfix(Map map, Faction faction, ref bool __result)
        {
            if (map == null || faction == null)
                return;

            if (!TradersGuildHelper.IsMapInTradersGuildSettlement(map))
                return;

            __result = SecurityDefeatUtility.IsSecurityDefeated(map, faction);
        }
    }
}
