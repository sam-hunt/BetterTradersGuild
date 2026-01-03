using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CommandersQuarters
{
    /// <summary>
    /// Handles unique weapon generation and spawning for the Commander's Quarters.
    /// Creates a high-quality weapon with gold inlay and weapon-specific traits.
    /// </summary>
    public static class CommandersWeaponSpawner
    {
        /// <summary>
        /// Spawns a unique weapon on the shelf in the commander's quarters.
        /// Searches for the ShelfSmall within the entire room area (bedroom + lounge).
        ///
        /// LEARNING NOTE: The shelf is in the bedroom prefab, but we search the full room
        /// to be robust. Also, we must add at least one trait BEFORE GoldInlay since it
        /// has canGenerateAlone="false" which prevents it from being the only trait.
        /// </summary>
        public static void SpawnUniqueWeaponOnShelf(Map map, LayoutRoom room, CellRect bedroomRect)
        {
            // Generate the unique weapon
            Thing weapon = GenerateCommandersWeapon();

            if (weapon == null)
            {
                Log.Warning("[Better Traders Guild] Failed to generate commander's unique weapon");
                return;
            }

            // Find the ShelfSmall within the entire room area (not just bedroom)
            Building_Storage shelf = null;
            int shelvesFound = 0;

            // Search full room rect (bedroom + lounge)
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                foreach (IntVec3 cell in roomRect.Cells)
                {
                    if (!cell.InBounds(map)) continue;

                    List<Thing> things = cell.GetThingList(map);
                    if (things != null)
                    {
                        foreach (Thing thing in things)
                        {
                            if (thing.def == Things.ShelfSmall && thing is Building_Storage storage)
                            {
                                shelvesFound++;
                                shelf = storage;
                                break;
                            }
                        }
                    }

                    if (shelf != null)
                    {
                        // Spawn weapon at the shelf's position
                        GenSpawn.Spawn(weapon, shelf.Position, map, Rot4.North);
                        break;
                    }
                }
            }

            if (shelf == null)
            {
                Log.Warning($"[Better Traders Guild] Could not find ShelfSmall in commander's quarters - spawning weapon at bedroom center");
                GenSpawn.Spawn(weapon, bedroomRect.CenterCell, map, Rot4.North);
            }

            weapon.SetForbidden(true, false);
        }

        /// <summary>
        /// Generates a unique high-quality weapon with gold inlay for the commander's bedroom.
        ///
        /// Weapon selection (weighted random):
        /// - 30% Revolver (Gold + HighPowerRounds + random)
        /// - 30% Charge Rifle (Gold + ChargeCapacitor + random)
        /// - 20% Charge Lance (Gold + ChargeCapacitor + random)
        /// - 20% Beam Repeater (Gold + FrequencyAmplifier + random)
        ///
        /// All weapons spawn with Excellent/Masterwork/Legendary quality via QualityUtility.GenerateQualitySuper()
        /// and have 3 unique traits (Gold Inlay + weapon-specific + random compatible).
        /// </summary>
        private static Thing GenerateCommandersWeapon()
        {
            // Weighted random weapon selection
            float roll = Rand.Value;
            ThingDef weaponDef;
            WeaponTraitDef primaryTrait;

            if (roll < 0.3f)
            {
                weaponDef = Things.Gun_Revolver_Unique;
                primaryTrait = WeaponTraits.PulseCharger;
            }
            else if (roll < 0.6f)
            {
                weaponDef = Things.Gun_ChargeRifle_Unique;
                primaryTrait = WeaponTraits.ChargeCapacitor;
            }
            else if (roll < 0.8f)
            {
                weaponDef = Things.Gun_ChargeLance_Unique;
                primaryTrait = WeaponTraits.ChargeCapacitor;
            }
            else
            {
                weaponDef = Things.Gun_BeamRepeater_Unique;
                primaryTrait = WeaponTraits.FrequencyAmplifier;
            }

            if (weaponDef == null)
            {
                Log.Error("[Better Traders Guild] Selected unique weapon ThingDef is null");
                return null;
            }

            Thing weapon = ThingMaker.MakeThing(weaponDef, null);

            // Set quality using Super Generator (biased toward high quality)
            CompQuality qualityComp = weapon.TryGetComp<CompQuality>();
            if (qualityComp != null)
            {
                QualityCategory quality = QualityUtility.GenerateQualitySuper();
                qualityComp.SetQuality(quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
            }

            // Add weapon traits
            CompUniqueWeapon uniqueComp = weapon.TryGetComp<CompUniqueWeapon>();
            if (uniqueComp != null)
            {
                // CRITICAL: Clear any randomly-generated traits that were added during ThingMaker.MakeThing()
                // This prevents ending up with 6+ traits (3 random + 3 ours)
                // Following vanilla pattern from DebugToolsMisc.RemoveTraitFromUniqueWeapon
                uniqueComp.TraitsListForReading.Clear();

                // Trait 1: Weapon-specific primary trait (MUST be added FIRST)
                // GoldInlay has canGenerateAlone="false" so it needs another trait to exist first
                // primaryTrait was already selected above based on weapon type
                // Fall back to AimAssistance if the selected trait is null
                WeaponTraitDef effectivePrimaryTrait = primaryTrait ?? WeaponTraits.AimAssistance;
                if (effectivePrimaryTrait != null && uniqueComp.CanAddTrait(effectivePrimaryTrait))
                {
                    uniqueComp.AddTrait(effectivePrimaryTrait);
                }
                else
                {
                    Log.Warning($"[Better Traders Guild] Could not add primary trait to commander's weapon");
                }

                // Trait 2: Gold Inlay (added SECOND after primary trait exists)
                // Note: GoldInlay requires Biotech DLC
                if (WeaponTraits.GoldInlay != null && uniqueComp.CanAddTrait(WeaponTraits.GoldInlay))
                {
                    uniqueComp.AddTrait(WeaponTraits.GoldInlay);
                }
                else
                {
                    Log.Warning("[Better Traders Guild] Could not add GoldInlay trait to commander's weapon (may require Biotech DLC)");
                }

                // Trait 3: Random compatible third trait
                List<WeaponTraitDef> compatibleTraits = DefDatabase<WeaponTraitDef>.AllDefs
                    .Where(traitDef => uniqueComp.CanAddTrait(traitDef))
                    .ToList();

                if (compatibleTraits.Count > 0)
                {
                    WeaponTraitDef randomTrait = compatibleTraits.RandomElement();
                    uniqueComp.AddTrait(randomTrait);
                }
                else
                {
                    Log.Warning("[Better Traders Guild] No compatible traits available for third trait slot");
                }

                // CRITICAL: Regenerate weapon name and color based on new traits
                // PostPostMake() cannot be used here because it has an early return guard
                // in InitializeTraits() that prevents regeneration when traits already exist.
                // Instead, use reflection-based helper to set name/color fields directly.
                UniqueWeaponNameColorRegenerator.RegenerateNameAndColor(weapon, uniqueComp);
            }
            else
            {
                Log.Warning($"[Better Traders Guild] Weapon '{weaponDef.defName}' does not have CompUniqueWeapon");
            }

            return weapon;
        }
    }
}
