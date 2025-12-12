using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Helper class for spawning apparel into outfit stands during room generation.
    /// Provides methods for populating outfit stands with armor, clothing, and weapons.
    ///
    /// LEARNING NOTE: Building_OutfitStand has a dedicated AddApparel(Apparel) method
    /// that handles storage settings and display cache updates internally. This is
    /// cleaner than the ThingOwner.TryAdd() pattern used for bookcases.
    ///
    /// USAGE: Designed for reuse in any RoomContentsWorker. Call this AFTER base.FillRoom()
    /// to populate outfit stands placed by XML prefabs.
    /// </summary>
    public static class RoomOutfitStandHelper
    {
        /// <summary>
        /// Spawns apparel items into all outfit stands within the search area.
        /// Each outfit stand receives the full set of apparel items specified.
        ///
        /// Quality is randomized within the specified range for each item.
        /// Items that require stuff (like flak vest) will use default stuff.
        /// </summary>
        /// <param name="map">The map to search for outfit stands</param>
        /// <param name="searchArea">Area to search (typically the full room rect)</param>
        /// <param name="apparelDefs">List of apparel ThingDefs to spawn in each stand</param>
        /// <param name="minQuality">Minimum quality for randomization (default: Normal)</param>
        /// <param name="maxQuality">Maximum quality for randomization (default: Excellent)</param>
        public static void SpawnApparelInOutfitStands(
            Map map,
            CellRect searchArea,
            List<ThingDef> apparelDefs,
            QualityCategory minQuality = QualityCategory.Normal,
            QualityCategory maxQuality = QualityCategory.Excellent)
        {
            if (apparelDefs == null || apparelDefs.Count == 0)
            {
                return;
            }

            // Find all unique outfit stands in search area
            // Use HashSet to avoid duplicates (multi-cell buildings appear at multiple positions)
            HashSet<Building_OutfitStand> uniqueStands = new HashSet<Building_OutfitStand>();

            foreach (IntVec3 cell in searchArea.Cells)
            {
                if (!cell.InBounds(map)) continue;

                List<Thing> things = cell.GetThingList(map);
                if (things == null) continue;

                foreach (Thing thing in things)
                {
                    if (thing is Building_OutfitStand outfitStand)
                    {
                        uniqueStands.Add(outfitStand);
                    }
                }
            }

            if (uniqueStands.Count == 0)
            {
                return; // No outfit stands found (may not be an error - some prefab variations might not include them)
            }

            // Populate each outfit stand with apparel
            foreach (Building_OutfitStand outfitStand in uniqueStands)
            {
                foreach (ThingDef apparelDef in apparelDefs)
                {
                    if (apparelDef == null)
                    {
                        Log.Warning("[Better Traders Guild] Null apparel def in list, skipping");
                        continue;
                    }

                    if (!apparelDef.IsApparel)
                    {
                        Log.Warning($"[Better Traders Guild] ThingDef '{apparelDef.defName}' is not apparel, skipping");
                        continue;
                    }

                    // Create the apparel item
                    Apparel apparel = CreateApparelWithQuality(apparelDef, minQuality, maxQuality);
                    if (apparel == null) continue;

                    // Add to outfit stand using the dedicated API
                    bool success = outfitStand.AddApparel(apparel);
                    if (!success)
                    {
                        // Clean up if insertion failed
                        Log.Warning($"[Better Traders Guild] Failed to add '{apparelDef.defName}' to outfit stand at {outfitStand.Position}");
                        apparel.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an apparel item with randomized quality within the specified range.
        ///
        /// LEARNING NOTE: Apparel items with CompQuality have their quality set via
        /// CompQuality.SetQuality(). The QualityGenerator parameter affects logging
        /// but doesn't change the actual quality value.
        /// </summary>
        /// <param name="apparelDef">The apparel ThingDef to create</param>
        /// <param name="minQuality">Minimum quality (inclusive)</param>
        /// <param name="maxQuality">Maximum quality (inclusive)</param>
        /// <returns>Created Apparel with random quality, or null if creation failed</returns>
        private static Apparel CreateApparelWithQuality(
            ThingDef apparelDef,
            QualityCategory minQuality,
            QualityCategory maxQuality)
        {
            // Determine stuff if required
            ThingDef stuffDef = null;
            if (apparelDef.MadeFromStuff)
            {
                stuffDef = GenStuff.DefaultStuffFor(apparelDef);
                if (stuffDef == null)
                {
                    Log.Warning($"[Better Traders Guild] Could not find default stuff for '{apparelDef.defName}'");
                    return null;
                }
            }

            // Create the apparel
            Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuffDef);
            if (apparel == null)
            {
                Log.Warning($"[Better Traders Guild] Failed to create apparel '{apparelDef.defName}'");
                return null;
            }

            // Set random quality within range
            CompQuality compQuality = apparel.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                QualityCategory quality = RandomQualityInRange(minQuality, maxQuality);
                compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
            }

            return apparel;
        }

        /// <summary>
        /// Returns a random QualityCategory between min and max (inclusive).
        /// </summary>
        private static QualityCategory RandomQualityInRange(
            QualityCategory min,
            QualityCategory max)
        {
            // QualityCategory is an enum with integer values
            int minInt = (int)min;
            int maxInt = (int)max;

            // Ensure valid range
            if (minInt > maxInt)
            {
                (minInt, maxInt) = (maxInt, minInt);
            }

            int randomValue = Rand.RangeInclusive(minInt, maxInt);
            return (QualityCategory)randomValue;
        }
    }
}
