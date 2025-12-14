using System.Collections.Generic;
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

                foreach (ThingDef thingDef in thingDefs)
                {
                    if (thingDef == null)
                        continue;

                    // Skip if there's already one of this type here
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
        /// <param name="defName">The defName to search for</param>
        /// <returns>List of matching buildings</returns>
        public static List<Building> FindBuildingsInRoom(Map map, CellRect roomRect, string defName)
        {
            List<Building> buildings = new List<Building>();

            foreach (IntVec3 cell in roomRect)
            {
                List<Thing> things = cell.GetThingList(map);
                foreach (Thing thing in things)
                {
                    if (thing is Building building && thing.def.defName == defName)
                    {
                        buildings.Add(building);
                    }
                }
            }

            return buildings;
        }
    }
}
