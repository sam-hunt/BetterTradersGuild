using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    // Centralized WeaponCategoryDef references.
    [DefOf]
    public static class WeaponCategories
    {
        public static WeaponCategoryDef PulseCharge;
        public static WeaponCategoryDef BeamWeapon;

        static WeaponCategories() => DefOfHelper.EnsureInitializedInCtor(typeof(WeaponCategories));
    }
}
