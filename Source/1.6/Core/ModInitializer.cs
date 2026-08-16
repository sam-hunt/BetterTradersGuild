using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace BetterTradersGuild
{
    // Main mod class - handles initialization and Harmony patching
    [StaticConstructorOnStartup]
    public static class BetterTradersGuildMod
    {
        // Mod settings instance - accessed statically throughout the mod
        public static BetterTradersGuildSettings Settings
        {
            get
            {
                return LoadedModManager.GetMod<BetterTradersGuildMod_ModClass>().settings;
            }
        }

        // Shared instance: startup PatchAll here, plus runtime Patch/Unpatch by the
        // manually lifecycled survivor-label patch (PawnNameColorUtilityPawnNameColorOf).
        public static readonly Harmony Harmony = new Harmony("shunter.bettertradersguild");

        static BetterTradersGuildMod()
        {
            // Apply Harmony patches
            Harmony.PatchAll();

            // Verify every reflection-based lookup the mod depends on actually resolved, so any
            // base-game or optional-mod API drift surfaces here at startup rather than as a silent
            // feature failure later. (Pattern ported from UniqueWeaponsUnbound.)
            ReflectionVerification.VerifyAll();

            // Apply def modifications
            ApplyLifeSupportUnitPowerSetting();
            ApplySmugglersDenQuestWeightSetting();
        }

        // Pushes the configured selection weight onto the smuggler's den QuestScriptDef.
        // NaturalRandomQuestChooser reads rootSelectionWeight off the def on every roll,
        // so there is nothing to invalidate — the next storyteller quest roll sees the
        // new value. A weight of 0 also flips QuestScriptDef.IsRootRandomSelected false,
        // dropping the quest out of the random pool entirely rather than just making it
        // rare. Re-applied from WriteSettings so the change lands on settings close.
        public static void ApplySmugglersDenQuestWeightSetting()
        {
            var questDef = DefRefs.QuestScripts.BTG_SmugglersDen;
            if (questDef == null) return;

            questDef.rootSelectionWeight = Settings.smugglersDenQuestWeight;
        }

        // Applies the configured power output to LifeSupportUnit ThingDef
        // Vanilla LifeSupportUnits output 3200W, but are isolated in small rooms.
        // BTG connects settlement buildings in a map-wide power grid, so this
        // setting allows players to balance or restore vanilla behavior.
        // Negative basePowerConsumption = power production in RimWorld.
        // Uses reflection since the NuGet reference package doesn't expose the field.
        public static void ApplyLifeSupportUnitPowerSetting()
        {
            var lifeSupportDef = DefDatabase<ThingDef>.GetNamedSilentFail("LifeSupportUnit");
            if (lifeSupportDef == null) return;

            var powerComp = lifeSupportDef.GetCompProperties<CompProperties_Power>();
            if (powerComp == null) return;

            // Reflect basePowerConsumption — it is a *private* field on CompProperties_Power
            // (only the read-only PowerConsumption property is public), so NonPublic is
            // required. Without it GetField returns null and the setting silently no-ops.
            var field = typeof(CompProperties_Power).GetField("basePowerConsumption",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                Log.Error("[Better Traders Guild] CompProperties_Power.basePowerConsumption field not found via reflection; "
                    + "the LifeSupportUnit power output setting will not apply. RimWorld API may have changed.");
                return;
            }

            // Negative value = power production
            field.SetValue(powerComp, (float)(-Settings.lifeSupportUnitPowerOutput));

            // Force already-spawned units to recompute their CURRENT output. CompPowerPlant
            // only recomputes in CompTick, but LifeSupportUnit is a Rare ticker so CompTick
            // never fires — the output is set once in PostSpawnSetup and otherwise frozen.
            // Without this, a live setting change would only move the max-power stat (read
            // from the def) while the actual output stayed at the spawn-time value.
            RefreshSpawnedLifeSupportUnits(lifeSupportDef);
        }

        // Recomputes the live power output of every spawned LifeSupportUnit across all
        // loaded maps. No-op outside of an active game (e.g. at startup or from the main
        // menu), where there are no maps to refresh.
        private static void RefreshSpawnedLifeSupportUnits(ThingDef lifeSupportDef)
        {
            if (Current.ProgramState != ProgramState.Playing) return;

            var maps = Find.Maps;
            if (maps == null) return;

            for (int i = 0; i < maps.Count; i++)
            {
                var things = maps[i].listerThings.ThingsOfDef(lifeSupportDef);
                for (int j = 0; j < things.Count; j++)
                    things[j].TryGetComp<CompPowerPlant>()?.UpdateDesiredPowerOutput();
            }
        }
    }
}
