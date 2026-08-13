using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.Patches.PawnNameColorUtilityPatches;
using HarmonyLib;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.SitePatches
{
    // Harmony patch: Site.CheckAllEnemiesDefeated (private, hence the name string)
    //
    // The den counterpart of the survivor-label Refresh call in
    // SettlementDefeatUtilityCheckDefeated: settlements announce defeat through
    // CheckDefeated, but a den's defeat is the Site latching its private
    // all-enemies-defeated signal inside this method. The prefix/postfix pair
    // detects that false-to-true edge and recomputes the label patch state.
    //
    // Deliberately hooked here rather than on the quest's AllEnemiesDefeated signal
    // (a QuestPart races the same-signal QuestNode_End, and the latch - not the
    // quest - is the truth) or the defenders' abandon transition (never fires when
    // every defender died but civilians survived: emptied lords are removed before
    // their transitions run). Always-applied but cold: sites tick on world-object
    // intervals and the method self-latches after firing once.
    [HarmonyPatch(typeof(Site), "CheckAllEnemiesDefeated")]
    public static class SiteCheckAllEnemiesDefeated
    {
        [HarmonyPrefix]
        public static void Prefix(Site __instance, out bool __state)
        {
            __state = SiteReflection.AllEnemiesDefeatedSent(__instance);
        }

        [HarmonyPostfix]
        public static void Postfix(Site __instance, bool __state)
        {
            if (__state || __instance.def != WorldObjects.BTG_SmugglersDenSite)
                return;

            if (!SiteReflection.AllEnemiesDefeatedSent(__instance))
                return;

            PawnNameColorUtilityPawnNameColorOf.Refresh();
        }
    }
}
