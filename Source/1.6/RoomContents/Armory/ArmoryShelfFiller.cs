using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Armory
{
    /// <summary>
    /// Fills armory shelves with randomized military equipment.
    /// Each shelf gets content from a randomly selected pool:
    /// - Mortar shells (25% antigrain, 75% random shells)
    /// - Charge rifles (Normal/Good/Excellent quality distribution)
    /// - Shield belt + gunlink combo
    /// - Charge lances (Normal/Good/Excellent quality distribution)
    /// </summary>
    public static class ArmoryShelfFiller
    {
        private enum ShelfContentPool
        {
            MortarShells,
            ChargeRifles,
            ShieldBeltGunlink,
            ChargeLances
        }

        /// <summary>
        /// Finds all 2-cell wide shelves in the room and fills them with random content.
        /// Each shelf gets content from a randomly selected pool.
        /// </summary>
        public static void FillWeaponShelves(Map map, CellRect roomRect)
        {
            List<Building_Storage> weaponShelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            foreach (Building_Storage shelf in weaponShelves)
            {
                ShelfContentPool pool = GetRandomContentPool();
                FillShelfWithContent(map, shelf, pool);
            }
        }

        private static ShelfContentPool GetRandomContentPool()
        {
            // Equal weights for all pools
            return (ShelfContentPool)Rand.Range(0, 4);
        }

        private static void FillShelfWithContent(Map map, Building_Storage shelf, ShelfContentPool pool)
        {
            switch (pool)
            {
                case ShelfContentPool.MortarShells:
                    FillWithMortarShells(map, shelf);
                    break;
                case ShelfContentPool.ChargeRifles:
                    FillWithWeapons(map, shelf, Things.Gun_ChargeRifle);
                    break;
                case ShelfContentPool.ShieldBeltGunlink:
                    FillWithShieldBeltGunlink(map, shelf);
                    break;
                case ShelfContentPool.ChargeLances:
                    FillWithWeapons(map, shelf, Things.Gun_ChargeLance);
                    break;
            }
        }

        /// <summary>
        /// Fills shelf with mortar shells.
        /// First cell: 6-12 HE shells (guaranteed)
        /// Second cell: 25% chance antigrain (1-3), 75% chance random shells (8-14)
        /// </summary>
        private static void FillWithMortarShells(Map map, Building_Storage shelf)
        {
            // First cell: Guaranteed HE shells
            RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Shell_HighExplosive, Rand.RangeInclusive(6, 12));

            // Second cell: Random ordnance
            SpawnRandomOrdnance(map, shelf);
        }

        /// <summary>
        /// Spawns random mortar ordnance into the shelf.
        /// 25% chance: 1-3 antigrain warhead shells
        /// 75% chance: 8-14 random mortar shells (excluding antigrain)
        /// </summary>
        private static void SpawnRandomOrdnance(Map map, Building_Storage shelf)
        {
            if (Rand.Chance(0.25f))
            {
                // 25% chance: Antigrain warheads (rare, powerful)
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Shell_AntigrainWarhead, Rand.RangeInclusive(1, 3));
                return;
            }

            // 75% chance (or fallback): Random mortar shell
            SpawnRandomNonAntigrainShell(map, shelf);
        }

        /// <summary>
        /// Spawns 8-14 random mortar shells (excluding antigrain warheads).
        /// </summary>
        private static void SpawnRandomNonAntigrainShell(Map map, Building_Storage shelf)
        {
            List<ThingDef> mortarShells = GetAllMortarShellTypes(excludeAntigrain: true);

            if (mortarShells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No mortar shell types found");
                return;
            }

            ThingDef shellDef = mortarShells.RandomElement();
            Thing ordnance = ThingMaker.MakeThing(shellDef);
            ordnance.stackCount = Rand.RangeInclusive(8, 14);

            if (!RoomShelfHelper.AddItemToShelf(map, shelf, ordnance))
            {
                ordnance.Destroy(DestroyMode.Vanish);
            }
        }

        /// <summary>
        /// Discovers all mortar shell types at runtime for mod compatibility.
        /// </summary>
        private static List<ThingDef> GetAllMortarShellTypes(bool excludeAntigrain)
        {
            ThingDef antigrainDef = excludeAntigrain ? Things.Shell_AntigrainWarhead : null;

            List<ThingDef> shells = new List<ThingDef>();

            if (ThingCategories.MortarShells != null)
            {
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.thingCategories != null && def.thingCategories.Contains(ThingCategories.MortarShells))
                    {
                        if (excludeAntigrain && def == antigrainDef)
                            continue;
                        shells.Add(def);
                    }
                }
            }

            // Fallback: defName pattern
            if (shells.Count == 0)
            {
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.defName.StartsWith("Shell_") && def.category == ThingCategory.Item)
                    {
                        if (excludeAntigrain && def == antigrainDef)
                            continue;
                        shells.Add(def);
                    }
                }
            }

            return shells;
        }

        /// <summary>
        /// Fills shelf with weapons (charge rifles or charge lances).
        /// Uses quality distribution: Normal 70%, Good 50%, Excellent 30%.
        /// Respects shelf capacity (maxItemsInCell) via helper.
        /// </summary>
        private static void FillWithWeapons(Map map, Building_Storage shelf, ThingDef weaponDef)
        {
            if (weaponDef == null)
            {
                Log.Warning("[Better Traders Guild] FillWithWeapons called with null weaponDef");
                return;
            }

            // Quality distribution - each roll is independent
            // Helper will place in first available cell, respecting maxItemsInCell
            if (Rand.Chance(0.70f))
            {
                SpawnWeaponWithQuality(map, shelf, weaponDef, QualityCategory.Normal);
            }
            if (Rand.Chance(0.50f))
            {
                SpawnWeaponWithQuality(map, shelf, weaponDef, QualityCategory.Good);
            }
            if (Rand.Chance(0.30f))
            {
                SpawnWeaponWithQuality(map, shelf, weaponDef, QualityCategory.Excellent);
            }
        }

        private static void SpawnWeaponWithQuality(Map map, Building_Storage shelf, ThingDef weaponDef, QualityCategory quality)
        {
            Thing weapon = ThingMaker.MakeThing(weaponDef);

            CompQuality compQuality = weapon.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
            }

            if (!RoomShelfHelper.AddItemToShelf(map, shelf, weapon))
            {
                weapon.Destroy(DestroyMode.Vanish);
            }
        }

        /// <summary>
        /// Fills shelf with shield belt and gunlink combo.
        /// 1-2 shield belts (Normal-Excellent quality)
        /// 1-2 gunlinks (Normal-Excellent quality)
        /// </summary>
        private static void FillWithShieldBeltGunlink(Map map, Building_Storage shelf)
        {
            // Shield belts
            if (Things.Apparel_ShieldBelt != null)
            {
                int shieldCount = Rand.RangeInclusive(1, 2);
                for (int i = 0; i < shieldCount; i++)
                {
                    QualityCategory quality = GetRandomQuality();
                    SpawnApparelWithQuality(map, shelf, Things.Apparel_ShieldBelt, quality);
                }
            }

            // Gunlinks
            if (Things.Apparel_Gunlink != null)
            {
                int gunlinkCount = Rand.RangeInclusive(1, 2);
                for (int i = 0; i < gunlinkCount; i++)
                {
                    QualityCategory quality = GetRandomQuality();
                    SpawnApparelWithQuality(map, shelf, Things.Apparel_Gunlink, quality);
                }
            }
        }

        /// <summary>
        /// Returns a random quality weighted toward Normal/Good.
        /// </summary>
        private static QualityCategory GetRandomQuality()
        {
            float roll = Rand.Value;
            if (roll < 0.50f)
                return QualityCategory.Normal;
            if (roll < 0.85f)
                return QualityCategory.Good;
            return QualityCategory.Excellent;
        }

        private static void SpawnApparelWithQuality(Map map, Building_Storage shelf, ThingDef apparelDef, QualityCategory quality)
        {
            Thing apparel = ThingMaker.MakeThing(apparelDef);

            CompQuality compQuality = apparel.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
            }

            if (!RoomShelfHelper.AddItemToShelf(map, shelf, apparel))
            {
                apparel.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
