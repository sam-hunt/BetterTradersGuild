using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Fills a single outfit stand with a set of apparel. GenStep_StockOutfitStands is the
    // only production caller; it decides which stands get which set (room rules, then the
    // map default) and this owns making, quality-rolling, tinting and inserting the items.
    //
    // LEARNING NOTE: Building_OutfitStand has a dedicated AddApparel(Apparel) method
    // that handles storage settings and display cache updates internally. This is
    // cleaner than the ThingOwner.TryAdd() pattern used for bookcases.
    public static class RoomOutfitStandHelper
    {
        // Adds one of each apparel def to a single outfit stand. Normalizes the stand for
        // HAR first (see OutfitStandHarFixer). stuff applies to every stuffable item (omit
        // for each item's default stuff).
        // Returns: number of apparel items added
        public static int FillOutfitStand(
            Building_OutfitStand outfitStand,
            List<ThingDef> apparelDefs,
            QualityCategory minQuality = QualityCategory.Normal,
            QualityCategory maxQuality = QualityCategory.Excellent,
            Faction faction = null,
            ThingDef stuff = null)
        {
            if (outfitStand == null || apparelDefs == null || apparelDefs.Count == 0)
                return 0;

            OutfitStandHarFixer.NormalizeOutfitStand(outfitStand);

            int added = 0;
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
                Apparel apparel = CreateApparelWithQuality(apparelDef, minQuality, maxQuality, stuff);
                if (apparel == null) continue;

                // Apply faction color if VEF is active
                ApparelFactionColorHelper.TryApplyFactionColor(apparel, faction);

                // Add to outfit stand using the dedicated API
                if (outfitStand.AddApparel(apparel))
                {
                    added++;
                }
                else
                {
                    // Clean up if insertion failed
                    Log.Warning($"[Better Traders Guild] Failed to add '{apparelDef.defName}' to outfit stand at {outfitStand.Position}");
                    apparel.Destroy(DestroyMode.Vanish);
                }
            }
            return added;
        }

        // Creates an apparel item with randomized quality within the specified range.
        //
        // LEARNING NOTE: Apparel items with CompQuality have their quality set via
        // CompQuality.SetQuality(). The QualityGenerator parameter affects logging
        // but doesn't change the actual quality value.
        // apparelDef: The apparel ThingDef to create
        // minQuality: Minimum quality (inclusive)
        // maxQuality: Maximum quality (inclusive)
        // stuff: stuff for a stuffable def, or null for its default stuff
        // Returns: Created Apparel with random quality, or null if creation failed
        private static Apparel CreateApparelWithQuality(
            ThingDef apparelDef,
            QualityCategory minQuality,
            QualityCategory maxQuality,
            ThingDef stuff)
        {
            // Determine stuff if required
            ThingDef stuffDef = null;
            if (apparelDef.MadeFromStuff)
            {
                stuffDef = stuff != null && stuff.IsStuff && stuff.stuffProps.CanMake(apparelDef)
                    ? stuff
                    : GenStuff.DefaultStuffFor(apparelDef);
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

        // Returns a random QualityCategory between min and max (inclusive).
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
