using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
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
        /// VE pipe valve defNames to process.
        /// These are the valves placed by BTG prefabs for tank systems.
        /// </summary>
        private static readonly string[] ValveDefNames = new string[]
        {
            // VE Chemfuel valves
            "VCHE_ChemfuelValve",
            "VCHE_DeepchemValve",
            // VE Nutrient Paste valve
            "VNPE_NutrientPasteValve",
            // VE Gravships valves
            "VGE_OxygenValve",
            "VGE_AstrofuelValve"
        };


        /// <summary>
        /// Closes all VE pipe valves on the map and removes faction ownership.
        ///
        /// BEHAVIOR:
        /// - Finds all Things on map matching supported valve defNames
        /// - For each valve, uses CompFlickable to turn it off (closed state)
        /// - Sets faction to null (claimable by player once area is secured)
        ///
        /// TECHNICAL NOTE:
        /// VE pipe valves use CompFlickable (standard RimWorld component) to control
        /// open/closed state, not a custom field. Setting SwitchIsOn = false closes them.
        /// </summary>
        /// <param name="map">The map containing valves to process</param>
        /// <returns>Number of valves processed</returns>
        public static int CloseAllValvesAndClearFaction(Map map)
        {
            int processedCount = 0;

            // Process each valve defName
            foreach (string valveDefName in ValveDefNames)
            {
                // Get ThingDef (silent fail for optional mods)
                ThingDef valveDef = DefDatabase<ThingDef>.GetNamedSilentFail(valveDefName);
                if (valveDef == null)
                {
                    continue; // This VE mod not installed, skip
                }

                // Find all things of this def on the map
                foreach (Thing thing in map.listerThings.ThingsOfDef(valveDef))
                {
                    if (thing == null)
                        continue;

                    // Process the valve: close it and clear faction
                    bool processed = ProcessValve(thing);
                    if (processed)
                    {
                        processedCount++;
                    }
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
