using System.Collections.Generic;
using BetterTradersGuild.LordJobs;
using BetterTradersGuild.LordJobs.Mechs;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.Patches.SettlementPatches
{
    // Harmony patch: SettlementDefeatUtility.IsDefeated
    //
    // Fixes the "base destroyed" letter firing while defenders are still alive. Vanilla's
    // defeat test mis-scores BTG settlements for two independent reasons:
    //
    //   1. Sleeping human defenders don't count. IsDefeated -> IsActiveThreatToPlayer ->
    //      IsPotentialThreat rejects any pawn that is not RestUtility.Awake. The entrenched-
    //      defender duty (BTG_DefendStructure) has a rest subnode, so during a combat lull -
    //      or when the player attacks from outside the room rects the defenders can target -
    //      every human guard can be asleep at once. The vanilla check then reports the base
    //      defeated though the whole garrison is alive and would wake within ~3.5s.
    //
    //   2. Mechs don't count at all. IsDefeated gates each pawn on RaceProps.Humanlike, so it
    //      ignores every guild mechanoid (roaming sentry drones, security-post mechs, gestator
    //      reinforcements) - all TradersGuild-faction here, so all present in
    //      SpawnedPawnsInFaction. Vanilla settlements never field mechs, so vanilla never hits
    //      this; BTG does. Unpatched, the defeat letter - the game-wide "you're safe now"
    //      signal every other settlement raid honours - fires while live hostile mechs are
    //      still hunting the player, a jarring break from that guarantee.
    //
    // Postfix only: when vanilla has concluded "defeated", override back to not-defeated if a
    // genuine defender remains. Scoped to TradersGuild settlement maps.
    //
    //   - Humans: only a LordJob_BTGDefendStructure member that is alive, not downed, and not
    //     in a mental break holds the base (asleep is fine - that is the bug being fixed).
    //     Scoping to the defender lord is what excludes the sheltering CIVILIANS (the separate
    //     LordJob_BTGShelterCivilians walkers plus the autonomous crib infants). Without that
    //     scope an always-asleep newborn would make the settlement impossible to defeat.
    //
    //   - Mechs: any non-humanlike pawn still an active threat by vanilla's own test
    //     (GenHostility.IsActiveThreatToPlayer) holds the base, unless its lord job is one of
    //     the BTG worker jobs (clean, farm, medic, stay-in-area) - those mechs are awake and
    //     "active" by vanilla's test but are never combatants, so an awake cleansweeper or
    //     paramedic must not keep the base undefeated once every real threat is gone. Sentries
    //     roam lordless and room-part defenders use other lords, so both still block as before.
    [HarmonyPatch(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.IsDefeated))]
    public static class SettlementDefeatUtilityIsDefeated
    {
        [HarmonyPostfix]
        public static void Postfix(Map map, Faction faction, ref bool __result)
        {
            // Only act when vanilla already believes the base is defeated.
            if (!__result || map == null || faction == null)
                return;

            if (!TradersGuildHelper.IsMapInTradersGuildSettlement(map))
                return;

            List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Downed)
                    continue;

                if (p.RaceProps.Humanlike)
                {
                    // Entrenched garrison only - never the sheltering civilians.
                    if (p.GetLord()?.LordJob is LordJob_BTGDefendStructure && !p.InMentalState)
                    {
                        __result = false;
                        return;
                    }
                }
                else if (GenHostility.IsActiveThreatToPlayer(p) && !IsWorkerMech(p))
                {
                    // Live hostile mechanoid still hunting - excludes worker mechs, which
                    // register as an "active threat" while awake despite never fighting.
                    __result = false;
                    return;
                }
            }
        }

        // Worker mechs (cleaner, farmer, medic, area-stay) are non-combat labor. They pass
        // GenHostility.IsActiveThreatToPlayer whenever awake, but must not block defeat.
        private static bool IsWorkerMech(Pawn p)
        {
            LordJob job = p.GetLord()?.LordJob;
            return job is LordJob_MechClean
                || job is LordJob_MechFarm
                || job is LordJob_MechMedic
                || job is LordJob_StayInArea;
        }
    }
}
