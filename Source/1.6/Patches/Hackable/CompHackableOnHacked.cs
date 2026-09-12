using BetterTradersGuild.MapGeneration;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.Hackable
{
    // Harmony patch: CompHackable.OnHacked method
    // Applies a severe goodwill penalty when a player hacks the cargo vault hatch
    // on a TradersGuild settlement while not already hostile.
    // This handles the edge case where a player:
    // 1. Raids a TradersGuild settlement (becoming hostile)
    // 2. Restores goodwill through gifts or other means while still on the map
    // 3. Attempts to hack the cargo vault
    //
    // Hacking the vault is a hostile action that should always result in hostility,
    // regardless of the player's current standing with the faction. The shuttle
    // equivalent (CompPromoteOnHack) applies the same penalty through HostileActHelper.
    [HarmonyPatch(typeof(CompHackable), "OnHacked")]
    public static class CompHackableOnHacked
    {
        [HarmonyPostfix]
        public static void Postfix(CompHackable __instance)
        {
            // Only process our cargo vault hatch
            if (__instance.parent is not CargoVaultHatch hatch)
                return;

            HostileActHelper.PunishHostileActOnTradersGuildMap(hatch.Map);
        }
    }
}
