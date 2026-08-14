using System.Reflection;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.Patches.PawnNameColorUtilityPatches;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.SitePatches
{
    // Harmony patch: Site.CheckAllEnemiesDefeated (private, hence the reflection lookup)
    //
    // Replaces the defeat verdict on smugglers den maps with BTG's own rule:
    // defeated once 80% of the map's original active security (entrenched garrison
    // humans + roaming sentry drones) is incapacitated. Full predicate and
    // rationale live in SecurityDefeatUtility; the settlement twin is
    // SettlementDefeatUtilityIsDefeated.
    //
    // Vanilla's check can never pass on a den: it requires
    // AnyHostileActiveThreatToPlayer(countDormantPawnsAsHostile: true,
    // canBeFogged: true) to go false, and that counts every powered hostile turret
    // (any IAttackTargetSearcher) plus dormant and fogged pawns. Den maps always
    // ship turrets (the required armory's corner turrets at minimum), so the
    // AllEnemiesDefeated latch - and with it quest success and the abandon-ship
    // phase - could never fire, even with every pawn dead. Settlements never had
    // this problem because SettlementDefeatUtility.IsDefeated only ever counted
    // humanlike pawns; this override brings the den in line with that.
    //
    // The prefix replaces the method wholesale for BTG dens (vanilla continues to
    // run for every other site): evaluate BTG's threshold, and on collapse mirror
    // vanilla's send-then-latch exactly - fire the AllEnemiesDefeated quest signal
    // (QuestNode_End(Success) in Script_BTG_SmugglersDen.xml listens for it), set
    // the site's private latch, and refresh survivor name colors. Hooked here
    // rather than on the quest's signal (a QuestPart races the same-signal
    // QuestNode_End, and the latch - not the quest - is the truth) or the
    // defenders' abandon transition (never fires when every defender died but
    // civilians survived: emptied lords are removed before their transitions run).
    // Cold path: sites tick on world-object intervals, and the latch check
    // early-outs every call after defeat.
    //
    // Degrades gracefully twice over: the target is resolved here rather than
    // string-named in the attribute, so on a rename (RimWorld API change) Prepare
    // skips this class instead of PatchAll throwing mid-run, and VerifyPatched
    // reports the drift at startup. And if the latch field is what drifted, the
    // prefix steps aside entirely - sending the signal without latching would
    // re-fire it every world-object tick - leaving vanilla's (turret-gated) check
    // in charge; SiteReflection.VerifyReflection reports that consequence.
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
                    + "smugglers den defeat falls back to vanilla's check, which counts powered turrets, so the "
                    + "den quest cannot succeed until every turret is destroyed or unpowered. "
                    + "RimWorld API may have changed.");
        }

        [HarmonyPrefix]
        public static bool Prefix(Site __instance)
        {
            if (__instance.def != WorldObjects.BTG_SmugglersDenSite)
                return true;

            // Without the latch field, stepping in would re-fire the quest signal
            // every world-object tick; let vanilla's own check run instead.
            if (SiteReflection.AllEnemiesDefeatedSignalSentField == null)
                return true;

            // Defeat is final; and the caller (Site.TickInterval) only runs this
            // with a map, but a guard costs nothing.
            if (SiteReflection.AllEnemiesDefeatedSent(__instance) || !__instance.HasMap)
                return false;

            if (!SecurityDefeatUtility.IsSecurityDefeated(__instance.Map, __instance.Faction))
                return false;

            QuestUtility.SendQuestTargetSignals(__instance.questTags, "AllEnemiesDefeated",
                __instance.Named("SUBJECT"));
            SiteReflection.SetAllEnemiesDefeatedSent(__instance);
            PawnNameColorUtilityPawnNameColorOf.Refresh();
            return false;
        }
    }
}
