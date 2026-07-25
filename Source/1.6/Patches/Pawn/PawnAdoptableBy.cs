using System.Collections.Generic;
using System.Text;
using BetterTradersGuild.LordJobs.Civilians;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.PawnPatches
{
    // Blocks the player's Adopt designator on the sheltering family's infants while any
    // guardian is still active. Vanilla offers Adopt on any baby of a non-player faction,
    // which lets the player scoop the family's infants out from under a living caretaker -
    // kidnapping, not adoption. The guardian rule (rather than a phase rule) blocks it in
    // every lord phase, including Stranded (a stranded family is still actively parenting),
    // and naturally re-enables it at exactly the right story beats: every walker dead or
    // downed, or the family evacuated leaving an infant behind (the lift-off logic permits
    // orphaned infants rather than deadlocking the launch) - an abandoned baby becoming a
    // legitimate rescue is a feature.
    //
    // Patch point: Pawn.AdoptableBy is the single gate Designator_Adopt consults for both
    // the designator and its reverse-designator gizmo (which shows the appended reason as
    // the disabled tooltip via showReverseDesignatorDisabledReason).
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.AdoptableBy))]
    public static class Pawn_AdoptableBy_Postfix
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, ref bool __result, StringBuilder reason)
        {
            if (!__result)
                return;

            Pawn baby = __instance;
            if (!(baby.DevelopmentalStage.Baby() || baby.DevelopmentalStage.Newborn()))
                return;

            Map map = baby.MapHeld;
            Faction faction = baby.Faction;
            if (map == null || faction == null)
                return;

            if (!AnyGuardianActive(map, faction))
                return;

            __result = false;
            reason?.AppendLine("BTG_AdoptBlockedByGuardians".Translate());
        }

        // True if a sheltering-civilian lord of the baby's faction still has a live,
        // un-downed walker on the map. Walkers only - the guardians are the caretaker and
        // walking children (children back-fill baby care when the adult dies, so they count).
        private static bool AnyGuardianActive(Map map, Faction faction)
        {
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord lord = lords[i];
                if (lord.faction != faction || !(lord.LordJob is LordJob_BTGShelterCivilians))
                    continue;

                List<Pawn> walkers = lord.ownedPawns;
                for (int j = 0; j < walkers.Count; j++)
                {
                    Pawn p = walkers[j];
                    if (p != null && !p.Dead && !p.Downed && p.Spawned)
                        return true;
                }
            }
            return false;
        }
    }
}
