using System.Reflection;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Resets and rewires a unique weapon's CompEquippableAbilityReloadable after manual trait
    // modification.
    //
    // PROBLEM SOLVED:
    // PostPostMake() rolls random traits and runs CompUniqueWeapon.Setup, which points the
    // ability comp's props at any rolled trait's abilityProps and materializes the Ability
    // instance. BTG's spawners then discard the roll (TraitsListForReading.Clear()) and add
    // their own traits - but the comp keeps the discarded trait's props and Ability, so the
    // weapon can retain an ability from a trait it no longer has (e.g. Odyssey's EMPPulser on
    // PulseCharge weapons). Conversely, an ability trait added after the clear is never wired
    // up, because vanilla only runs Setup during generation/load.
    //
    // SOLUTION:
    // Restore the def's authored comp props, drop the materialized Ability via reflection
    // (CompEquippableAbility.ability is private with no reset path), then re-run the public
    // Setup(fromSave: false) so the traits now on the weapon wire themselves back up.
    public static class UniqueWeaponAbilityResetter
    {
        private static readonly FieldInfo AbilityField = typeof(CompEquippableAbility)
            .GetField("ability", BindingFlags.NonPublic | BindingFlags.Instance);

        // Logs a targeted error for any member that failed to resolve. Called once at startup
        // from ReflectionVerification.VerifyAll.
        public static void VerifyReflection()
        {
            if (AbilityField == null)
                Log.Error("[Better Traders Guild] CompEquippableAbility.ability field not found via reflection; "
                    + "unique weapons from settlements may keep an ability from a discarded random trait. "
                    + "RimWorld API may have changed.");
        }

        // Discards whatever ability the initial random trait roll wired up and rewires from the
        // traits currently on the weapon (a no-op ability-wise when none carry abilityProps).
        // Must be called AFTER all traits have been added and BEFORE the weapon is equipped.
        public static void ResetAndRewire(Thing weapon, CompUniqueWeapon uniqueComp)
        {
            if (weapon == null || uniqueComp == null)
                return;

            CompEquippableAbilityReloadable abilityComp = weapon.TryGetComp<CompEquippableAbilityReloadable>();
            if (abilityComp == null)
                return;

            CompProperties_EquippableAbilityReloadable originalProps =
                weapon.def.GetCompProperties<CompProperties_EquippableAbilityReloadable>();
            if (originalProps != null)
                abilityComp.props = originalProps;

            // Degrades gracefully: if the field didn't resolve, the stale Ability instance
            // survives, but props are already restored so no new one is created from it.
            AbilityField?.SetValue(abilityComp, null);

            uniqueComp.Setup(fromSave: false);
        }
    }
}
