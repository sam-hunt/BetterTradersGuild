using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Armory
{
    /// <summary>
    /// Fills armory shelves with randomized military equipment.
    /// Each shelf gets content from a randomly selected pool.
    /// Pools are discovered dynamically at startup and cached.
    /// Per cell slot, a random ThingDef is selected and its count is determined
    /// by a value-based algorithm (soft-max between 150-250 market value).
    /// </summary>
    public static class ArmoryShelfFiller
    {
        private static List<List<ThingDef>> pools;
        private static bool initialized;

        public static void FillWeaponShelves(Map map, CellRect roomRect)
        {
            EnsureInitialized();

            if (pools.Count == 0)
            {
                Log.Warning("[Better Traders Guild] ArmoryShelfFiller: No item pools available");
                return;
            }

            List<Building_Storage> weaponShelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            // Copy pool list so we can remove categories as they're assigned,
            // ensuring no two shelves in the same room share a category.
            var availablePools = new List<List<ThingDef>>(pools);

            foreach (Building_Storage shelf in weaponShelves)
            {
                if (availablePools.Count == 0)
                    availablePools.AddRange(pools);

                List<ThingDef> pool = availablePools.RandomElement();
                availablePools.Remove(pool);
                FillShelf(map, shelf, pool);
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized) return;

            pools = new List<List<ThingDef>>();
            AddPoolIfNonEmpty(DiscoverBaseWeaponsByCategory(WeaponCategories.PulseCharge));
            AddPoolIfNonEmpty(DiscoverBaseWeaponsByCategory(WeaponCategories.BeamWeapon));
            AddPoolIfNonEmpty(DiscoverMortarShells());
            AddPoolIfNonEmpty(DiscoverUtilityItems());
            AddPoolIfNonEmpty(DiscoverCombatDrugs());
            initialized = true;
        }

        private static void AddPoolIfNonEmpty(List<ThingDef> pool)
        {
            if (pool.Count > 0)
                pools.Add(pool);
        }

        private static void FillShelf(Map map, Building_Storage shelf, List<ThingDef> pool)
        {
            int slotCount = shelf.AllSlotCellsList().Count;
            for (int i = 0; i < slotCount; i++)
            {
                ThingDef def = pool.RandomElement();
                int count = DetermineCount(def);
                Thing item = CreateItem(def, count);

                if (!RoomShelfHelper.AddItemToShelf(map, shelf, item))
                {
                    item.Destroy(DestroyMode.Vanish);
                }
            }
        }

        /// <summary>
        /// Determines item count using a value-based algorithm.
        /// Generates a random soft-max between 250-350, then increments count
        /// until total value exceeds the soft-max. Naturally produces high counts
        /// for cheap items (shells) and low counts for expensive items (weapons).
        /// </summary>
        private static int DetermineCount(ThingDef def)
        {
            float softMax = Rand.Range(250f, 350f);
            float marketValue = def.BaseMarketValue;

            // Safety: avoid infinite loop for zero-value items
            if (marketValue <= 0f)
                return 1;

            int count = 0;
            while (count * marketValue <= softMax)
                count++;

            return System.Math.Min(count, def.stackLimit);
        }

        private static Thing CreateItem(ThingDef def, int count)
        {
            Thing item = ThingMaker.MakeThing(def);
            item.stackCount = count;

            CompQuality compQuality = item.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(GetRandomQuality(), ArtGenerationContext.Outsider);
            }

            return item;
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

        /// <summary>
        /// Discovers non-unique base weapons by finding unique weapons with the given
        /// WeaponCategoryDef and resolving their base weapon via descriptionHyperlinks.
        /// </summary>
        private static List<ThingDef> DiscoverBaseWeaponsByCategory(WeaponCategoryDef category)
        {
            var weapons = new List<ThingDef>();
            if (category == null) return weapons;

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.comps == null) continue;

                var props = def.comps.OfType<CompProperties_UniqueWeapon>().FirstOrDefault();
                if (props?.weaponCategories == null || !props.weaponCategories.Contains(category))
                    continue;

                // Resolve the base (non-unique) weapon via descriptionHyperlinks
                if (def.descriptionHyperlinks == null) continue;
                foreach (DefHyperlink hyperlink in def.descriptionHyperlinks)
                {
                    if (hyperlink.def is ThingDef baseDef && baseDef != def)
                    {
                        weapons.Add(baseDef);
                    }
                }
            }

            if (weapons.Count == 0)
                Log.Warning($"[Better Traders Guild] ArmoryShelfFiller: No base weapons found for category {category.defName}");

            return weapons;
        }

        /// <summary>
        /// Discovers all mortar shell types via ThingCategoryDef,
        /// with defName prefix fallback for mod compatibility.
        /// </summary>
        private static List<ThingDef> DiscoverMortarShells()
        {
            var shells = new List<ThingDef>();

            if (ThingCategories.MortarShells != null)
            {
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.thingCategories != null && def.thingCategories.Contains(ThingCategories.MortarShells))
                        shells.Add(def);
                }
            }

            // Fallback: defName pattern
            if (shells.Count == 0)
            {
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.defName.StartsWith("Shell_") && def.category == ThingCategory.Item)
                        shells.Add(def);
                }
            }

            return shells;
        }

        private static List<ThingDef> DiscoverUtilityItems()
        {
            var items = new List<ThingDef>();
            if (Things.Apparel_SmokepopBelt != null) items.Add(Things.Apparel_SmokepopBelt);
            if (Things.Apparel_ShieldBelt != null) items.Add(Things.Apparel_ShieldBelt);
            if (Things.Apparel_Gunlink != null) items.Add(Things.Apparel_Gunlink);
            return items;
        }

        /// <summary>
        /// Discovers all drugs marked as combat-enhancing via CompProperties_Drug.isCombatEnhancingDrug.
        /// In vanilla, this includes Go-juice and Yayo.
        /// </summary>
        private static List<ThingDef> DiscoverCombatDrugs()
        {
            var drugs = new List<ThingDef>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.comps == null) continue;

                var drugProps = def.comps.OfType<CompProperties_Drug>().FirstOrDefault();
                if (drugProps != null && drugProps.isCombatEnhancingDrug)
                    drugs.Add(def);
            }

            if (drugs.Count == 0)
                Log.Warning("[Better Traders Guild] ArmoryShelfFiller: No combat-enhancing drugs found");

            return drugs;
        }
    }
}
