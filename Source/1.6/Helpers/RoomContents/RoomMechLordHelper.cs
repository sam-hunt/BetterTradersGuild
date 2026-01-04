using System.Collections.Generic;
using BetterTradersGuild.LordJobs;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Defines mech behavior modes for room assignment.
    /// </summary>
    public enum MechRoomBehavior
    {
        /// <summary>
        /// Active defense - wanders in room and aggressively attacks nearby hostiles.
        /// Uses LordJob_DefendPoint with DutyDefOf.Defend.
        /// Suitable for combat mechs (Militor).
        /// </summary>
        Defend,

        /// <summary>
        /// Passive - wanders in room but does not seek enemies.
        /// Uses LordJob_StayInArea with BTG_WanderInArea duty.
        /// Self-defense only via ThinkTree fallback if directly threatened.
        /// Suitable for expensive/specialized mechs (Fabricor, Paramedic, Cleansweeper, Agrihand).
        /// </summary>
        Passive
    }

    /// <summary>
    /// Manages Lords for mechs spawned in TradersGuild rooms via RoomPartWorkers.
    ///
    /// PURPOSE:
    /// Keeps mechs in their designated rooms by assigning them to a shared Lord.
    /// Without this, NPC mechs fall through to default "wander anywhere" behavior
    /// in the Mechanoid ThinkTree.
    ///
    /// BEHAVIOR MODES:
    /// - Defend: Active defense using LordJob_DefendPoint (for Militors)
    /// - Passive: Wander only using LordJob_StayInArea (for utility mechs)
    ///
    /// TECHNICAL APPROACH:
    /// - Each room gets one shared Lord at the room's center point
    /// - Multiple mechs in the same room share the Lord
    /// - Mechs with same behavior mode in same room share a Lord
    /// - Different behavior modes create separate Lords (rare case)
    ///
    /// USAGE:
    /// Called from RoomPart_MechXxx workers after spawning each mech:
    /// <![CDATA[
    /// // For combat mechs (Militor):
    /// RoomMechLordHelper.AddMechToRoomLord(mech, map, room, faction, MechRoomBehavior.Defend);
    ///
    /// // For utility mechs (Cleansweeper, Fabricor, etc.):
    /// RoomMechLordHelper.AddMechToRoomLord(mech, map, room, faction, MechRoomBehavior.Passive);
    /// ]]>
    /// </summary>
    public static class RoomMechLordHelper
    {
        /// <summary>
        /// Default wander radius for Defend mode - how far mechs roam from room center.
        /// </summary>
        public const float DefaultWanderRadius = 7f;

        /// <summary>
        /// Default defend radius - how far Defend mode mechs will chase/engage enemies.
        /// </summary>
        public const float DefaultDefendRadius = 16f;

        /// <summary>
        /// Tolerance for matching existing Lords to room center points.
        /// </summary>
        private const float PointMatchTolerance = 3f;

        /// <summary>
        /// Finds or creates a Lord for the room and adds the mech to it.
        /// Multiple mechs with the same behavior in the same room share one Lord.
        /// </summary>
        /// <param name="mech">The spawned mech pawn to add</param>
        /// <param name="map">The map</param>
        /// <param name="room">The LayoutRoom (used to calculate center point)</param>
        /// <param name="faction">The faction for the Lord (typically TradersGuild)</param>
        /// <param name="behavior">The behavior mode (Defend or Passive)</param>
        public static void AddMechToRoomLord(
            Pawn mech,
            Map map,
            LayoutRoom room,
            Faction faction,
            MechRoomBehavior behavior)
        {
            if (mech == null || map == null || room == null || faction == null)
                return;

            // Calculate room center from the first rect
            IntVec3 roomCenter = room.rects.Count > 0
                ? room.rects[0].CenterCell
                : mech.Position;

            // Try to find existing Lord for this room and behavior
            Lord existingLord = FindMatchingLord(map, faction, roomCenter, behavior);

            if (existingLord != null)
            {
                existingLord.AddPawn(mech);
            }
            else
            {
                CreateNewLord(mech, map, faction, roomCenter, behavior);
            }
        }

        /// <summary>
        /// Overload for backward compatibility - defaults to Defend behavior.
        /// </summary>
        public static void AddMechToRoomLord(
            Pawn mech,
            Map map,
            LayoutRoom room,
            Faction faction)
        {
            AddMechToRoomLord(mech, map, room, faction, MechRoomBehavior.Defend);
        }

        /// <summary>
        /// Finds an existing Lord matching the faction, location, and behavior type.
        /// </summary>
        private static Lord FindMatchingLord(Map map, Faction faction, IntVec3 point, MechRoomBehavior behavior)
        {
            foreach (Lord lord in map.lordManager.lords)
            {
                if (lord.faction != faction)
                    continue;

                // Check for matching LordJob type based on behavior
                if (behavior == MechRoomBehavior.Defend)
                {
                    if (!(lord.LordJob is LordJob_DefendPoint))
                        continue;

                    if (lord.CurLordToil is LordToil_DefendPoint defendToil)
                    {
                        if (defendToil.FlagLoc.DistanceTo(point) <= PointMatchTolerance)
                            return lord;
                    }
                }
                else // Passive
                {
                    if (!(lord.LordJob is LordJob_StayInArea))
                        continue;

                    if (lord.CurLordToil is LordToil_WanderInArea wanderToil)
                    {
                        if (wanderToil.Point.DistanceTo(point) <= PointMatchTolerance)
                            return lord;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new Lord with the appropriate LordJob for the behavior.
        /// </summary>
        private static void CreateNewLord(Pawn mech, Map map, Faction faction, IntVec3 roomCenter, MechRoomBehavior behavior)
        {
            LordJob lordJob;

            if (behavior == MechRoomBehavior.Defend)
            {
                lordJob = new LordJob_DefendPoint(
                    roomCenter,
                    wanderRadius: DefaultWanderRadius,
                    defendRadius: DefaultDefendRadius,
                    isCaravanSendable: false,
                    addFleeToil: false
                );
            }
            else // Passive
            {
                lordJob = new LordJob_StayInArea(roomCenter);
            }

            LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { mech });
        }
    }
}
