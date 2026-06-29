using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // Single source of truth for the sheltering-civilian lord, gated on the same
    // "Entrenched defender AI" opt-out (useEntrenchedDefenders) that decides the garrison's
    // AI - so a settlement's defenders and its sheltering family always share one AI style.
    // Mirrors DefenderLords: when the setting is off, civilians are left lordless (today's
    // behaviour) and just stand around; the crib-placement fix and spawn rebalance in
    // ShelteringCivilianSpawner apply either way (they're plain improvements, not AI).
    //
    // Babies/newborns are deliberately NOT added to the lord - they stay autonomous in their
    // cribs and are tended/carried via faction scans - so only the caretaker and walking
    // children are passed here.
    public static class CivilianLords
    {
        public static Lord MakeShelterLordIfEnabled(Map map, Faction faction, IntVec3 subroomCenter, List<Pawn> walkers)
        {
            if (!BetterTradersGuildMod.Settings.useEntrenchedDefenders)
                return null;
            if (faction == null || walkers == null || walkers.Count == 0)
                return null;

            Lord lord = LordMaker.MakeNewLord(faction, new LordJob_BTGShelterCivilians(faction, subroomCenter), map);
            for (int i = 0; i < walkers.Count; i++)
            {
                if (walkers[i] != null)
                    lord.AddPawn(walkers[i]);
            }
            return lord;
        }
    }
}
