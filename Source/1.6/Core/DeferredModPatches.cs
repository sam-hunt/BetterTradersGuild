using System;
using HarmonyLib;
using Verse;

namespace BetterTradersGuild
{
    // Deferred application of the Harmony patches that target OTHER MODS' methods.
    //
    // Why they can't ride the ctor-time PatchAll: applying a detour makes MonoMod compile the
    // target method, and compiling a method runs its declaring type's static initializer. Mod
    // constructors execute during LoadedModManager.CreateModClasses — before ANY defs are
    // loaded — so a target type whose static fields resolve defs blows up and, worse, stays
    // broken: a cctor runs once per process, so its def fields are permanently null for the
    // session. Real case (v1.1.0): patching CWTL's TransportersArrivalAction_CWTLAttackSettlement
    // .CanAttack fired its cctor, whose fixedArrivalMode field initializer is
    // DefDatabase<PawnsArrivalModeDef>.GetNamed("CWTL_ChooseWhereToLand") — a red error at
    // startup for every BTG+CWTL player, and an NRE inside CWTL's Arrived() whenever they
    // actually landed an attack. BTG's own patch targets are safe (vanilla cctors don't resolve
    // defs inline, and BTG types follow the BTGStartup rules), but another mod's cctor contents
    // are outside our control and one update away from changing, so EVERY patch on another
    // mod's method goes through here.
    //
    // How: the deferred classes' Prepare gates on PassActive, so the ctor-time PatchAll passes
    // (initial load, and the reload re-runs) skip them silently; BTGStartup.Run — which fires
    // after defs, DefOf rebinding and language injection — calls ApplyAll, which patches each
    // class explicitly. By then any def lookup a target's cctor makes resolves normally.
    //
    // ApplyAll is once per process, not per load: Harmony patches survive a play-data reload,
    // and reapplying would stack duplicate postfixes. Per-class try/catch so one broken target
    // (an optional mod's API drift) can't abort the others — unlike PatchAll, where one throwing
    // processor kills every later patch.
    public static class DeferredModPatches
    {
        private static readonly Type[] DeferredPatchClasses =
        {
            typeof(Patches.TransportersArrivalActionPatches.CWTLAttackSettlementCanAttack),
            typeof(Patches.MapGenerationPatches.CompOutfitStandHARPostSpawnSetup),
            typeof(Patches.BuildingAndroidStandPatches.BuildingAndroidStandCannotUseNowReason),
        };

        private static bool applied;

        // True only while ApplyAll is patching; the deferred classes' Prepare methods AND-in
        // this flag so every other patching pass leaves them alone.
        public static bool PassActive { get; private set; }

        public static void ApplyAll()
        {
            if (applied)
                return;
            applied = true;

            PassActive = true;
            try
            {
                foreach (Type patchClass in DeferredPatchClasses)
                {
                    try
                    {
                        new PatchClassProcessor(BetterTradersGuildMod.Harmony, patchClass).Patch();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[Better Traders Guild] Deferred patch " + patchClass.Name
                            + " failed to apply; its integration will be inactive this session: " + ex);
                    }
                }
            }
            finally
            {
                PassActive = false;
            }
        }
    }
}
