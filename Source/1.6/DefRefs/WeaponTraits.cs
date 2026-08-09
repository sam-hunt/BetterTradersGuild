using RimWorld;

namespace BetterTradersGuild.DefRefs
{
    // Centralized WeaponTraitDef references.
    [DefOf]
    public static class WeaponTraits
    {
        public static WeaponTraitDef AimAssistance;
        public static WeaponTraitDef ChargeCapacitor;
        public static WeaponTraitDef PulseCharger;
        public static WeaponTraitDef FrequencyAmplifier;

        public static WeaponTraitDef GoldInlay;
        public static WeaponTraitDef SilverInlay;

        // Melee counterpart of SilverInlay; its def ships in the UMW compat load root
        // (its weaponCategory references UMW's WeaponCategoryDef), so null when UMW is
        // inactive.
        [MayRequire(Integrations.UMWIntegration.PackageId)]
        public static WeaponTraitDef BTG_SilverInlayMelee;

        static WeaponTraits() => DefOfHelper.EnsureInitializedInCtor(typeof(WeaponTraits));
    }
}
