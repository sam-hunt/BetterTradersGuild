using HarmonyLib;
using System.Linq;
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
            var harmony = new Harmony("sam.bettertradersguild");
            harmony.PatchAll();

            // Log initialization with patch count
            var patchCount = harmony.GetPatchedMethods().Count();
            Log.Message($"[Better Traders Guild] Mod initialized with {patchCount} Harmony patches applied.");
        }
    }
}
