using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Fixes Humanoid Alien Races (HAR) compatibility issues with Building_OutfitStand.
    ///
    /// HAR adds Comp_OutfitStandHAR to all outfit stands. During PostSpawnSetup, it:
    /// 1. Accesses faction.def.basicMemberKind.race — TradersGuild has no basicMemberKind → NullRef
    /// 2. For factionless stands, defaults to Human and picks random body type from ALL
    ///    BodyTypeDefs (including Baby/Child) → store filter rejects adult apparel
    ///
    /// This helper normalizes outfit stands after spawn by fixing juvenile body types
    /// back to adult types via reflection, with a vanilla API fallback for the store filter.
    /// Safe to call when HAR is not installed (no-op).
    /// </summary>
    public static class OutfitStandHarFixer
    {
        private static bool _reflectionInitialized;
        private static Type _compType;
        private static PropertyInfo _bodyTypeProp;

        /// <summary>
        /// Normalizes an outfit stand after spawn by fixing HAR's juvenile body type selection.
        /// Safe to call when HAR is not installed. All errors are caught and logged.
        /// Call this BEFORE adding apparel to the stand.
        /// </summary>
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

        /// <summary>
        /// Attempts to fix HAR's body type via reflection.
        /// Returns true if HAR comp was found (regardless of whether fix was needed).
        /// Returns false if HAR is not installed.
        /// </summary>
        private static bool TryFixHarBodyType(Building_OutfitStand stand)
        {
            InitReflection();
            if (_compType == null) return false;

            ThingComp harComp = stand.AllComps?.FirstOrDefault(c => _compType.IsInstanceOfType(c));
            if (harComp == null) return false;

            if (_bodyTypeProp == null) return true; // HAR found but can't fix — caller should use fallback

            BodyTypeDef currentBody = _bodyTypeProp.GetValue(harComp) as BodyTypeDef;
            if (currentBody == null || currentBody == BodyTypeDefOf.Baby || currentBody == BodyTypeDefOf.Child)
            {
                _bodyTypeProp.SetValue(harComp, BodyTypeDefOf.Thin);
            }

            return true;
        }

        /// <summary>
        /// Vanilla API fallback: ensures the outfit stand's store filter accepts adult apparel.
        /// Used when HAR reflection fails or HAR is not installed but filter is still wrong.
        /// </summary>
        private static void EnsureAdultApparelFilter(Building_OutfitStand stand)
        {
            ThingFilter filter = stand.StoreSettings?.filter;
            if (filter == null) return;

            SpecialThingFilterDef adultFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowAdultOnlyApparel");
            SpecialThingFilterDef childFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowChildOnlyApparel");

            if (adultFilter != null) filter.SetAllow(adultFilter, true);
            if (childFilter != null) filter.SetAllow(childFilter, false);
        }

        private static void InitReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            _compType = GenTypes.GetTypeInAnyAssembly("AlienRace.Comp_OutfitStandHAR");
            if (_compType == null) return;

            _bodyTypeProp = _compType.GetProperty("BodyType",
                BindingFlags.Public | BindingFlags.Instance);
        }
    }
}
