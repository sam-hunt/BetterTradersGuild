using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Discovers and caches all unique weapons with PulseCharge or BeamWeapon categories.
    /// Used by CommandersWeaponSpawner and ShelfCustomizer for dynamic weapon pool selection.
    /// Automatically includes VWE weapons when Vanilla Weapons Expanded is loaded.
    /// </summary>
    internal static class UniqueWeaponPoolHelper
    {
        private static List<ThingDef> cachedWeapons;
        private static bool initialized;

        /// <summary>
        /// Gets all unique weapon ThingDefs whose CompProperties_UniqueWeapon.weaponCategories
        /// include PulseCharge or BeamWeapon. Results are cached after first call.
        /// </summary>
        internal static IReadOnlyList<ThingDef> GetPulseChargeAndBeamWeapons()
        {
            if (!initialized)
            {
                cachedWeapons = BuildWeaponPool();
                initialized = true;
            }
            return cachedWeapons;
        }

        /// <summary>
        /// Returns the appropriate primary WeaponTraitDef for a weapon based on its category:
        /// PulseCharge -> ChargeCapacitor, BeamWeapon -> FrequencyAmplifier.
        /// Falls back to AimAssistance if neither category is found.
        /// </summary>
        internal static WeaponTraitDef GetPrimaryTrait(ThingDef weaponDef)
        {
            if (HasCategory(weaponDef, WeaponCategories.PulseCharge))
                return WeaponTraits.ChargeCapacitor;
            if (HasCategory(weaponDef, WeaponCategories.BeamWeapon))
                return WeaponTraits.FrequencyAmplifier;
            return WeaponTraits.AimAssistance;
        }

        private static bool HasCategory(ThingDef weaponDef, WeaponCategoryDef category)
        {
            if (weaponDef == null || category == null) return false;
            var props = weaponDef.comps?.OfType<CompProperties_UniqueWeapon>().FirstOrDefault();
            return props?.weaponCategories != null && props.weaponCategories.Contains(category);
        }

        private static List<ThingDef> BuildWeaponPool()
        {
            var weapons = new List<ThingDef>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.comps == null) continue;
                var props = def.comps.OfType<CompProperties_UniqueWeapon>().FirstOrDefault();
                if (props?.weaponCategories == null) continue;

                if (props.weaponCategories.Contains(WeaponCategories.PulseCharge)
                    || props.weaponCategories.Contains(WeaponCategories.BeamWeapon))
                {
                    weapons.Add(def);
                }
            }

            if (weapons.Count == 0)
                Log.Warning("[Better Traders Guild] UniqueWeaponPoolHelper: No PulseCharge or BeamWeapon unique weapons found in DefDatabase");
            else
                Log.Message($"[Better Traders Guild] UniqueWeaponPoolHelper: Discovered {weapons.Count} PulseCharge/BeamWeapon unique weapons");

            return weapons;
        }
    }
}
