using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Shared utilities for CrewQuarters subroom customization.
    /// Note: DLC def lookups now use centralized DefRefs classes instead of caching here.
    /// </summary>
    internal static class CrewQuartersHelpers
    {
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
