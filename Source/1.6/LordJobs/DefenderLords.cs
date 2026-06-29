using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    // Single source of truth for which LordJob a TradersGuild settlement's
    // defenders get, gated on the "Entrenched defender AI" opt-out setting
    // (useEntrenchedDefenders). The two sites that create a defender lord from
    // scratch — the mapgen garrison (GenStep_BTGSettlementPawns) and gestator-
    // spawned reinforcement mechs (CompMechGestatorTankTrigger) — both route
    // through here so they can never disagree about which AI the player asked for.
    //
    // This decides FRESH creation only. A settlement's defender AI style is fixed
    // when its map is generated; flipping the setting mid-visit must not retro-
    // actively re-lord the existing garrison. So reinforcement mechs join the
    // existing garrison lord (IsDefenderLord, either style) and fall back to
    // MakeDefenderLordJob only when no garrison lord remains (e.g. the whole
    // garrison died and its lord was cleaned up) — there is nothing to stay
    // consistent with at that point, so honouring the current setting is correct.
    public static class DefenderLords
    {
        // Mirrors vanilla GenStep_SettlementPawnsLoot: delayBeforeAssault = 25000,
        // attackWhenPlayerBecameEnemy = false. Kept identical so the opt-out path
        // produces exactly the vanilla settlement defender behaviour.
        private const int VanillaAssaultDelayTicks = 25000;

        public static LordJob MakeDefenderLordJob(Faction faction, IntVec3 baseCenter)
        {
            if (BetterTradersGuildMod.Settings.useEntrenchedDefenders)
                return new LordJob_BTGDefendStructure(faction, baseCenter);

            return new LordJob_DefendBase(faction, baseCenter, VanillaAssaultDelayTicks, attackWhenPlayerBecameEnemy: false);
        }

        // True if the lord is a settlement defender garrison lord of either AI
        // style. Lets the gestator site find the existing garrison to reinforce
        // without caring which style the current setting selects.
        public static bool IsDefenderLord(Lord lord)
        {
            return lord?.LordJob is LordJob_BTGDefendStructure
                || lord?.LordJob is LordJob_DefendBase;
        }
    }
}
