using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.Helpers;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Armory.
    ///
    /// Post-processes spawned prefabs:
    /// 1. Replaces single HE shell markers with random ordnance:
    ///    - 25% chance: 1~3 antigrain warhead shells (rare, powerful)
    ///    - 75% chance: 8~14 random mortar shells (including mod-added types)
    /// 2. Paints outfit stands with BTG_OrbitalSteel color for consistent aesthetic
    ///
    /// This approach allows mod compatibility by discovering all available
    /// mortar shell types at runtime rather than hardcoding vanilla types.
    /// </summary>
    public class RoomContents_Armory : RoomContentsWorker
    {
        // Shell type constants
        private const string HIGH_EXPLOSIVE_DEFNAME = "Shell_HighExplosive";
        private const string ANTIGRAIN_DEFNAME = "Shell_AntigrainWarhead";

        // Furniture constants
        private const string OUTFIT_STAND_DEFNAME = "OutfitStand";
        private const string ORBITAL_STEEL_COLOR_DEFNAME = "BTG_OrbitalSteel";

        // Marine armor constants
        private const string MARINE_ARMOR_DEFNAME = "Apparel_PowerArmor";
        private const string MARINE_HELMET_DEFNAME = "Apparel_PowerArmorHelmet";

        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs, then post-processes:
        /// - Replaces single HE shell markers with random ordnance variety
        /// - Paints outfit stands with orbital steel color
        /// - Spawns marine armor sets in outfit stands
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (shelves, crates, lockers, shell markers)
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Post-process spawned prefabs
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();

                // Replace single HE shell markers with random ordnance
                ReplaceMortarShellMarkers(map, roomRect);

                // Paint outfit stands with orbital steel color
                PaintOutfitStands(map, roomRect);

                // Spawn marine armor sets in outfit stands
                SpawnMarineArmorInOutfitStands(map, roomRect);
            }
        }

        /// <summary>
        /// Finds all cells containing exactly 1 High Explosive shell and replaces
        /// them with random mortar ordnance.
        /// </summary>
        private void ReplaceMortarShellMarkers(Map map, CellRect roomRect)
        {
            ThingDef heShellDef = DefDatabase<ThingDef>.GetNamed(HIGH_EXPLOSIVE_DEFNAME, false);
            if (heShellDef == null)
            {
                Log.Warning("[Better Traders Guild] Could not find Shell_HighExplosive def");
                return;
            }

            // Collect marker cells and their shells (can't modify during iteration)
            List<(IntVec3 cell, Thing shell)> markersToReplace = new List<(IntVec3, Thing)>();

            foreach (IntVec3 cell in roomRect.Cells)
            {
                if (!cell.InBounds(map)) continue;

                List<Thing> things = cell.GetThingList(map);
                if (things == null) continue;

                // Find HE shells with exactly 1 stack count (our markers)
                foreach (Thing thing in things)
                {
                    if (thing.def == heShellDef && thing.stackCount == 1)
                    {
                        markersToReplace.Add((cell, thing));
                        break; // Only one marker per cell
                    }
                }
            }

            // Replace each marker with random ordnance
            foreach (var (cell, markerShell) in markersToReplace)
            {
                // Unspawn the marker shell
                markerShell.DeSpawn();

                // Spawn replacement ordnance
                SpawnRandomOrdnance(map, cell);
            }
        }

        /// <summary>
        /// Spawns random mortar ordnance at the specified cell.
        /// 25% chance: 1~3 antigrain warhead shells
        /// 75% chance: 8~14 random mortar shells (excluding antigrain)
        /// </summary>
        private void SpawnRandomOrdnance(Map map, IntVec3 cell)
        {
            ThingDef shellDef;
            int stackCount;

            if (Rand.Chance(0.25f))
            {
                // 25% chance: Antigrain warheads (rare, powerful)
                shellDef = DefDatabase<ThingDef>.GetNamed(ANTIGRAIN_DEFNAME, false);
                stackCount = Rand.RangeInclusive(1, 3);

                if (shellDef == null)
                {
                    Log.Warning("[Better Traders Guild] Could not find Shell_AntigrainWarhead def, falling back to random shell");
                    SpawnRandomNonAntigrainShell(map, cell);
                    return;
                }
            }
            else
            {
                // 75% chance: Random mortar shell (excluding antigrain)
                SpawnRandomNonAntigrainShell(map, cell);
                return;
            }

            // Spawn the antigrain shells
            Thing ordnance = ThingMaker.MakeThing(shellDef);
            ordnance.stackCount = stackCount;
            GenSpawn.Spawn(ordnance, cell, map);
            ordnance.SetForbidden(true, false);
        }

        /// <summary>
        /// Spawns 8~14 random mortar shells (excluding antigrain warheads).
        /// Discovers all mortar shell types at runtime for mod compatibility.
        /// </summary>
        private void SpawnRandomNonAntigrainShell(Map map, IntVec3 cell)
        {
            // Get all mortar shell types (excluding antigrain)
            List<ThingDef> mortarShells = GetAllMortarShellTypes(excludeAntigrain: true);

            if (mortarShells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No mortar shell types found");
                return;
            }

            // Select random shell type
            ThingDef shellDef = mortarShells.RandomElement();
            int stackCount = Rand.RangeInclusive(8, 14);

            Thing ordnance = ThingMaker.MakeThing(shellDef);
            ordnance.stackCount = stackCount;
            GenSpawn.Spawn(ordnance, cell, map);
            ordnance.SetForbidden(true, false);
        }

        /// <summary>
        /// Discovers all mortar shell types at runtime.
        /// Looks for ThingDefs that are in the mortar shell category or have
        /// the Shell_ prefix and are projectile-related.
        ///
        /// LEARNING NOTE: This approach finds both vanilla shells and any
        /// mod-added shell types, making the armory contents dynamic and
        /// mod-compatible without hardcoding specific defNames.
        /// </summary>
        private List<ThingDef> GetAllMortarShellTypes(bool excludeAntigrain)
        {
            ThingDef antigrainDef = excludeAntigrain
                ? DefDatabase<ThingDef>.GetNamed(ANTIGRAIN_DEFNAME, false)
                : null;

            // Find all mortar shells by checking thingCategories
            // Mortar shells are typically in ThingCategoryDef "MortarShells"
            ThingCategoryDef mortarShellCategory = DefDatabase<ThingCategoryDef>.GetNamed("MortarShells", false);

            List<ThingDef> shells = new List<ThingDef>();

            if (mortarShellCategory != null)
            {
                // Use the category to find all shells (most reliable method)
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.thingCategories != null && def.thingCategories.Contains(mortarShellCategory))
                    {
                        if (excludeAntigrain && def == antigrainDef)
                            continue;

                        shells.Add(def);
                    }
                }
            }

            // Fallback: If category search found nothing, try defName pattern
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
        /// Finds all outfit stands in the room and paints them with BTG_OrbitalSteel color.
        /// Uses CompColorable API for furniture painting.
        /// </summary>
        private void PaintOutfitStands(Map map, CellRect roomRect)
        {
            ThingDef outfitStandDef = DefDatabase<ThingDef>.GetNamed(OUTFIT_STAND_DEFNAME, false);
            if (outfitStandDef == null)
            {
                return; // OutfitStand not available (missing DLC?)
            }

            ColorDef orbitalSteelColor = DefDatabase<ColorDef>.GetNamed(ORBITAL_STEEL_COLOR_DEFNAME, false);
            if (orbitalSteelColor == null)
            {
                Log.Warning("[Better Traders Guild] Could not find BTG_OrbitalSteel ColorDef");
                return;
            }

            foreach (IntVec3 cell in roomRect.Cells)
            {
                if (!cell.InBounds(map)) continue;

                List<Thing> things = cell.GetThingList(map);
                if (things == null) continue;

                foreach (Thing thing in things)
                {
                    if (thing.def == outfitStandDef)
                    {
                        // Use CompColorable to paint the outfit stand
                        CompColorable colorComp = thing.TryGetComp<CompColorable>();
                        if (colorComp != null)
                        {
                            colorComp.SetColor(orbitalSteelColor.color);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Spawns marine armor and helmet into outfit stands.
        /// Uses the RoomOutfitStandHelper for consistent behavior across rooms.
        /// Quality is randomized between Normal and Excellent.
        /// </summary>
        private void SpawnMarineArmorInOutfitStands(Map map, CellRect roomRect)
        {
            // Build list of apparel to spawn in each outfit stand
            List<ThingDef> marineArmorSet = new List<ThingDef>();

            ThingDef marineArmor = DefDatabase<ThingDef>.GetNamed(MARINE_ARMOR_DEFNAME, false);
            if (marineArmor != null)
            {
                marineArmorSet.Add(marineArmor);
            }
            else
            {
                Log.Warning($"[Better Traders Guild] Could not find {MARINE_ARMOR_DEFNAME} def");
            }

            ThingDef marineHelmet = DefDatabase<ThingDef>.GetNamed(MARINE_HELMET_DEFNAME, false);
            if (marineHelmet != null)
            {
                marineArmorSet.Add(marineHelmet);
            }
            else
            {
                Log.Warning($"[Better Traders Guild] Could not find {MARINE_HELMET_DEFNAME} def");
            }

            if (marineArmorSet.Count == 0)
            {
                return; // No armor defs found
            }

            // Spawn armor in outfit stands (Normal to Excellent quality)
            RoomOutfitStandHelper.SpawnApparelInOutfitStands(
                map,
                roomRect,
                marineArmorSet,
                QualityCategory.Normal,
                QualityCategory.Excellent);
        }
    }
}
