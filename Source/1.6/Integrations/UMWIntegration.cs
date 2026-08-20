using System;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Integrations
{
    // Optional integration with "Unique Melee Weapons" (UMW), a sibling mod by the same author.
    //
    // When UMW is active, the nursery caretaker's plain plasteel knife is upgraded to UMW's
    // unique knife variant carrying BTG's silver-inlay melee trait (BTG_SilverInlayMelee,
    // shipped in the 1.6/Mods/UniqueMeleeWeapons compat load root because its weaponCategory
    // references UMW's WeaponCategoryDef).
    //
    // Unlike the other integrations here, UMW is consumed purely through defs, not C# types, so
    // detection is by packageId (the ApparelFactionColorHelper idiom) and the lookups are
    // DefDatabase resolutions instead of reflection. GetNamedSilentFail keeps an absent def a
    // null, not an error. The Royalty-gated traits (UMW_Monomolecular/UMW_PlasmaCored are
    // MayRequire Royalty in UMW) are legitimately null without that DLC, so they are excluded
    // from Available and from the no-Royalty drift warning.
    //
    // Self-reports drift at startup (Pattern B, ported from UniqueWeaponsUnbound): silent when
    // UMW isn't active; a single Log.Warning when UMW IS active but an expected def is missing.
    //
    // Timing: unlike the reflection-based integrations, this one holds def INSTANCES, which an
    // in-process play-data reload replaces — so resolution lives in an idempotent Resolve()
    // invoked once per load from ReflectionVerification.VerifyAll (via BTGStartup.Run), never
    // a static ctor. Resolve() must not be called before defs are loaded; map-generation
    // consumers run safely after.
    public static class UMWIntegration
    {
        public const string PackageId = "shunter.uniquemeleeweapons";

        // True when the UMW mod is in the active mod list.
        public static bool ModActive { get; private set; }

        // UMW's unique variant of the vanilla knife (rolls its own texture/colours/name).
        public static ThingDef KnifeUnique { get; private set; }

        // Royalty-gated ultratech blade traits; null without Royalty (or if UMW drifted).
        public static WeaponTraitDef Monomolecular { get; private set; }
        public static WeaponTraitDef PlasmaCored { get; private set; }

        // Non-lethal capture coating; the caretaker's second trait when the ultratech pair is
        // unavailable.
        public static WeaponTraitDef Opiated { get; private set; }

        // True only when UMW is active AND everything the caretaker's knife needs resolved:
        // UMW's unique knife plus BTG's own compat-root-gated silver-inlay melee trait.
        public static bool Available =>
            ModActive && KnifeUnique != null && DefRefs.WeaponTraits.BTG_SilverInlayMelee != null;

        // Resolves (or re-resolves) UMW's defs against the current DefDatabase. Idempotent:
        // every field is overwritten each call, and the drift warnings re-fire at most once
        // per load.
        public static void Resolve()
        {
            try
            {
                ModActive = ModsConfig.IsActive(PackageId);
                if (!ModActive)
                    return; // UMW not active — stay silent.

                KnifeUnique = DefDatabase<ThingDef>.GetNamedSilentFail("UMW_Knife_Unique");
                Monomolecular = DefDatabase<WeaponTraitDef>.GetNamedSilentFail("UMW_Monomolecular");
                PlasmaCored = DefDatabase<WeaponTraitDef>.GetNamedSilentFail("UMW_PlasmaCored");
                Opiated = DefDatabase<WeaponTraitDef>.GetNamedSilentFail("UMW_Opiated");
            }
            catch (Exception ex)
            {
                Log.Warning("[Better Traders Guild] 'Unique Melee Weapons' def resolution failed "
                    + "(the nursery caretaker keeps a plain plasteel knife): " + ex);
                return;
            }

            // UMW is present but a def we rely on is missing — warn the affected user only.
            // Independent checks, not a chain: each is a distinct failure with its own remedy.
            if (KnifeUnique == null || Opiated == null)
            {
                Log.Warning("[Better Traders Guild] 'Unique Melee Weapons' active but "
                    + (KnifeUnique == null ? "UMW_Knife_Unique" : "UMW_Opiated")
                    + " could not be resolved; the nursery caretaker keeps a plain plasteel knife. "
                    + "The mod's defs may have changed.");
            }
            if (ModsConfig.RoyaltyActive && (Monomolecular == null || PlasmaCored == null))
            {
                Log.Warning("[Better Traders Guild] 'Unique Melee Weapons' and Royalty active but "
                    + (Monomolecular == null ? "UMW_Monomolecular" : "UMW_PlasmaCored")
                    + " could not be resolved; the caretaker's knife falls back to the opiated "
                    + "trait. The mod's defs may have changed.");
            }
            if (DefRefs.WeaponTraits.BTG_SilverInlayMelee == null)
            {
                Log.Warning("[Better Traders Guild] 'Unique Melee Weapons' active but BTG's own "
                    + "BTG_SilverInlayMelee trait did not load; the nursery caretaker keeps a "
                    + "plain plasteel knife. Check the 1.6/Mods/UniqueMeleeWeapons entry in "
                    + "LoadFolders.xml.");
            }
        }
    }
}
