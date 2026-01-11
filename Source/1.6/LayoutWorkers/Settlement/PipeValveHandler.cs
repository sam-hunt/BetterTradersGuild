using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.LayoutWorkers.Settlement
{
    /// <summary>
    /// Handles VE pipe network valve state and ownership after map generation.
    ///
    /// PURPOSE:
    /// After settlement generation, closes all VE pipe valves and removes faction ownership.
    /// This simulates a station lockdown state and enables room-by-room security progression:
    /// - Closed valves prevent resource flow until player manually opens them
    /// - Null faction allows players to claim valves once they secure the area
    ///
    /// GAMEPLAY DESIGN:
    /// With enhanced defenders and defenses, players may not be able to secure the entire
    /// settlement in one raid. Removing valve faction ownership enables interesting tactical
    /// choices: secure a room, claim its valve, extract resources, then decide whether to
    /// push deeper or evacuate with current gains.
    ///
    /// TECHNICAL NOTE:
    /// VE pipe valves use CompFlickable (standard RimWorld component) to control open/closed
    /// state. We simply set SwitchIsOn = false to close them. No reflection needed.
    ///
    /// CLAIMING BEHAVIOR:
    /// Standard RimWorld rules apply - players cannot claim buildings while hostile pawns
    /// are nearby. Once the area is secured (enemies killed/fled), valves become claimable.
    /// </summary>
    public static class PipeValveHandler
    {
        /// <summary>
        /// Closes all VE pipe valves on the map and removes faction ownership.
        /// Simulates station lockdown - players must claim valves room-by-room.
        /// </summary>
        public static void CloseAllValvesAndClearFaction(Map map)
        {
            // VE Chemfuel valves
            ProcessValvesOfDef(map, Things.VCHE_ChemfuelValve);

            // VE Nutrient Paste valve
            ProcessValvesOfDef(map, Things.VNPE_NutrientPasteValve);

            // VE Gravships valves
            // Skip the first oxygen valve to leave one open for initial oxygen flow
            ProcessValvesOfDef(map, Things.VGE_OxygenValve, skipFirst: true);
            ProcessValvesOfDef(map, Things.VGE_AstrofuelValve);
        }

        /// <summary>
        /// Processes all valves of a specific ThingDef on the map.
        /// </summary>
        /// <param name="map">The map to search</param>
        /// <param name="valveDef">The valve ThingDef (may be null if mod not installed)</param>
        /// <param name="skipFirst">If true, leaves the first valve open for initial resource flow</param>
        /// <returns>Number of valves processed</returns>
        private static int ProcessValvesOfDef(Map map, ThingDef valveDef, bool skipFirst = false)
        {
            if (valveDef == null)
                return 0;

            int processedCount = 0;
            bool skippedFirst = false;

            foreach (Thing thing in map.listerThings.ThingsOfDef(valveDef))
            {
                if (skipFirst && !skippedFirst)
                {
                    skippedFirst = true;
                    continue;
                }

                if (ProcessValve(thing))
                {
                    processedCount++;
                }
            }

            return processedCount;
        }

        /// <summary>
        /// Processes a single valve: closes it and removes faction ownership.
        /// </summary>
        /// <param name="valve">The valve Thing to process</param>
        /// <returns>True if valve was processed successfully</returns>
        private static bool ProcessValve(Thing valve)
        {
            // Must be a ThingWithComps to have comps
            ThingWithComps thingWithComps = valve as ThingWithComps;
            if (thingWithComps == null)
            {
                return false;
            }

            // Close the valve using CompFlickable (standard RimWorld component)
            // VE pipe valves use this to control open/closed state
            CompFlickable flickable = thingWithComps.GetComp<CompFlickable>();
            if (flickable != null)
            {
                // Set the switch to off (closed)
                flickable.SwitchIsOn = false;
            }

            // Remove faction ownership (set to null so player can claim once area is secured)
            // Note: Player still can't claim while hostile pawns are nearby (standard RimWorld behavior)
            valve.SetFactionDirect(null);

            return true;
        }
    }
}
