using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized WeaponTraitDef references.
    /// </summary>
    [DefOf]
    public static class WeaponTraits
    {
        public static WeaponTraitDef AimAssistance;
        public static WeaponTraitDef ChargeCapacitor;
        public static WeaponTraitDef PulseCharger;
        public static WeaponTraitDef FrequencyAmplifier;

        [MayRequireBiotech]
        public static WeaponTraitDef GoldInlay;

        static WeaponTraits() => DefOfHelper.EnsureInitializedInCtor(typeof(WeaponTraits));
    }
}
