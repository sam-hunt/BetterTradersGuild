using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild
{
    // Turns the Traders Guild hostile after a player act that vanilla does not treat as an
    // attack: hacking the cargo vault hatch, stealing a parked shuttle. Shared by the
    // CompHackable.OnHacked postfix (hatch) and CompPromoteOnHack (shuttle).
    public static class HostileActHelper
    {
        // Applies the penalty when the map is a still-standing TradersGuild settlement whose
        // faction is not yet hostile. After defeat the map is reparented to a
        // DestroyedSettlement, so the cast fails and a restored-goodwill player is not
        // punished for looting a station they already took.
        public static void PunishHostileActOnTradersGuildMap(Map map)
        {
            if (map?.Parent is not Settlement settlement || settlement.Faction == null) return;
            if (!TradersGuildHelper.IsTradersGuildSettlement(settlement)) return;

            Faction faction = settlement.Faction;
            Faction player = Faction.OfPlayerSilentFail;
            if (player == null) return;

            // Ensure a relation entry exists before reading/affecting goodwill. No-op when already
            // related, but if the relation matrix is incomplete this avoids the vanilla "null
            // relation" error and ensures the penalty below actually applies (TryAffectGoodwillWith
            // would otherwise mutate a throwaway dummy and fail to turn the faction hostile).
            faction.TryMakeInitialRelationsWith(player);

            if (faction.PlayerRelationKind == FactionRelationKind.Hostile) return;

            // -200 guarantees hostility regardless of current goodwill level.
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
