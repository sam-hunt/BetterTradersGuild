using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Connects "floating island" subrooms to the power grid by placing HiddenConduit
    /// along horizontal rows where interior subroom doors exist.
    ///
    /// Subrooms in middle strips with exclusion zones at both ends (due to side doors)
    /// may be disconnected from the wall-based power grid. Since subroom prefabs
    /// have doors (AncientBlastDoor) at their entrance, this helper finds these door
    /// rows and runs conduits along them to ensure power connectivity.
    /// </summary>
    internal static class CorridorPowerConnector
    {
        /// <summary>
        /// Places HiddenConduit along horizontal rows where interior subroom doors exist.
        /// </summary>
        /// <param name="map">The map</param>
        /// <param name="roomRect">The room's bounding rect (includes walls)</param>
        /// <returns>Number of door rows connected</returns>
        internal static int ConnectInteriorDoorRows(Map map, CellRect roomRect)
        {
            ThingDef hiddenConduitDef = DefDatabase<ThingDef>.GetNamed("HiddenConduit", false);
            if (hiddenConduitDef == null)
            {
                Log.Warning("[Better Traders Guild] HiddenConduit def not found, cannot connect door rows");
                return 0;
            }

            ThingDef blastDoorDef = ThingDefOf.AncientBlastDoor;

            // Find all AncientBlastDoor in the room interior (not on room perimeter)
            HashSet<int> doorZCoordinates = new HashSet<int>();

            foreach (IntVec3 cell in roomRect)
            {
                // Skip room perimeter (where exterior doors are)
                if (cell.x == roomRect.minX || cell.x == roomRect.maxX ||
                    cell.z == roomRect.minZ || cell.z == roomRect.maxZ)
                    continue;

                Building edifice = cell.GetEdifice(map);
                if (edifice != null && edifice.def == blastDoorDef)
                {
                    doorZCoordinates.Add(cell.z);
                }
            }

            if (doorZCoordinates.Count == 0)
            {
                return 0;
            }

            int interiorMinX = roomRect.minX + 1;
            int interiorMaxX = roomRect.maxX - 1;

            // For each door z coordinate, place HiddenConduit along the entire row
            foreach (int z in doorZCoordinates)
            {
                for (int x = interiorMinX; x <= interiorMaxX; x++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);

                    if (!cell.InBounds(map))
                        continue;

                    // Check if cell already has a power transmitter
                    bool hasPowerTransmitter = false;
                    foreach (Thing thing in cell.GetThingList(map))
                    {
                        CompPower compPower = thing.TryGetComp<CompPower>();
                        if (compPower != null && compPower.Props.transmitsPower)
                        {
                            hasPowerTransmitter = true;
                            break;
                        }
                    }

                    if (!hasPowerTransmitter)
                    {
                        Thing conduit = ThingMaker.MakeThing(hiddenConduitDef);
                        GenSpawn.Spawn(conduit, cell, map);
                    }
                }
            }

            return doorZCoordinates.Count;
        }
    }
}
