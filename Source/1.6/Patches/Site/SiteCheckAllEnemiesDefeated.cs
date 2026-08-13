using System.Reflection;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.Patches.PawnNameColorUtilityPatches;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.SitePatches
{
    // Harmony patch: Site.CheckAllEnemiesDefeated (private, hence the reflection lookup)
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
    //
    // Degrades gracefully: the target is resolved here rather than string-named in
    // the attribute, so on a rename (RimWorld API change) Prepare skips this class
    // instead of PatchAll throwing mid-run and aborting every later patch;
    // VerifyPatched reports the drift at startup. The consequence is timing only:
    // the label override then first applies on the next save load
    // (BTGGameComponent) or settlement defeat instead of at the den's latch moment.
    [HarmonyPatch]
    public static class SiteCheckAllEnemiesDefeated
    {
        private static readonly MethodInfo Target = AccessTools.Method(
            typeof(Site), "CheckAllEnemiesDefeated");

        [HarmonyPrepare]
        public static bool Prepare()
        {
            return Target != null;
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return Target;
        }

        // Logs a targeted warning if the target failed to resolve. Called once at
        // startup by ReflectionVerification.VerifyAll after Harmony.PatchAll has run.
        public static void VerifyPatched()
        {
            if (Target == null)
                Log.Warning("[Better Traders Guild] Site.CheckAllEnemiesDefeated method not found via reflection; "
                    + "survivor name labels will not recolor at the moment a smugglers den is cleared "
                    + "(they still apply on the next save load or settlement defeat). RimWorld API may have changed.");
        }

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
