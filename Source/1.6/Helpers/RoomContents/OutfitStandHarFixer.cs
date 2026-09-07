using System;
using System.Linq;
using BetterTradersGuild.Integrations;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Fixes a Humanoid Alien Races (HAR) quirk on freshly spawned Building_OutfitStands.
    //
    // HAR adds Comp_OutfitStandHAR to every outfit stand. PostSpawnSetup resolves the stand's
    // race from its faction (Human for factionless stands and for any faction whose basic
    // member kind is human, i.e. every BTG stand). The Race setter then rolls a body type; for
    // non-alien races the pool is ALL BodyTypeDefs, which with Biotech includes Baby and Child.
    // A juvenile roll flips the store filter to child-only apparel, so adult apparel is rejected
    // (or dropped by HAR's deferred graphics recache once the map finishes generating).
    //
    // This helper normalizes a stand right after spawn by forcing a juvenile body type back to
    // an adult one via reflection, with a vanilla API fallback for the store filter.
    // Safe to call when HAR is not installed (no-op). Call it BEFORE adding apparel.
    public static class OutfitStandHarFixer
    {
        // Normalizes an outfit stand after spawn by fixing HAR's juvenile body type selection.
        // Safe to call when HAR is not installed. All errors are caught and logged.
        // Call this BEFORE adding apparel to the stand.
        public static void NormalizeOutfitStand(Building_OutfitStand stand)
        {
            if (stand == null) return;

            try
            {
                if (!TryFixHarBodyType(stand))
                    EnsureAdultApparelFilter(stand);
            }
            catch (Exception e)
            {
                Log.Warning($"[Better Traders Guild] Failed to normalize outfit stand at {stand.Position}: {e.Message}");
                // Last resort: ensure the filter at least accepts adult apparel
                try { EnsureAdultApparelFilter(stand); }
                catch { /* silently give up */ }
            }
        }

        // Attempts to fix HAR's body type via reflection.
        // Returns true if HAR comp was found (regardless of whether fix was needed).
        // Returns false if HAR is not installed.
        private static bool TryFixHarBodyType(Building_OutfitStand stand)
        {
            if (HARIntegration.CompType == null) return false;

            ThingComp harComp = stand.AllComps?.FirstOrDefault(c => HARIntegration.CompType.IsInstanceOfType(c));
            if (harComp == null) return false;

            if (HARIntegration.BodyTypeProperty == null) return true; // HAR found but can't fix — caller uses fallback

            BodyTypeDef currentBody = HARIntegration.BodyTypeProperty.GetValue(harComp) as BodyTypeDef;
            if (currentBody == null || currentBody == BodyTypeDefOf.Baby || currentBody == BodyTypeDefOf.Child)
            {
                HARIntegration.BodyTypeProperty.SetValue(harComp, BodyTypeDefOf.Thin);
            }

            return true;
        }

        // Vanilla API fallback: ensures the outfit stand's store filter accepts adult apparel.
        // Used when HAR reflection fails or HAR is not installed but filter is still wrong.
        private static void EnsureAdultApparelFilter(Building_OutfitStand stand)
        {
            ThingFilter filter = stand.StoreSettings?.filter;
            if (filter == null) return;

            SpecialThingFilterDef adultFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowAdultOnlyApparel");
            SpecialThingFilterDef childFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowChildOnlyApparel");

            if (adultFilter != null) filter.SetAllow(adultFilter, true);
            if (childFilter != null) filter.SetAllow(childFilter, false);
        }
    }
}
