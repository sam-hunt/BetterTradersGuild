using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.PawnPatches
{
    // Blocks the player's Adopt designator on faction infants while any guardian is still
    // active. Vanilla offers Adopt on any baby of a non-player faction, which lets the
    // player scoop an infant out from under the people caring for it - kidnapping, not
    // adoption. A guardian is any live, un-downed, spawned humanlike of the baby's faction
    // that is not itself an infant: the sheltering family's walkers, and the garrison at
    // large - the defenders' duty chain tucks and bottle-feeds faction babies, so the whole
    // garrison is genuinely tending them. Children count too (they back-fill baby care when
    // no adult remains); mechs don't (ChildcareUtility's Humanlike gate means they can't
    // feed anyone).
    //
    // The rule naturally re-enables adoption at exactly the right story beats: every
    // guardian dead, downed, or evacuated - e.g. the sheltering family flew off leaving an
    // orphan behind (the lift-off logic permits that rather than deadlocking the launch),
    // and the garrison has since fallen. An abandoned baby becoming a legitimate rescue is
    // a feature.
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

        private static bool AnyGuardianActive(Map map, Faction faction)
        {
            List<Pawn> facPawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < facPawns.Count; i++)
            {
                Pawn p = facPawns[i];
                if (p == null || p.Dead || p.Downed)
                    continue;
                if (p.RaceProps == null || !p.RaceProps.Humanlike)
                    continue;
                if (p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Newborn())
                    continue;
                return true;
            }
            return false;
        }
    }
}
