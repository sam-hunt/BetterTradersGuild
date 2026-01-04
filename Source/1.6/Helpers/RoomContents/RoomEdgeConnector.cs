using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Connects buildings to room edge infrastructure (conduits, pipes, etc.).
    ///
    /// PURPOSE:
    /// Prefabs placed in room interiors (like sun lamps, pod launchers) need to connect
    /// to the infrastructure network that runs under room walls. This helper places
    /// a line of hidden conduits/pipes from an interior position to the nearest room edge.
    ///
    /// DESIGN NOTE:
    /// The edge cell itself is excluded because LayoutConduitPlacer already places
    /// infrastructure under walls during initial layout generation.
    /// </summary>
    public static class RoomEdgeConnector
    {
        /// <summary>
        /// Places a line of things from startPos toward the nearest room edge.
        /// Stops one cell before the edge (edge already has conduits under walls).
        /// </summary>
        /// <param name="map">The map</param>
        /// <param name="startPos">Position to start from (e.g., a sun lamp)</param>
        /// <param name="roomRect">The room's bounding rect</param>
        /// <param name="thingDefs">ThingDefs to place at each position (e.g., HiddenConduit, pipes)</param>
        /// <returns>Number of positions where things were placed</returns>
        public static int ConnectToNearestEdge(Map map, IntVec3 startPos, CellRect roomRect, IEnumerable<ThingDef> thingDefs)
        {
            // Calculate distance to each edge
            int distToLeft = startPos.x - roomRect.minX;
            int distToRight = roomRect.maxX - startPos.x;
            int distToBottom = startPos.z - roomRect.minZ;
            int distToTop = roomRect.maxZ - startPos.z;

            // Find minimum distance and corresponding direction
            int minDist = distToLeft;
            IntVec3 direction = IntVec3.West;

            if (distToRight < minDist)
            {
                minDist = distToRight;
                direction = IntVec3.East;
            }
            if (distToBottom < minDist)
            {
                minDist = distToBottom;
                direction = IntVec3.South;
            }
            if (distToTop < minDist)
            {
                minDist = distToTop;
                direction = IntVec3.North;
            }

            int placedCount = 0;

            // Place things from start position toward edge (excluding edge cell itself)
            // minDist is distance to edge, so we iterate i < minDist to stop before edge
            for (int i = 0; i < minDist; i++)
            {
                IntVec3 pos = startPos + direction * i;

                if (!pos.InBounds(map))
                    continue;

                List<Thing> existingThings = pos.GetThingList(map);

                // Check if any existing thing already transmits power
                bool hasPowerTransmitter = false;
                foreach (Thing thing in existingThings)
                {
                    CompPower compPower = thing.TryGetComp<CompPower>();
                    if (compPower != null && compPower.Props.transmitsPower)
                    {
                        hasPowerTransmitter = true;
                        break;
                    }
                }

                foreach (ThingDef thingDef in thingDefs)
                {
                    if (thingDef == null)
                        continue;

                    // Check if this def transmits power
                    bool defTransmitsPower = thingDef.GetCompProperties<CompProperties_Power>()?.transmitsPower ?? false;

                    // Skip power transmitters if cell already has power transmission
                    if (defTransmitsPower && hasPowerTransmitter)
                        continue;

                    // Skip if there's already one of this exact type here (for non-power things like pipes)
                    bool alreadyExists = false;
                    foreach (Thing thing in existingThings)
                    {
                        if (thing.def == thingDef)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        Thing newThing = ThingMaker.MakeThing(thingDef);
                        GenSpawn.Spawn(newThing, pos, map);
                        placedCount++;
                    }
                }
            }

            return placedCount;
        }

        /// <summary>
        /// Convenience overload for connecting with a single ThingDef.
        /// </summary>
        public static int ConnectToNearestEdge(Map map, IntVec3 startPos, CellRect roomRect, ThingDef thingDef)
        {
            return ConnectToNearestEdge(map, startPos, roomRect, new[] { thingDef });
        }

        /// <summary>
        /// Finds all buildings of a specific type within a room rect.
        /// </summary>
        /// <param name="map">The map</param>
        /// <param name="roomRect">The room's bounding rect</param>
        /// <param name="buildingDef">The ThingDef to search for (use DefRefs.Things.*)</param>
        /// <returns>List of matching buildings, or empty list if buildingDef is null</returns>
        public static List<Building> FindBuildingsInRoom(Map map, CellRect roomRect, ThingDef buildingDef)
        {
            List<Building> buildings = new List<Building>();

            // Silent abort if def not available (e.g., mod not loaded)
            if (buildingDef == null)
                return buildings;

            foreach (IntVec3 cell in roomRect)
            {
                List<Thing> things = cell.GetThingList(map);
                foreach (Thing thing in things)
                {
                    if (thing is Building building && thing.def == buildingDef)
                    {
                        buildings.Add(building);
                    }
                }
            }

            return buildings;
        }

        /// <summary>
        /// Connects all buildings of a specific type to the room edge using hidden conduits.
        /// Convenience method for the common pattern of connecting power-hungry interior
        /// buildings (sun lamps, vitals monitors, etc.) to the wall conduit network.
        /// </summary>
        /// <param name="map">The map</param>
        /// <param name="roomRect">The room's bounding rect</param>
        /// <param name="buildingDef">The ThingDef of buildings to connect (use DefRefs.Things.*)</param>
        /// <returns>Number of conduit segments placed, or 0 if buildingDef is null</returns>
        public static int ConnectBuildingsToConduitNetwork(Map map, CellRect roomRect, ThingDef buildingDef)
        {
            return ConnectBuildingsToInfrastructure(map, roomRect, buildingDef, Things.HiddenConduit);
        }

        /// <summary>
        /// Connects all buildings of a specific type to the room edge using a specified
        /// infrastructure type (conduits, pipes, etc.).
        /// </summary>
        /// <param name="map">The map</param>
        /// <param name="roomRect">The room's bounding rect</param>
        /// <param name="buildingDef">The ThingDef of buildings to connect (use DefRefs.Things.*)</param>
        /// <param name="infrastructureDef">The ThingDef of infrastructure to place (use DefRefs.Things.*)</param>
        /// <returns>Number of infrastructure segments placed, or 0 if either def is null</returns>
        public static int ConnectBuildingsToInfrastructure(Map map, CellRect roomRect, ThingDef buildingDef, ThingDef infrastructureDef)
        {
            // Silent abort if either def not available (e.g., mod not loaded)
            if (buildingDef == null || infrastructureDef == null)
                return 0;

            List<Building> buildings = FindBuildingsInRoom(map, roomRect, buildingDef);
            int totalPlaced = 0;

            foreach (Building building in buildings)
            {
                totalPlaced += ConnectToNearestEdge(map, building.Position, roomRect, infrastructureDef);
            }

            return totalPlaced;
        }
    }
}
