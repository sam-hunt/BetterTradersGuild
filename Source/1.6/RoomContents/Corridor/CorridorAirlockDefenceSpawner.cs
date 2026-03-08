using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    /// <summary>
    /// Finds AncientBlastDoors on corridor rect edges, replaces each with an
    /// airlock defense prefab (airlock + autocannons), and populates the
    /// prefab's outfit stands with vacsuits.
    ///
    /// Must run BEFORE base room content placement so the airlock space is
    /// occupied and unavailable for other prefab placement.
    /// </summary>
    public static class CorridorAirlockDefenceSpawner
    {
        private const float PopulateChancePerStand = 0.5f;

        public static void SpawnAirlockDefences(Map map, LayoutRoom room, Faction faction)
        {
            PrefabDef prefab = Prefabs.BTG_AirlockDefences;
            if (prefab == null || room.rects == null)
                return;

            HashSet<IntVec3> processed = new HashSet<IntVec3>();

            foreach (CellRect rect in room.rects)
            {
                foreach (IntVec3 cell in rect.EdgeCells)
                {
                    if (!cell.InBounds(map) || processed.Contains(cell))
                        continue;

                    Building edifice = cell.GetEdifice(map);
                    if (edifice == null || edifice.def != Things.AncientBlastDoor)
                        continue;

                    Rot4? rotation = GetEdgeRotation(cell, rect);
                    if (!rotation.HasValue)
                        continue;

                    processed.Add(cell);
                    edifice.Destroy();
                    PrefabUtility.SpawnPrefab(prefab, map, cell, rotation.Value, faction);
                }
            }

            PopulateVacsuitStands(map, room, faction);
        }

        /// <summary>
        /// Returns the prefab rotation based on which edge of the rect the cell is on.
        /// Returns null for corner cells (ambiguous edge).
        /// </summary>
        private static Rot4? GetEdgeRotation(IntVec3 cell, CellRect rect)
        {
            bool onNorth = cell.z == rect.maxZ;
            bool onSouth = cell.z == rect.minZ;
            bool onEast = cell.x == rect.maxX;
            bool onWest = cell.x == rect.minX;

            int edgeCount = (onNorth ? 1 : 0) + (onSouth ? 1 : 0) + (onEast ? 1 : 0) + (onWest ? 1 : 0);
            if (edgeCount != 1)
                return null;

            if (onNorth) return Rot4.North;
            if (onSouth) return Rot4.South;
            if (onEast) return Rot4.East;
            if (onWest) return Rot4.West;

            return null;
        }

        /// <summary>
        /// Populates outfit stands placed by the airlock defense prefab with vacsuits.
        /// Each stand has a 50% chance of being populated with a vacsuit and helmet.
        /// </summary>
        private static void PopulateVacsuitStands(Map map, LayoutRoom room, Faction faction)
        {
            List<ThingDef> vacsuitSet = new List<ThingDef>();
            if (Things.Apparel_Vacsuit != null) vacsuitSet.Add(Things.Apparel_Vacsuit);
            if (Things.Apparel_VacsuitHelmet != null) vacsuitSet.Add(Things.Apparel_VacsuitHelmet);
            if (vacsuitSet.Count == 0) return;

            HashSet<Building_OutfitStand> stands = new HashSet<Building_OutfitStand>();

            foreach (CellRect rect in room.rects)
            {
                foreach (IntVec3 cell in rect.Cells)
                {
                    if (!cell.InBounds(map)) continue;

                    foreach (Thing thing in cell.GetThingList(map))
                    {
                        if (thing is Building_OutfitStand stand)
                            stands.Add(stand);
                    }
                }
            }

            foreach (Building_OutfitStand stand in stands)
            {
                if (!Rand.Chance(PopulateChancePerStand))
                    continue;

                foreach (ThingDef apparelDef in vacsuitSet)
                {
                    ThingDef stuffDef = apparelDef.MadeFromStuff ? GenStuff.DefaultStuffFor(apparelDef) : null;
                    Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuffDef);

                    CompQuality comp = apparel.TryGetComp<CompQuality>();
                    if (comp != null)
                    {
                        QualityCategory quality = Rand.Chance(0.6f) ? QualityCategory.Normal : QualityCategory.Good;
                        comp.SetQuality(quality, ArtGenerationContext.Outsider);
                    }

                    ApparelFactionColorHelper.TryApplyFactionColor(apparel, faction);

                    if (!stand.AddApparel(apparel))
                        apparel.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }
}
