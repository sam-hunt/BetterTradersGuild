using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Main mod class - handles initialization and Harmony patching
    /// </summary>
    [StaticConstructorOnStartup]
    public static class BetterTradersGuildMod
    {
        /// <summary>
        /// Mod settings instance - accessed statically throughout the mod
        /// </summary>
        public static BetterTradersGuildSettings Settings
        {
            get
            {
                return LoadedModManager.GetMod<BetterTradersGuildMod_ModClass>().settings;
            }
        }

        static BetterTradersGuildMod()
        {
            // Apply Harmony patches
            var harmony = new Harmony("samhunt.bettertradersguild");
            harmony.PatchAll();

            // Apply def modifications
            ApplyLifeSupportUnitPowerSetting();
        }

        /// <summary>
        /// Applies the configured power output to LifeSupportUnit ThingDef
        /// </summary>
        /// <remarks>
        /// Vanilla LifeSupportUnits output 3200W, but are isolated in small rooms.
        /// BTG connects settlement buildings in a map-wide power grid, so this
        /// setting allows players to balance or restore vanilla behavior.
        /// Negative basePowerConsumption = power production in RimWorld.
        /// Uses reflection since the NuGet reference package doesn't expose the field.
        /// </remarks>
        public static void ApplyLifeSupportUnitPowerSetting()
        {
            var lifeSupportDef = DefDatabase<ThingDef>.GetNamedSilentFail("LifeSupportUnit");
            if (lifeSupportDef == null) return;

            var powerComp = lifeSupportDef.GetCompProperties<CompProperties_Power>();
            if (powerComp == null) return;

            // Use reflection to access basePowerConsumption field
            var field = typeof(CompProperties_Power).GetField("basePowerConsumption",
                BindingFlags.Public | BindingFlags.Instance);
            // Negative value = power production
            if (field != null)
                field.SetValue(powerComp, (float)(-Settings.lifeSupportUnitPowerOutput));
        }
    }
}
