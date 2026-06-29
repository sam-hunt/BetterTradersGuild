using System.Collections.Generic;
using BetterTradersGuild.LordJobs.Mechs;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Defines mech behavior modes for room assignment.
    public enum MechRoomBehavior
    {
        // Active defense - wanders in room and aggressively attacks nearby hostiles.
        // Uses LordJob_DefendPoint with DutyDefOf.Defend.
        // Suitable for combat mechs (Militor).
        Defend,

        // Passive - wanders in room but does not seek enemies.
        // Uses LordJob_StayInArea with BTG_WanderInArea duty.
        // Self-defense only via ThinkTree fallback if directly threatened.
        // Placeholder for expensive/specialized mechs without narrower behavior
        // added yet, e.g. fabricor
        Passive,

        // Medic - room-bound triage. Tends and rescues wounded same-faction
        // defenders inside its room, and goes dormant (self-charge) when idle.
        // Uses LordJob_MechMedic with the BTG_MechMedic duty. Suitable for the
        // Paramedic mech.
        Medic,

        // Clean - janitor. Cleans filth strictly within the one room it was spawned into
        // (never any other room or outside the walls) and goes dormant (self-charge) near
        // the room centre when none remains. Uses LordJob_MechClean with the BTG_MechClean
        // duty. Suitable for the Cleansweeper mech.
        Clean,

        // Farm - greenhouse tender. Harvests mature food crops within a moderate radius
        // of its anchor point (never outside the settlement structure bounds), hauls the
        // produce to a nearby shelf, sows rice into the emptied basin cells, and goes
        // dormant (self-charge) near the point when there is no work. Uses LordJob_MechFarm
        // with the BTG_MechFarm duty. Suitable for the Agrihand mech.
        Farm
    }

    // Manages Lords for mechs spawned in TradersGuild rooms via RoomPartWorkers.
    //
    // PURPOSE:
    // Keeps mechs in their designated rooms by assigning them to a shared Lord.
    // Without this, NPC mechs fall through to default "wander anywhere" behavior
    // in the Mechanoid ThinkTree.
    //
    // BEHAVIOR MODES:
    // - Defend: Active defense using LordJob_DefendPoint (for Militors)
    // - Passive: Wander only using LordJob_StayInArea (for utility mechs)
    // - Medic: Room-bound triage using LordJob_MechMedic (for Paramedics)
    // - Clean: Room-bound filth cleaning using LordJob_MechClean (for Cleansweepers)
    // - Farm: Radius-bound greenhouse tending using LordJob_MechFarm (for Agrihands)
    //
    // TECHNICAL APPROACH:
    // - Each room gets one shared Lord at the room's center point
    // - Multiple mechs in the same room share the Lord
    // - Mechs with same behavior mode in same room share a Lord
    // - Different behavior modes create separate Lords (rare case)
    //
    // USAGE:
    // Called from RoomPart_MechXxx workers after spawning each mech:
    // // For combat mechs (Militor):
    // RoomMechLordHelper.AddMechToRoomLord(mech, map, room, faction, MechRoomBehavior.Defend);
    //
    // // For utility mechs (Fabricor, Lifter, etc.):
    // RoomMechLordHelper.AddMechToRoomLord(mech, map, room, faction, MechRoomBehavior.Passive);
    public static class RoomMechLordHelper
    {
        // Default wander radius for Defend mode - how far mechs roam from room center.
        public const float DefaultWanderRadius = 7f;

        // Default defend radius - how far Defend mode mechs will chase/engage enemies.
        public const float DefaultDefendRadius = 16f;

        // Tolerance for matching existing Lords to room center points.
        private const float PointMatchTolerance = 3f;

        // Finds or creates a Lord for the room and adds the mech to it.
        // Multiple mechs with the same behavior in the same room share one Lord.
        // mech: The spawned mech pawn to add
        // map: The map
        // room: The LayoutRoom (used to calculate center point)
        // faction: The faction for the Lord (typically TradersGuild)
        // behavior: The behavior mode (Defend or Passive)
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

        // Overload for backward compatibility - defaults to Defend behavior.
        public static void AddMechToRoomLord(
            Pawn mech,
            Map map,
            LayoutRoom room,
            Faction faction)
        {
            AddMechToRoomLord(mech, map, room, faction, MechRoomBehavior.Defend);
        }

        // Finds an existing Lord matching the faction, location, and behavior type.
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
                else if (behavior == MechRoomBehavior.Medic)
                {
                    if (!(lord.LordJob is LordJob_MechMedic))
                        continue;

                    if (lord.CurLordToil is LordToil_MechMedic medicToil)
                    {
                        if (medicToil.Point.DistanceTo(point) <= PointMatchTolerance)
                            return lord;
                    }
                }
                else if (behavior == MechRoomBehavior.Clean)
                {
                    if (!(lord.LordJob is LordJob_MechClean))
                        continue;

                    if (lord.CurLordToil is LordToil_MechClean cleanToil)
                    {
                        if (cleanToil.Point.DistanceTo(point) <= PointMatchTolerance)
                            return lord;
                    }
                }
                else if (behavior == MechRoomBehavior.Farm)
                {
                    if (!(lord.LordJob is LordJob_MechFarm))
                        continue;

                    if (lord.CurLordToil is LordToil_MechFarm farmToil)
                    {
                        if (farmToil.Point.DistanceTo(point) <= PointMatchTolerance)
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

        // Creates a new Lord with the appropriate LordJob for the behavior.
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
            else if (behavior == MechRoomBehavior.Medic)
            {
                lordJob = new LordJob_MechMedic(roomCenter);
            }
            else if (behavior == MechRoomBehavior.Clean)
            {
                lordJob = new LordJob_MechClean(roomCenter);
            }
            else if (behavior == MechRoomBehavior.Farm)
            {
                lordJob = new LordJob_MechFarm(roomCenter);
            }
            else // Passive
            {
                lordJob = new LordJob_StayInArea(roomCenter);
            }

            LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { mech });
        }
    }
}
