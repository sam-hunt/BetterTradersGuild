using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.Helpers.RoomContents;
using BetterTradersGuild.Integrations;
using BetterTradersGuild.Patches.CaravanPatches;
using BetterTradersGuild.Patches.MechGestatorPatches;
using BetterTradersGuild.Patches.PawnNameColorUtilityPatches;
using BetterTradersGuild.Patches.SettlementPatches;
using BetterTradersGuild.Patches.SitePatches;
using BetterTradersGuild.Patches.WorldObjectPatches;

namespace BetterTradersGuild
{
    // Single startup trigger for all of BTG's reflection self-checks (pattern ported from
    // UniqueWeaponsUnbound). Each reflecting class owns and caches its own FieldInfo/MethodInfo
    // and exposes a VerifyReflection() that logs a targeted, consequence-naming error if a
    // member failed to resolve; each optional-mod integration self-reports drift from its own
    // resolution point (a static constructor, except UMWIntegration's per-load Resolve).
    //
    // This is a central trigger, not a central registry: every reflected member
    // name still lives in exactly one owner, so nothing is declared twice and nothing can drift
    // apart. Called once per play-data load from BTGStartup.Run(), so API drift surfaces at
    // startup rather than as a silent feature failure (or a player's bug report) much later.
    // The type/member caches themselves survive an in-process reload (assemblies are not
    // reloaded), so re-running the checks is a cheap no-op unless something drifted.
    public static class ReflectionVerification
    {
        public static void VerifyAll()
        {
            // Base-game (RimWorld) reflection — hard dependencies, Log.Error on drift.
            TraderTrackerReflection.VerifyReflection();
            CompHackableReflection.VerifyReflection();
            RefuelableReflection.VerifyReflection();
            MapGenUtilityReflection.VerifyReflection();
            SiteReflection.VerifyReflection();
            PawnNameColorUtilityPawnNameColorOf.VerifyReflection();
            UniqueWeaponNameColorRegenerator.VerifyReflection();
            UniqueWeaponAbilityResetter.VerifyReflection();
            CompMechGestatorTankTrigger.VerifyReflection();
            WorldObjectRequiresSignalJammer.VerifyReflection();
            TransportersArrivalActionTradeArrived.VerifyReflection();
            CaravanMakerMakeCaravan.VerifyPatched();
            SiteCheckAllEnemiesDefeated.VerifyPatched();
            TransportersArrivalActionTradeCanTradeWith.VerifyPatched();
            TransportersArrivalActionTradeArrived.VerifyPatched();

            // Optional-mod integrations — soft dependencies. Forcing each static constructor makes
            // it resolve now and self-report drift (silent unless the mod is present but shifted).
            // These cache only types/members, which survive an in-process play-data reload.
            _ = HARIntegration.Available;
            _ = VEPipesIntegration.Available;
            _ = CWTLIntegration.Available;
            _ = VREAIntegration.Available;

            // UMW is consumed through def instances, which do NOT survive a reload — its
            // resolution is an explicit idempotent call so it re-runs here every load.
            UMWIntegration.Resolve();
        }
    }
}
