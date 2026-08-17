using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // Single source of truth for the sheltering-civilian lord.
    //
    // Babies/newborns are deliberately NOT added to the lord - they stay autonomous in their
    // cribs and are tended via faction scans - so only the caretaker and walking children
    // become members. The infants are still passed through to the LordJob, which remembers
    // them to scope the escape leg's carry-target scan to the shelter's own babies.
    public static class CivilianLords
    {
        public static Lord MakeShelterLord(Map map, Faction faction, IntVec3 subroomCenter, List<Pawn> walkers, List<Pawn> infants)
        {
            if (faction == null || walkers == null || walkers.Count == 0)
                return null;

            Lord lord = LordMaker.MakeNewLord(faction, new LordJob_BTGShelterCivilians(subroomCenter, infants), map);
            for (int i = 0; i < walkers.Count; i++)
            {
                if (walkers[i] != null)
                    lord.AddPawn(walkers[i]);
            }
            return lord;
        }
    }
}
