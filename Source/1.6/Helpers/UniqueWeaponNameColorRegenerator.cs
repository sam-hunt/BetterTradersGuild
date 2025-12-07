using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Regenerates CompUniqueWeapon name and color after manual trait modification.
    ///
    /// PROBLEM SOLVED:
    /// PostPostMake() cannot be called a second time to regenerate name/color
    /// because it has an early return guard in InitializeTraits() that skips
    /// regeneration if traits already exist.
    ///
    /// SOLUTION:
    /// Directly set the private 'name' and 'color' fields via reflection,
    /// bypassing the PostPostMake() guard entirely.
    /// </summary>
    public static class UniqueWeaponNameColorRegenerator
    {
        // Cache the FieldInfo objects for performance (reflection is expensive)
        private static readonly FieldInfo NameField = typeof(CompUniqueWeapon)
            .GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo ColorField = typeof(CompUniqueWeapon)
            .GetField("color", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Regenerates weapon name and color based on current traits.
        /// Must be called AFTER all traits have been added to the component.
        /// </summary>
        public static void RegenerateNameAndColor(Thing weapon, CompUniqueWeapon uniqueComp)
        {
            if (weapon == null || uniqueComp == null)
                return;

            try
            {
                // Step 1: Determine weapon color
                // Priority: trait.forcedColor > random color
                ColorDef weaponColor = SelectWeaponColor(uniqueComp);
                if (weaponColor != null && ColorField != null)
                {
                    ColorField.SetValue(uniqueComp, weaponColor);
                }

                // Step 2: Generate weapon name based on traits and color
                string weaponName = GenerateWeaponName(uniqueComp);
                if (!weaponName.NullOrEmpty() && NameField != null)
                {
                    NameField.SetValue(uniqueComp, weaponName);
                }

                // Step 3: Update CompArt title if it exists
                // This ensures the name appears in inspection dialogs and UI
                CompArt artComp = weapon.TryGetComp<CompArt>();
                if (artComp != null && !weaponName.NullOrEmpty())
                {
                    artComp.Title = weaponName;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Better Traders Guild] Failed to regenerate unique weapon name/color: {ex}");
            }
        }

        /// <summary>
        /// Selects weapon color based on trait definitions.
        /// Traits with forcedColor (like GoldInlay) take priority.
        /// </summary>
        private static ColorDef SelectWeaponColor(CompUniqueWeapon uniqueComp)
        {
            if (uniqueComp?.TraitsListForReading == null || uniqueComp.TraitsListForReading.Count == 0)
                return null;

            // Step 1: Check if any trait forces a specific color
            foreach (WeaponTraitDef trait in uniqueComp.TraitsListForReading)
            {
                if (trait?.forcedColor != null)
                {
                    return trait.forcedColor;
                }
            }

            // Step 2: If no forced color, select any random color that exists
            var allColors = new List<ColorDef>(DefDatabase<ColorDef>.AllDefs);
            if (allColors.Count > 0)
            {
                return allColors.RandomElement();
            }

            return null;
        }

        /// <summary>
        /// Generates weapon name using trait adjectives, color, and weapon type.
        /// Format: "[adjective] [color] [weapon_type]"
        /// Examples: "Golden Charge Rifle", "Brilliant Revolver"
        /// </summary>
        private static string GenerateWeaponName(CompUniqueWeapon uniqueComp)
        {
            if (uniqueComp == null)
                return "Unique Weapon";

            // Collect all adjectives from traits
            var allAdjectives = new List<string>();
            foreach (WeaponTraitDef trait in uniqueComp.TraitsListForReading)
            {
                if (trait?.traitAdjectives != null && trait.traitAdjectives.Count > 0)
                {
                    allAdjectives.AddRange(trait.traitAdjectives);
                }
            }

            // Get the weapon color (or null if not set yet)
            ColorDef color = ColorField != null ? (ColorDef)ColorField.GetValue(uniqueComp) : null;
            string colorName = color?.label ?? "Blue";

            // Get weapon type from component properties
            string weaponType = "Weapon";
            if (uniqueComp.Props?.namerLabels != null && uniqueComp.Props.namerLabels.Count > 0)
            {
                weaponType = uniqueComp.Props.namerLabels.RandomElement();
            }

            // Build the final name
            if (allAdjectives.Count > 0)
            {
                string adjective = allAdjectives.RandomElement();
                return $"{adjective.CapitalizeFirst()} {colorName.CapitalizeFirst()} {weaponType}";
            }
            else
            {
                return $"{colorName.CapitalizeFirst()} {weaponType}";
            }
        }
    }
}
