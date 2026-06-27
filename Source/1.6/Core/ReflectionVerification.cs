using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.Helpers.RoomContents;
using BetterTradersGuild.Integrations;
using BetterTradersGuild.Patches.MechGestatorPatches;
using BetterTradersGuild.Patches.PlanetTilePatches;
using BetterTradersGuild.Patches.SettlementPatches;
using BetterTradersGuild.Patches.WorldObjectPatches;

namespace BetterTradersGuild
{
    // Single startup trigger for all of BTG's reflection self-checks (pattern ported from
    // UniqueWeaponsUnbound). Each reflecting class owns and caches its own FieldInfo/MethodInfo
    // and exposes a VerifyReflection() that logs a targeted, consequence-naming error if a
    // member failed to resolve; each optional-mod integration self-reports drift from its own
    // static constructor.
    //
    // This is a central trigger, not a central registry: every reflected member
    // name still lives in exactly one owner, so nothing is declared twice and nothing can drift
    // apart. Called once from BetterTradersGuildMod's static constructor right after
    // Harmony.PatchAll(), so API drift surfaces at startup rather than as a silent feature
    // failure (or a player's bug report) much later.
    public static class ReflectionVerification
    {
        public static void VerifyAll()
        {
            // Base-game (RimWorld) reflection — hard dependencies, Log.Error on drift.
            TraderTrackerReflection.VerifyReflection();
            CompHackableReflection.VerifyReflection();
            RefuelableReflection.VerifyReflection();
            UniqueWeaponNameColorRegenerator.VerifyReflection();
            CompMechGestatorTankTrigger.VerifyReflection();
            WorldObjectRequiresSignalJammer.VerifyReflection();
            TransportersArrivalActionTradeArrived.VerifyReflection();
            PlanetTileLayerDef.VerifyReflection();

            // Optional-mod integrations — soft dependencies. Forcing each static constructor makes
            // it resolve now and self-report drift (silent unless the mod is present but shifted).
            _ = HARIntegration.Available;
            _ = VEPipesIntegration.Available;
        }
    }
}
