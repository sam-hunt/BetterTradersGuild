using BetterTradersGuild.DefRefs;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild
{
    /// <summary>
    /// Helper class for checking Traders Guild faction status and relations
    /// </summary>
    public static class TradersGuildHelper
    {
        /// <summary>
        /// Finds a valid negotiator pawn in the caravan for trading with the given settlement.
        /// Returns null if no pawn qualifies (e.g., missing required royal title for Imperial traders).
        /// </summary>
        public static Pawn FindNegotiator(Caravan caravan, Settlement settlement)
        {
            if (caravan == null || settlement == null)
                return null;

            return BestCaravanPawnUtility.FindBestNegotiator(
                caravan, settlement.Faction, settlement.TraderKind);
        }

        /// <summary>
        /// Gets a human-readable reason why trading is blocked, or null if trading is allowed.
        /// Checks each pawn in the caravan against FactionUtility.CanTradeWith to find the rejection reason.
        /// </summary>
        public static string GetTradeBlockedReason(Caravan caravan, Settlement settlement)
        {
            if (caravan == null || settlement == null)
                return null;

            // If we can find a negotiator, trade is not blocked
            if (FindNegotiator(caravan, settlement) != null)
                return null;

            // Check each pawn to find the most informative rejection reason
            // Prefer title-related rejections over generic ones
            string reason = null;
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                if (!pawn.RaceProps.Humanlike)
                    continue;

                AcceptanceReport report = FactionUtility.CanTradeWith(
                    pawn, settlement.Faction, settlement.TraderKind);

                if (!report.Accepted && report.Reason != null)
                {
                    reason = report.Reason;
                    // Title-related reasons are the most specific, keep looking
                    // only if we haven't found one yet
                    if (settlement.TraderKind?.permitRequiredForTrading != null)
                        return reason; // This is the title reason, return immediately
                }
            }

            return reason ?? "BTG_NoNegotiator".Translate();
        }

        /// <summary>
        /// Gets a human-readable reason why trading is blocked for shuttle pods, or null if trading is allowed.
        /// Extracts pawns from shuttle pods and checks each against FactionUtility.CanTradeWith.
        /// </summary>
        public static string GetTradeBlockedReasonFromPods(IEnumerable<IThingHolder> pods, Settlement settlement)
        {
            if (pods == null || settlement == null)
                return null;

            string reason = null;
            foreach (IThingHolder pod in pods)
            {
                ThingOwner thingsOwner = pod.GetDirectlyHeldThings();

                // For caravan shuttles, get items from the caravan instead of the pod
                CompTransporter compTransporter = pod as CompTransporter;
                if (compTransporter != null && CaravanShuttleUtility.IsCaravanShuttle(compTransporter))
                {
                    Caravan caravan = CaravanUtility.GetCaravan(compTransporter.parent);
                    if (caravan != null)
                        thingsOwner = caravan.GetDirectlyHeldThings();
                }

                foreach (Thing thing in thingsOwner)
                {
                    Pawn pawn = thing as Pawn;
                    if (pawn == null || !pawn.RaceProps.Humanlike)
                        continue;

                    AcceptanceReport report = FactionUtility.CanTradeWith(
                        pawn, settlement.Faction, settlement.TraderKind);

                    if (report.Accepted)
                        return null; // Found a valid negotiator

                    if (!report.Accepted && report.Reason != null)
                        reason = report.Reason;
                }
            }

            return reason ?? "BTG_NoNegotiator".Translate();
        }

        /// <summary>
        /// Checks if a settlement belongs to the Traders Guild faction.
        /// </summary>
        public static bool IsTradersGuildSettlement(Settlement settlement)
        {
            // Null check - make sure the settlement and its faction exist
            if (settlement == null || settlement.Faction == null)
                return false;

            // Check if the faction's def matches the Traders Guild
            return settlement.Faction.def == Factions.TradersGuild;
        }

        /// <summary>
        /// Checks if the player can peacefully visit a faction's settlement
        /// Requires non-hostile relations (neutral or better)
        /// </summary>
        public static bool CanPeacefullyVisit(Faction faction)
        {
            // Null check
            if (faction == null)
                return false;

            // Check if the faction is hostile to the player
            // PlayerRelationKind.Hostile means they're enemies
            // We want Neutral or Ally
            return faction.PlayerRelationKind != FactionRelationKind.Hostile;
        }

        /// <summary>
        /// Checks if a map belongs to a TradersGuild settlement.
        /// Used by patches that need to determine context during map events.
        /// </summary>
        public static bool IsMapInTradersGuildSettlement(Verse.Map map)
        {
            if (map == null)
                return false;

            Settlement settlement = map.Parent as Settlement;
            return IsTradersGuildSettlement(settlement);
        }
    }
}
