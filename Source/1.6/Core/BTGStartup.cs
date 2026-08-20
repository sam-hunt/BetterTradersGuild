using BetterTradersGuild.AI.Mechs;
using BetterTradersGuild.Helpers.MapGeneration;
using BetterTradersGuild.Helpers.RoomContents;
using BetterTradersGuild.Patches.SettlementPatches;
using BetterTradersGuild.RoomContents.Armory;
using BetterTradersGuild.RoomContents.CrewQuarters;

namespace BetterTradersGuild
{
    // Startup work that must run against the CURRENT DefDatabase: reflection/integration
    // verification, the settings-driven def-field writes, and dropping every cached def
    // instance. Runs once per play-data LOAD, not once per process — deliberately NOT
    // [StaticConstructorOnStartup], whose once-per-process contract is too weak for
    // def-derived work: an in-process reload (a mid-session language change) replaces every
    // def instance and a type initializer never re-runs. Invoked instead by
    // Patches/StaticConstructorOnStartupUtility/StaticConstructorOnStartupUtilityCallAll.cs
    // at exactly the moment static ctors run — after defs, DefOf rebinding and full language
    // injection — on every load; that file carries the verified load ordering, the DoPlayLoad
    // trap, and the hot-reload caveat.
    //
    // Everything called here must stay idempotent (reloads and re-patching make it fire more
    // than once per process).
    public static class BTGStartup
    {
        public static void Run()
        {
            // Reflection self-checks, plus per-load re-resolution of UMWIntegration's def
            // references (the other integrations cache only types/members, which survive a
            // play-data reload).
            ReflectionVerification.VerifyAll();

            // Settings-driven def-field writes: the fresh def instances carry shipped XML
            // values until these overwrite them again.
            BetterTradersGuildMod.ApplyLifeSupportUnitPowerSetting();
            BetterTradersGuildMod.ApplyQuestWeightSettings();

            // Drop every lazily built cache of def instances so the next use rebuilds from
            // the current DefDatabase instead of serving dead defs from the previous load.
            WasteFillerPrefabSelector.InvalidateCache();
            SubroomCarpetCustomizer.InvalidateCache();
            ArmoryShelfFiller.InvalidateCache();
            UniqueWeaponPoolHelper.InvalidateCache();
            BookGenerationHelper.InvalidateCache();
            HiddenPipeHelper.InvalidateCache();
            RoomPetHelper.InvalidateCache();
            JobGiver_BTGAgrihandPlantPots.InvalidateCache();
            SettlementTraderTrackerGetTraderKind.ClearLocalCache();
        }
    }
}
