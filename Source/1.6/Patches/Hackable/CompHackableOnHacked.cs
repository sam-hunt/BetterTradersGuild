using BetterTradersGuild.MapGeneration;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
    // regardless of the player's current standing with the faction.
    [HarmonyPatch(typeof(CompHackable), "OnHacked")]
    public static class CompHackableOnHacked
    {
        // Postfix: After hacking completes, check if this is a TradersGuild cargo vault
        // and apply goodwill penalty if the player is not already hostile.
        [HarmonyPostfix]
        public static void Postfix(CompHackable __instance)
        {
            // Only process our cargo vault hatch
            if (__instance.parent is not CargoVaultHatch hatch)
                return;

            // Get the settlement from the map
            // Note: After defeat, maps are reparented to DestroyedSettlement, so this cast
            // returns null - correctly skipping goodwill penalty for defeated settlements
            Settlement settlement = hatch.Map?.Parent as Settlement;
            if (settlement?.Faction == null)
                return;

            // Only apply to TradersGuild settlements
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                return;

            Faction faction = settlement.Faction;

            Faction player = Faction.OfPlayerSilentFail;
            if (player == null)
                return;

            // Ensure a relation entry exists before reading/affecting goodwill. No-op when already
            // related, but if the relation matrix is incomplete this avoids the vanilla "null
            // relation" error and ensures the penalty below actually applies (TryAffectGoodwillWith
            // would otherwise mutate a throwaway dummy and fail to turn the faction hostile).
            faction.TryMakeInitialRelationsWith(player);

            // If already hostile, no penalty needed
            if (faction.PlayerRelationKind == FactionRelationKind.Hostile)
                return;

            // Apply severe goodwill penalty to ensure hostility
            // -200 guarantees hostility regardless of current goodwill level
            faction.TryAffectGoodwillWith(
                player,
                -200,
                canSendMessage: true,
                canSendHostilityLetter: true,
                HistoryEventDefOf.AttackedSettlement
            );
        }
    }
}
