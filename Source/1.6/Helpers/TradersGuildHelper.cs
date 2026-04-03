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
        /// Returns the faction to use for trade permission checks (royal title requirements).
        /// Only resolves the TraderKind's own faction when it has a permitRequiredForTrading,
        /// so that royal title checks look up the correct faction (e.g., Empire for Imperial
        /// traders). For traders without title requirements (pirates, generic, modded), always
        /// returns the settlement faction to avoid false hostility rejections.
        /// </summary>
        public static Faction GetFactionForTradeCheck(Settlement settlement)
        {
            TraderKindDef traderKind = settlement.TraderKind;
            if (traderKind?.permitRequiredForTrading != null && traderKind.faction != null)
            {
                Faction traderFaction = Find.FactionManager.FirstFactionOfDef(traderKind.faction);
                if (traderFaction != null)
                    return traderFaction;
            }
            return settlement.Faction;
        }

        /// <summary>
        /// Finds a valid negotiator pawn in the caravan for trading with the given settlement.
        /// Returns null if no pawn qualifies (e.g., missing required royal title for Imperial traders).
        /// </summary>
        public static Pawn FindNegotiator(Caravan caravan, Settlement settlement)
        {
            if (caravan == null || settlement == null)
                return null;

            return BestCaravanPawnUtility.FindBestNegotiator(
                caravan, GetFactionForTradeCheck(settlement), settlement.TraderKind);
        }

        /// <summary>
        /// Finds a negotiator, jumps the camera, and opens the trade dialog.
        /// Shared by all BTG trade initiation paths (gizmos, float menus, shuttle arrival).
        /// </summary>
        public static void OpenTradeDialog(Caravan caravan, Settlement settlement)
        {
            Pawn negotiator = FindNegotiator(caravan, settlement);
            if (negotiator != null)
            {
                CameraJumper.TryJumpAndSelect(
                    (GlobalTargetInfo)caravan,
                    CameraJumper.MovementMode.Cut);
                Find.WindowStack.Add(new Dialog_Trade(negotiator, settlement, false));
            }
        }

        /// <summary>
        /// Checks whether any pawn in the shuttle pods can negotiate with the settlement.
        /// </summary>
        public static bool HasNegotiatorInPods(IEnumerable<IThingHolder> pods, Settlement settlement)
        {
            return GetTradeBlockedReasonFromPods(pods, settlement) == null;
        }

        /// <summary>
        /// Gets a human-readable reason why trading is blocked, or null if trading is allowed.
        /// Checks each pawn in the caravan against FactionUtility.CanTradeWith to find the rejection reason.
        /// </summary>
        public static string GetTradeBlockedReason(Caravan caravan, Settlement settlement)
        {
            if (caravan == null || settlement == null)
                return null;

            Faction tradeCheckFaction = GetFactionForTradeCheck(settlement);

            // If the trader's faction is hostile (e.g., Empire), block with a clear reason
            if (tradeCheckFaction != settlement.Faction
                && FactionUtility.HostileTo(tradeCheckFaction, Faction.OfPlayer))
                return "BTG_TraderFactionHostile".Translate(tradeCheckFaction.Name);

            // If we can find a negotiator, trade is not blocked
            if (BestCaravanPawnUtility.FindBestNegotiator(
                    caravan, tradeCheckFaction, settlement.TraderKind) != null)
                return null;

            // Check each pawn to find the most informative rejection reason
            string reason = null;
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                if (!pawn.RaceProps.Humanlike)
                    continue;

                AcceptanceReport report = FactionUtility.CanTradeWith(
                    pawn, tradeCheckFaction, settlement.TraderKind);

                if (!report.Accepted && report.Reason != null)
                {
                    reason = report.Reason;
                    if (settlement.TraderKind?.permitRequiredForTrading != null)
                        return reason;
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

            Faction tradeCheckFaction = GetFactionForTradeCheck(settlement);

            if (tradeCheckFaction != settlement.Faction
                && FactionUtility.HostileTo(tradeCheckFaction, Faction.OfPlayer))
                return "BTG_TraderFactionHostile".Translate(tradeCheckFaction.Name);

            string reason = null;
            foreach (IThingHolder pod in pods)
            {
                ThingOwner thingsOwner = pod.GetDirectlyHeldThings();

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
                        pawn, tradeCheckFaction, settlement.TraderKind);

                    if (report.Accepted)
                        return null;

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
            if (faction == null)
                return false;

            // During world generation, the player faction doesn't exist yet
            // (created in ScenPart_PlayerFaction.PostWorldGenerate, after FinalizeInit).
            // Our PlanetTile.LayerDef patch can fire during path cost recalculation
            // before that point, so bail out early.
            if (Faction.OfPlayerSilentFail == null)
                return false;

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
