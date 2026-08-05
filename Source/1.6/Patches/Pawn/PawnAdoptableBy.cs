using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.PawnPatches
{
    // Blocks the player's Adopt designator on faction infants while any guardian of THAT
    // infant is still active. Vanilla offers Adopt on any baby of a non-player faction,
    // which lets the player scoop an infant out from under the people caring for it -
    // kidnapping, not adoption. A guardian is any live, un-downed, spawned humanlike of the
    // baby's faction that is not itself an infant AND can currently reach the baby - the
    // same reachability the childcare givers gate on, so the gizmo state always tracks
    // whether someone can actually tend this baby. The reach test matters: a caretaker
    // alive but sealed inside the locked nursery holdout is no guardian to a crew-quarters
    // baby on the far side of that door (a locked blast door blocks CanReach even for its
    // own faction), so that baby correctly becomes adoptable when the rest of the garrison
    // falls. Children count as guardians (they back-fill baby care when no adult remains);
    // mechs don't (ChildcareUtility's Humanlike gate means they can't feed anyone).
    //
    // The rule naturally re-enables adoption at exactly the right story beats: every
    // guardian who could reach the baby dead, downed, or evacuated - e.g. the sheltering
    // family flew off leaving an orphan behind (the lift-off logic permits that rather than
    // deadlocking the launch), and the garrison has since fallen. An abandoned baby becoming
    // a legitimate rescue is a feature. Vanilla's Alert_AbandonedBaby consults this same
    // method as its final filter, so the warning stays in lockstep with the gizmo.
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

            if (!AnyGuardianActive(baby, map, faction))
                return;

            __result = false;
            reason?.AppendLine("BTG_AdoptBlockedByGuardians".Translate());
        }

        private static bool AnyGuardianActive(Pawn baby, Map map, Faction faction)
        {
            // PositionHeld rather than the baby Thing itself: a baby mid-carry is despawned
            // into the carrier, and a cell destination stays valid either way.
            IntVec3 babyPos = baby.PositionHeld;
            if (!babyPos.IsValid)
                return false;

            List<Pawn> facPawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < facPawns.Count; i++)
            {
                Pawn p = facPawns[i];
                if (p?.Dead != false || p.Downed)
                    continue;
                if (p.RaceProps?.Humanlike != true)
                    continue;
                if (p.DevelopmentalStage.Baby() || p.DevelopmentalStage.Newborn())
                    continue;
                if (!p.CanReach(babyPos, PathEndMode.Touch, Danger.Deadly))
                    continue;
                return true;
            }
            return false;
        }
    }
}
