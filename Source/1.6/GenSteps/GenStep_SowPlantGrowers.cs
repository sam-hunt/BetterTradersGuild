using System.Collections.Generic;
using BetterTradersGuild.Helpers.MapGeneration;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that sows every empty plant grower on the map (hydroponics basins, plant
    // pots, modded growers) from XML planting rules, in two passes:
    //
    // 1. Room rules: for each LayoutRoom the platform step built (map.layoutStructureSketches),
    //    the PlantGrowerRulesExtension on its LayoutRoomDefs decides what the growers inside
    //    its rects grow. This is where room identity lives (healroot in the medbay, roses for
    //    the commander, one shared crop across a greenhouse's basins).
    // 2. Map defaults: this def's own rules cover every grower no room rule planted, which
    //    is all of them on prefab-built stations BTG did not author (VGE2's Traders Guild
    //    station places basins but leaves them empty) and any stray grower on BTG maps.
    //
    // Growers that already hold a plant are skipped, so the step layers safely over any
    // earlier sowing. Basins need power or their plants rot; pipelines that use this step
    // run their conduit step first. See PlantGrowerRule for the rule fields.
    public class GenStep_SowPlantGrowers : GenStep
    {
        public List<PlantGrowerRule> rules;

        public override int SeedPart => 1287744061;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null)
                return;

            List<Building_PlantGrower> empty = EmptyGrowers(map);
            if (empty.Count == 0)
                return;

            SowRooms(map, empty);
            Sow(map, PlantGrowerRuleResolver.PlanForGrowers(rules, empty, def?.defName ?? "GenStep_SowPlantGrowers"), empty);
        }

        // Applies each built room's LayoutRoomDef rules to the empty growers inside it,
        // removing what it plants from the pool.
        private static void SowRooms(Map map, List<Building_PlantGrower> empty)
        {
            if (map.layoutStructureSketches == null)
                return;

            var inRoom = new List<Building_PlantGrower>();
            var roomRules = new List<PlantGrowerRule>();
            foreach (LayoutStructureSketch sketch in map.layoutStructureSketches)
            {
                if (sketch?.structureLayout?.Rooms == null)
                    continue;

                foreach (LayoutRoom room in sketch.structureLayout.Rooms)
                {
                    if (room?.rects == null || room.defs == null)
                        continue;

                    roomRules.Clear();
                    string label = null;
                    foreach (LayoutRoomDef roomDef in room.defs)
                    {
                        PlantGrowerRulesExtension ext = roomDef?.GetModExtension<PlantGrowerRulesExtension>();
                        if (ext?.rules == null)
                            continue;
                        roomRules.AddRange(ext.rules);
                        label ??= roomDef.defName;
                    }
                    if (roomRules.Count == 0)
                        continue;

                    inRoom.Clear();
                    foreach (Building_PlantGrower grower in empty)
                    {
                        if (Contains(room.rects, grower.Position))
                            inRoom.Add(grower);
                    }
                    if (inRoom.Count == 0)
                        continue;

                    Sow(map, PlantGrowerRuleResolver.PlanForGrowers(roomRules, inRoom, label), empty);
                }
            }
        }

        private static void Sow(Map map, List<PlantGrowerRuleResolver.Assignment<Building_PlantGrower, ThingDef>> plan, List<Building_PlantGrower> empty)
        {
            foreach (PlantGrowerRuleResolver.Assignment<Building_PlantGrower, ThingDef> a in plan)
            {
                RoomPlantHelper.TrySpawnPlantInGrower(map, a.Grower, a.Plant, a.Growth);
                empty.Remove(a.Grower);
            }
        }

        // Every grower on the map with at least one plant-free cell.
        private static List<Building_PlantGrower> EmptyGrowers(Map map)
        {
            var result = new List<Building_PlantGrower>();
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (thing is not Building_PlantGrower grower)
                    continue;

                foreach (IntVec3 cell in grower.OccupiedRect())
                {
                    if (cell.InBounds(map) && cell.GetPlant(map) == null)
                    {
                        result.Add(grower);
                        break;
                    }
                }
            }
            return result;
        }

        private static bool Contains(List<CellRect> rects, IntVec3 cell)
        {
            foreach (CellRect rect in rects)
            {
                if (rect.Contains(cell))
                    return true;
            }
            return false;
        }
    }
}
