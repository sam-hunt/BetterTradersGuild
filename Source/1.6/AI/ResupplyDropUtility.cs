using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.AI
{
    // The mechanics behind the defender comms-console food resupply (§6.5): a custom
    // drop-cell finder plus the cargo-pod spawn and per-defender drop sizing.
    //
    // Why a custom finder rather than vanilla DropCellFinder: on our orbital maps the
    // open-air bays are vacuum-exposed, and vanilla's CanPhysicallyDropInto rejects
    // vacuum cells outright - so it would refuse to land a pod in exactly the rooms we
    // most want (the open-sky pod/shuttle bays). This finder is scoped to a priority
    // list of in-structure rooms and only enforces what a skyfaller actually needs
    // (a standable cell, no thick roof, nothing blocking skyfallers), so the pod stays
    // inside the structure - dropping outside would defeat the bounded-defender design.
    //
    // Room priority, each with a per-room roof-punch policy (we walk them in order and
    // take the first room that yields a landable cell):
    //   1. BTG_PodLaunchBay   - open-sky only: open-air room, pod lands through open sky.
    //   2. BTG_ShuttleBay     - open-sky only: the room's rects cover roofed cells too, so
    //                           "open-sky only" is what confines the drop to the unroofed
    //                           landing-pad subroom. We never punch the bay's roof - if the
    //                           pad is blocked we fall through to the interior rooms below.
    //   3. BTG_MessHall       - roof-punch allowed: fully roofed, so the incoming pod
    //                           punches the thin roof on landing.
    //   4. BTG_ControlCenter  - roof-punch allowed (roofed fallback).
    // The bays therefore never take roof damage; roof-punching a pressurized interior is an
    // accepted cost only reached when no bay pad is available - floor-spawning without a pod
    // reads as cheating, and dropping outside the structure would break containment.
    internal static class ResupplyDropUtility
    {
        // Finds a pod-landing cell in the highest-priority available room. The two bays are
        // open-sky-only (their unroofed cells, no roof damage; for the ShuttleBay this confines
        // the drop to its landing-pad subroom); the interior rooms also accept a thin-roofed
        // cell (the pod punches the roof). Returns false only if no room yields a landable cell.
        // Room defs are referenced via the LayoutRooms DefOf at call time (always after DefOf
        // binding), and each TryRoom is null-safe if a def somehow failed to load.
        public static bool TryFindDropCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
                return false;

            return TryRoom(map, LayoutRooms.BTG_PodLaunchBay, allowRoofPunch: false, out cell)
                || TryRoom(map, LayoutRooms.BTG_ShuttleBay, allowRoofPunch: false, out cell)
                || TryRoom(map, LayoutRooms.BTG_MessHall, allowRoofPunch: true, out cell)
                || TryRoom(map, LayoutRooms.BTG_ControlCenter, allowRoofPunch: true, out cell);
        }

        private static bool TryRoom(Map map, LayoutRoomDef roomDef, bool allowRoofPunch, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (roomDef == null)
                return false;

            bool requireOpenSky = !allowRoofPunch;
            foreach (LayoutRoom room in StructureRoomLocator.RoomsOfDef(map, roomDef))
            {
                if (TryFindCellInRoom(room, map, requireOpenSky, out cell))
                    return true;
            }
            return false;
        }

        // Re-checks a single cell at drop time (it may have been blocked since scouting).
        public static bool IsCellStillLandable(IntVec3 c, Map map)
        {
            return c.IsValid && IsValidPodCell(c, map, requireOpenSky: false);
        }

        // Spawns a survival-meal cargo pod at cell. The incoming
        // skyfaller punches a thin roof on landing if the cell is roofed.
        public static void SpawnResupplyDrop(Map map, IntVec3 cell, int mealCount, Faction faction)
        {
            if (map == null || !cell.IsValid || mealCount <= 0)
                return;

            var info = new ActiveTransporterInfo();
            int remaining = mealCount;
            int stackLimit = Mathf.Max(1, ThingDefOf.MealSurvivalPack.stackLimit);
            while (remaining > 0)
            {
                int n = Mathf.Min(remaining, stackLimit);
                Thing meals = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
                meals.stackCount = n;
                info.innerContainer.TryAdd(meals);
                remaining -= n;
            }

            // faction's pod defs are used if it has any, else MakeDropPodAt falls back to
            // the vanilla drop pod - safe even when TradersGuild defines no custom pod.
            DropPodUtility.MakeDropPodAt(cell, map, info, faction);
        }

        // Drop size = ModSettings.resupplyMealsPerDefender x living humanlike defenders on
        // the pawn's lord. Mechs aren't counted (they don't eat), and dead/removed pawns
        // have already left the lord, so the count shrinks as the garrison is neutralized.
        // Returns 0 when the behavior is disabled or the pawn has no lord.
        public static int MealCountForDefenders(Pawn pawn)
        {
            int perDefender = BetterTradersGuildMod.Settings.resupplyMealsPerDefender;
            if (perDefender <= 0)
                return 0;

            Lord lord = pawn.GetLord();
            if (lord == null)
                return 0;

            int defenders = 0;
            List<Pawn> owned = lord.ownedPawns;
            for (int i = 0; i < owned.Count; i++)
            {
                Pawn p = owned[i];
                if (p != null && !p.Dead && p.RaceProps != null && p.RaceProps.Humanlike)
                    defenders++;
            }
            return perDefender * defenders;
        }

        private static bool TryFindCellInRoom(LayoutRoom room, Map map, bool requireOpenSky, out IntVec3 cell)
        {
            // padding 1 => a clear 3x3 so the pod and its ejected meals fit; fall back to a
            // single clear cell for cramped rooms.
            return room.TryGetRandomCellInRoom(map, out cell, contractedBy: 0, padding: 1, validator: c => IsValidPodCell(c, map, requireOpenSky))
                || room.TryGetRandomCellInRoom(map, out cell, contractedBy: 0, padding: 0, validator: c => IsValidPodCell(c, map, requireOpenSky));
        }

        private static bool IsValidPodCell(IntVec3 c, Map map, bool requireOpenSky)
        {
            if (!c.InBounds(map) || !c.Standable(map))
                return false;

            RoofDef roof = map.roofGrid.RoofAt(c);
            if (roof != null)
            {
                // Open-sky pass rejects any roof; otherwise only thick roof is a hard no
                // (a skyfaller can punch a thin/constructed roof but not overhead mountain).
                if (requireOpenSky || roof.isThickRoof)
                    return false;
            }

            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def.preventSkyfallersLandingOn || things[i] is Skyfaller)
                    return false;
            }
            return true;
        }
    }
}
