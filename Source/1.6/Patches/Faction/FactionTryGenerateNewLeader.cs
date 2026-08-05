using System;
using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.FactionPatches
{
    // Harmony finalizer on Faction.TryGenerateNewLeader.
    //
    // Leader generation builds a pawn, which can throw when another mod's pawn/hediff data is
    // malformed (the reported case was an NRE in PawnTechHediffsGenerator.GenerateTechHediffsFor
    // while installing the leader's implants). This is fatal to world generation: vanilla's
    // NewGeneratedFaction adds the faction's settlement and establishes its relations BEFORE
    // calling TryGenerateNewLeader, but FactionManager.Add runs only AFTER it returns. So an
    // exception here aborts NewGeneratedFaction with the settlement already on the map - the
    // faction is never registered, and the later-created player faction never relates to it. Every
    // subsequent relation query on that orphaned faction logs vanilla's "null relation" error,
    // spamming the log for the rest of the session.
    //
    // BTG raises the TradersGuild leader's failure surface specifically (its pawnkind patches crank
    // techHediffsChance and add required implants), so we guard the TradersGuild faction only:
    // swallow the leader-generation exception and keep the faction registered. RimWorld regenerates
    // a missing faction leader on demand, so a momentarily leaderless faction is harmless compared
    // to an unregistered, error-spamming one. Other factions are left to fail as vanilla would, so
    // we don't silently mask unrelated mods' world-generation bugs.
    [HarmonyPatch(typeof(Faction), nameof(Faction.TryGenerateNewLeader))]
    public static class FactionTryGenerateNewLeader
    {
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception, Faction __instance)
        {
            // No exception, or not our faction: leave vanilla behavior untouched. (Returning the
            // incoming exception re-throws it; returning null when there was none is a no-op.)
            if (__exception == null || __instance?.def != Factions.TradersGuild)
                return __exception;

            Log.Warning(
                "[Better Traders Guild] Suppressed an exception while generating the leader for "
                + (__instance.Name ?? __instance.def.defName) + ". The faction is kept (without a "
                + "leader, which RimWorld regenerates on demand) so world faction/relation setup is "
                + "not aborted. This is almost always a mod pawn/hediff conflict during leader "
                + "generation - underlying error:\n" + __exception);

            // Swallow the exception so NewGeneratedFaction returns and FactionManager.Add registers
            // the faction with its relations intact.
            return null;
        }
    }
}
