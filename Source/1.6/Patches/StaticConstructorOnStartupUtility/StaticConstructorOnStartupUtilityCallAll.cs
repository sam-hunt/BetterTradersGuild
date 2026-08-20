using HarmonyLib;
using Verse;

namespace BetterTradersGuild.Patches.StaticConstructorOnStartupUtilityPatches
{
    // Re-runs BTGStartup.Run on EVERY play-data load, where [StaticConstructorOnStartup]
    // would run it only once per process. This file is the full rationale for that
    // divergence; BTGStartup and CLAUDE.md carry only pointers here.
    //
    // Why the attribute's contract is too weak for us: BTGStartup's work is all state derived
    // from or written onto the live DefDatabase — the settings-driven def-field writes
    // (LifeSupportUnit power output, quest selection weights), UMWIntegration's resolved def
    // references, and every lazily built cache of def instances used by settlement map
    // generation. An in-process play-data reload (LanguageDatabase.SelectLanguage does
    // ClearAllPlayData + LoadAllPlayData; the mid-session language switch is the one
    // player-facing trigger) replaces every def instance, but a type initializer can never
    // run twice (StaticConstructorOnStartupUtility.CallAll goes through
    // RuntimeHelpers.RunClassConstructor, which no-ops on an initialized type). With
    // attribute-based startup the fresh defs are left unwritten and the caches keep serving
    // dead instances: the power/quest-weight settings silently revert to shipped XML values
    // until the next settings-window close, the nursery caretaker's UMW knife spawns from a
    // dead def, and room generation fills shelves/pots/subrooms from the previous load's
    // defs. Vanilla itself never needs a re-run hook: its own cross-load state is either
    // [DefOf] fields (rebound every load) or load-agnostic static texture/material caches —
    // mods that mutate defs own the re-application problem, and vanilla ships no standing
    // "play data loaded" callback (LongEventHandler.ExecuteWhenFinished only accepts
    // registrations from inside the load already in progress).
    //
    // Why THIS hook (decompile-verified, RimWorld 1.6): PlayDataLoader.DoPlayLoad queues its
    // finishing work as ExecuteWhenFinished delegates that run on the main thread after the
    // method returns, in order: InjectIntoData_AfterImpliedDefs (full DefInjected
    // application) + GenLabel.ClearCache → StaticConstructorOnStartupUtility.CallAll → atlas
    // baking → GC. A postfix on CallAll therefore fires at exactly the moment static ctors
    // run — after defs, cross-refs, DefOf rebinding and full language injection — on the
    // first load and on every reload, and it stays correct for any future reload trigger
    // because it hooks the load pipeline, not the language switch. On the first load the
    // patch is armed in time because mod constructors (where PatchAll runs) execute at the
    // very start of LoadAllPlayData — which is why BTG's PatchAll lives in the Mod subclass
    // constructor rather than a [StaticConstructorOnStartup] class: patching from inside
    // CallAll would be too late for this postfix to fire on the load that applies it.
    //
    // The trap this replaces: a postfix on PlayDataLoader.DoPlayLoad itself LOOKS equivalent
    // but fires before those queued delegates — i.e. before DefInjected is applied — and
    // would rebuild every cache from untranslated labels, silently reintroducing the bug
    // this fixes.
    //
    // Deliberately out of scope: dev-mode PlayDataLoader.HotReloadDefs never calls CallAll
    // (nor RebindAllDefOfs — vanilla does not uphold even its own DefOf contract there), so
    // def hot reload stays best-effort for us exactly as it is for vanilla.
    //
    // Everything Run() calls must stay idempotent: reloads make this fire repeatedly per
    // process, and the Mod constructor re-runs PatchAll on each reload besides.
    [HarmonyPatch(typeof(StaticConstructorOnStartupUtility), nameof(StaticConstructorOnStartupUtility.CallAll))]
    public static class StaticConstructorOnStartupUtilityCallAll
    {
        public static void Postfix()
        {
            BTGStartup.Run();
        }
    }
}
