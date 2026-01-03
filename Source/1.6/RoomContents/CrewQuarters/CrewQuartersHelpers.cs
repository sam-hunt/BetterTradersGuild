using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Shared utilities and DLC def cache for CrewQuarters subroom customization.
    /// Caches DLC-dependent defs once per room generation to avoid repeated lookups.
    /// </summary>
    internal static class CrewQuartersHelpers
    {
        #region DLC Def Cache

        // Odyssey drones
        internal static ThingDef HunterDroneDef { get; private set; }
        internal static ThingDef WaspDroneDef { get; private set; }

        // Biotech mechs
        internal static PawnKindDef MilitorKind { get; private set; }

        // Anomaly items
        internal static PawnKindDef ShamblerKind { get; private set; }
        internal static ThingDef ScrapCubeDef { get; private set; }
        internal static ThingDef GoldenCubeDef { get; private set; }

        // Ideology items
        internal static ThingDef SlaveHarnessDef { get; private set; }

        // VFE Spacer Module items
        internal static ThingDef InteractiveTableDef { get; private set; }
        internal static ThingDef AirPurifierDef { get; private set; }

        // Common defs
        internal static ThingDef SteelDef { get; private set; }
        internal static ThingDef ShelfSmallDef { get; private set; }

        /// <summary>
        /// Caches DLC-dependent defs at start of customization.
        /// Using DefDatabase.GetNamed with errorOnFail=false - null means DLC not available.
        /// </summary>
        internal static void CacheDlcDefs()
        {
            // Odyssey drones
            HunterDroneDef = DefDatabase<ThingDef>.GetNamed("HunterDroneTrap", false);
            WaspDroneDef = DefDatabase<ThingDef>.GetNamed("WaspDroneTrap", false);

            // Biotech mechs
            MilitorKind = DefDatabase<PawnKindDef>.GetNamed("Mech_Militor", false);

            // Anomaly items
            ShamblerKind = DefDatabase<PawnKindDef>.GetNamed("ShamblerSwarmer", false);
            ScrapCubeDef = DefDatabase<ThingDef>.GetNamed("ScrapCubeSculpture", false);
            GoldenCubeDef = DefDatabase<ThingDef>.GetNamed("GoldenCube", false);

            // Ideology items
            SlaveHarnessDef = DefDatabase<ThingDef>.GetNamed("Apparel_BodyStrap", false);

            // VFE Spacer Module items
            InteractiveTableDef = DefDatabase<ThingDef>.GetNamed("Table_interactive_1x1c", false);
            AirPurifierDef = DefDatabase<ThingDef>.GetNamed("VFES_AirPurifier", false);

            // Common defs
            SteelDef = ThingDefOf.Steel;
            ShelfSmallDef = DefDatabase<ThingDef>.GetNamed("ShelfSmall", false);
        }

        #endregion

        #region Shared Utilities

        /// <summary>
        /// Replaces a thing with a new thing of the specified def at the same position.
        /// Preserves rotation only for things with meaningful directional graphics.
        /// </summary>
        internal static Thing ReplaceThingAt(Thing oldThing, ThingDef newDef, ThingDef stuff, Map map)
        {
            IntVec3 pos = oldThing.Position;
            Rot4 oldRot = oldThing.Rotation;
            oldThing.Destroy(DestroyMode.Vanish);

            // Only preserve rotation if the new thing has meaningful rotation
            // (Graphic_Multi provides directional graphics, rotatable allows player rotation)
            bool hasMeaningfulRotation = newDef.rotatable &&
                newDef.graphicData?.graphicClass == typeof(Graphic_Multi);
            Rot4 rot = hasMeaningfulRotation ? oldRot : Rot4.North;

            Thing newThing = ThingMaker.MakeThing(newDef, stuff);
            GenSpawn.Spawn(newThing, pos, map, rot);
            return newThing;
        }

        /// <summary>
        /// Finds all things of a specific def within the given subroom rects.
        /// </summary>
        internal static List<T> FindThingsInSubrooms<T>(Map map, List<CellRect> subroomRects, ThingDef thingDef) where T : Thing
        {
            List<T> results = new List<T>();
            HashSet<T> seen = new HashSet<T>();

            foreach (CellRect subroomRect in subroomRects)
            {
                foreach (IntVec3 cell in subroomRect)
                {
                    if (!cell.InBounds(map)) continue;
                    foreach (Thing thing in cell.GetThingList(map))
                    {
                        if (thing.def == thingDef && thing is T typed && !seen.Contains(typed))
                        {
                            seen.Add(typed);
                            results.Add(typed);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Finds all things of a specific def within the given subroom rects (non-generic version).
        /// </summary>
        internal static List<Thing> FindThingsInSubrooms(Map map, List<CellRect> subroomRects, ThingDef thingDef)
        {
            return FindThingsInSubrooms<Thing>(map, subroomRects, thingDef);
        }

        #endregion
    }
}
