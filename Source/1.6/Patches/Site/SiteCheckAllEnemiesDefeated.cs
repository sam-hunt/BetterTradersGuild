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
    // Adds BTG's defeat rule on smugglers den maps: defeated once the
    // securityDefeatFraction setting's share (default 80%) of the map's original
    // active security (entrenched garrison humans + roaming sentry drones) is
    // incapacitated. Full predicate and rationale live in SecurityDefeatUtility;
    // the settlement twin is SettlementDefeatUtilityIsDefeated.
    //
    // Vanilla's own check can essentially never pass on a den: it requires
    // AnyHostileActiveThreatToPlayer(countDormantPawnsAsHostile: true,
    // canBeFogged: true) to go false, and that counts every powered hostile turret
    // (any IAttackTargetSearcher) plus dormant and fogged pawns. Den maps always
    // ship turrets (the required armory's corner turrets at minimum), so the
    // AllEnemiesDefeated latch - and with it quest success and the abandon-ship
    // phase - could not fire even with every pawn dead. Settlements never had this
    // problem because SettlementDefeatUtility.IsDefeated only ever counted
    // humanlike pawns; this patch brings the den in line with that.
    //
    // Additive postfix, never a skip: the original always runs, so other mods'
    // prefixes, transpilers, and postfixes on this method keep their full
    // semantics. Letting vanilla go first can never produce a different outcome,
    // because its latch condition (zero active threats, turrets included) is a
    // strict subset of BTG's threshold - any state vanilla would latch on, BTG's
    // rule has already latched on or latches in the same call. When the threshold
    // fires first, the postfix mirrors vanilla's send-then-latch exactly: fire the
    // AllEnemiesDefeated quest signal (QuestNode_End(Success) in
    // Script_BTG_SmugglersDen.xml listens for it), set the site's private latch,
    // and refresh survivor name colors. The prefix only snapshots the latch so the
    // postfix can tell a fresh vanilla latch (refresh labels only) from an old one
    // (do nothing - defeat is final).
    //
    // Hooked here rather than on the quest's signal (a QuestPart races the
    // same-signal QuestNode_End, and the latch - not the quest - is the truth) or
    // the defenders' abandon transition (never fires when every defender died but
    // civilians survived: emptied lords are removed before their transitions run).
    // Cold path: sites tick on world-object intervals, and everything early-outs
    // once the latch is set.
    //
    // Degrades gracefully twice over: the target is resolved here rather than
    // string-named in the attribute, so on a rename (RimWorld API change) Prepare
    // skips this class instead of PatchAll throwing mid-run, and VerifyPatched
    // reports the drift at startup. And if the latch field is what drifted, the
    // postfix stays passive - sending the signal without latching would re-fire it
    // every world-object tick - leaving vanilla's (turret-gated) check in charge;
    // SiteReflection.VerifyReflection reports that consequence.
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
        public static void Prefix(Site __instance, out bool __state)
        {
            __state = SiteReflection.AllEnemiesDefeatedSent(__instance);
        }

        [HarmonyPostfix]
        public static void Postfix(Site __instance, bool __state)
        {
            if (__instance.def != WorldObjects.BTG_SmugglersDenSite)
                return;

            // Defeat is final: latched before this call means nothing left to do.
            if (__state)
                return;

            // Vanilla latched during this call (only reachable once every turret is
            // down too, a strict subset of BTG's threshold): the defeat side is
            // done, just recompute the survivor label state.
            if (SiteReflection.AllEnemiesDefeatedSent(__instance))
            {
                PawnNameColorUtilityPawnNameColorOf.Refresh();
                return;
            }

            // Without the latch field, stepping in would re-fire the quest signal
            // every world-object tick; stay passive (see class comment).
            if (SiteReflection.AllEnemiesDefeatedSignalSentField == null || !__instance.HasMap)
                return;

            if (!SecurityDefeatUtility.IsSecurityDefeated(__instance.Map, __instance.Faction))
                return;

            QuestUtility.SendQuestTargetSignals(__instance.questTags, "AllEnemiesDefeated",
                __instance.Named("SUBJECT"));
            SiteReflection.SetAllEnemiesDefeatedSent(__instance);
            PawnNameColorUtilityPawnNameColorOf.Refresh();
        }
    }
}
