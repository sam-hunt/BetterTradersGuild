using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Armory
{
    /// <summary>
    /// Custom RoomContentsWorker for Armory.
    ///
    /// Post-processes spawned prefabs:
    /// 1. Fills steel shelves (BTG_SteelShelf_Edge) with random content from pools:
    ///    - Mortar shells (25% antigrain, 75% random shells)
    ///    - Charge rifles (Normal/Good/Excellent quality distribution)
    ///    - Shield belt + gunlink combo
    ///    - Charge lances (Normal/Good/Excellent quality distribution)
    /// 2. Paints outfit stands with BTG_OrbitalSteel color
    /// 3. Spawns marine armor sets in outfit stands
    /// </summary>
    public class RoomContents_Armory : RoomContentsWorker
    {

        /// <summary>
        /// Content pool options for weapon shelves.
        /// </summary>
        private enum ShelfContentPool
        {
            MortarShells,
            ChargeRifles,
            ShieldBeltGunlink,
            ChargeLances
        }

        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs, then post-processes:
        /// - Fills weapon shelves with random content from pools
        /// - Paints outfit stands with orbital steel color
        /// - Spawns marine armor sets in outfit stands
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (empty shelves, crates, lockers)
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Post-process spawned prefabs
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();

                // Fill weapon shelves with random content
                FillWeaponShelves(map, roomRect);

                // Paint outfit stands with orbital steel color
                PaintOutfitStands(map, roomRect);

                // Spawn marine armor sets in outfit stands
                SpawnMarineArmorInOutfitStands(map, roomRect);
            }
        }

        /// <summary>
        /// Finds all 2-cell wide shelves in the room and fills them with random content.
        /// Each shelf gets content from a randomly selected pool.
        /// </summary>
        private void FillWeaponShelves(Map map, CellRect roomRect)
        {
            List<Building_Storage> weaponShelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            // Fill each weapon shelf with random content
            foreach (Building_Storage shelf in weaponShelves)
            {
                ShelfContentPool pool = GetRandomContentPool();
                FillShelfWithContent(map, shelf, pool);
            }
        }

        /// <summary>
        /// Selects a random content pool for a shelf.
        /// </summary>
        private ShelfContentPool GetRandomContentPool()
        {
            // Equal weights for all pools
            return (ShelfContentPool)Rand.Range(0, 4);
        }

        /// <summary>
        /// Fills a shelf with content from the specified pool.
        /// </summary>
        private void FillShelfWithContent(Map map, Building_Storage shelf, ShelfContentPool pool)
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
        private void FillWithMortarShells(Map map, Building_Storage shelf)
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
        private void SpawnRandomOrdnance(Map map, Building_Storage shelf)
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
        private void SpawnRandomNonAntigrainShell(Map map, Building_Storage shelf)
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
        private List<ThingDef> GetAllMortarShellTypes(bool excludeAntigrain)
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
        /// Now respects shelf capacity (maxItemsInCell) via helper.
        /// </summary>
        private void FillWithWeapons(Map map, Building_Storage shelf, ThingDef weaponDef)
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

        /// <summary>
        /// Spawns a weapon with the specified quality into the shelf.
        /// Uses helper to respect maxItemsInCell limit.
        /// </summary>
        private void SpawnWeaponWithQuality(Map map, Building_Storage shelf, ThingDef weaponDef, QualityCategory quality)
        {
            Thing weapon = ThingMaker.MakeThing(weaponDef);

            CompQuality compQuality = weapon.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
            }

            // Use helper - respects maxItemsInCell
            if (!RoomShelfHelper.AddItemToShelf(map, shelf, weapon))
            {
                weapon.Destroy(DestroyMode.Vanish); // Clean up if no space
            }
        }

        /// <summary>
        /// Fills shelf with shield belt and gunlink combo.
        /// 1-2 shield belts (Normal-Excellent quality)
        /// 1-2 gunlinks (Normal-Excellent quality)
        /// </summary>
        private void FillWithShieldBeltGunlink(Map map, Building_Storage shelf)
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
        private QualityCategory GetRandomQuality()
        {
            float roll = Rand.Value;
            if (roll < 0.50f)
                return QualityCategory.Normal;
            if (roll < 0.85f)
                return QualityCategory.Good;
            return QualityCategory.Excellent;
        }

        /// <summary>
        /// Spawns apparel with the specified quality into the shelf.
        /// Uses helper to respect maxItemsInCell limit.
        /// </summary>
        private void SpawnApparelWithQuality(Map map, Building_Storage shelf, ThingDef apparelDef, QualityCategory quality)
        {
            Thing apparel = ThingMaker.MakeThing(apparelDef);

            CompQuality compQuality = apparel.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(quality, ArtGenerationContext.Outsider);
            }

            // Use helper - respects maxItemsInCell
            if (!RoomShelfHelper.AddItemToShelf(map, shelf, apparel))
            {
                apparel.Destroy(DestroyMode.Vanish); // Clean up if no space
            }
        }

        /// <summary>
        /// Finds all outfit stands in the room and paints them with BTG_OrbitalSteel color.
        /// </summary>
        private void PaintOutfitStands(Map map, CellRect roomRect)
        {
            if (Things.Building_OutfitStand == null)
            {
                return; // OutfitStand not available (missing DLC?)
            }

            if (Colors.BTG_OrbitalSteel == null)
            {
                Log.Warning("[Better Traders Guild] Colors.BTG_OrbitalSteel is null");
                return;
            }

            // Find outfit stands and paint them using helper
            List<Thing> outfitStands = roomRect.Cells
                .Where(c => c.InBounds(map))
                .SelectMany(c => c.GetThingList(map))
                .Where(t => t.def == Things.Building_OutfitStand)
                .Distinct()
                .ToList();

            foreach (Thing stand in outfitStands)
            {
                PaintableFurnitureHelper.TryPaint(stand, Colors.BTG_OrbitalSteel);
            }
        }

        /// <summary>
        /// Spawns marine armor and helmet into outfit stands.
        /// </summary>
        private void SpawnMarineArmorInOutfitStands(Map map, CellRect roomRect)
        {
            List<ThingDef> marineArmorSet = new List<ThingDef>();

            if (Things.Apparel_PowerArmor != null)
            {
                marineArmorSet.Add(Things.Apparel_PowerArmor);
            }
            else
            {
                Log.Warning("[Better Traders Guild] Things.Apparel_PowerArmor is null");
            }

            if (Things.Apparel_PowerArmorHelmet != null)
            {
                marineArmorSet.Add(Things.Apparel_PowerArmorHelmet);
            }
            else
            {
                Log.Warning("[Better Traders Guild] Things.Apparel_PowerArmorHelmet is null");
            }

            if (marineArmorSet.Count == 0)
            {
                return;
            }

            RoomOutfitStandHelper.SpawnApparelInOutfitStands(
                map,
                roomRect,
                marineArmorSet,
                QualityCategory.Normal,
                QualityCategory.Excellent);
        }
    }
}
