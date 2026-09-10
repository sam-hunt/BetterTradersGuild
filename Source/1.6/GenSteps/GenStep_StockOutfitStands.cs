using System.Collections.Generic;
using BetterTradersGuild.Helpers.MapGeneration;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that stocks every empty outfit stand on the map from XML stocking rules, in
    // two passes:
    //
    // 1. Room rules: for each LayoutRoom the platform step built (map.layoutStructureSketches),
    //    the OutfitStandRuleExtension on its LayoutRoomDefs decides what the stands inside
    //    its rects display. This is where room identity lives (marine armour in the armory,
    //    vacsuits at the corridor airlocks, a varied wardrobe in the crew quarters).
    // 2. Map default: this def's own rule covers every stand no room rule reached, which is
    //    all of them on prefab-built stations BTG did not author (VGE2's Traders Guild
    //    station places steel stands but leaves them empty) and any stray stand on BTG maps.
    //
    // Stands that already hold anything are skipped, so the step layers safely over any
    // earlier stocking. Every empty stand is normalised for HAR first (OutfitStandHarFixer)
    // whether or not it ends up stocked, so a stand the player later dresses accepts adult
    // apparel. Apparel is tinted in the map owner's faction colour when VEF is active. See
    // OutfitStandRule for the rule fields.
    public class GenStep_StockOutfitStands : GenStep
    {
        public OutfitStandRule rule;

        public override int SeedPart => 913350217;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null)
                return;

            List<Building_OutfitStand> empty = EmptyStands(map);
            if (empty.Count == 0)
                return;

            foreach (Building_OutfitStand stand in empty)
                OutfitStandHarFixer.NormalizeOutfitStand(stand);

            Faction faction = map.ParentFaction;
            StockRooms(map, empty, faction);
            Stock(OutfitStandRuleResolver.PlanForStands(rule, empty), empty, faction);
        }

        // Applies each built room's LayoutRoomDef rule to the empty stands inside it, removing
        // what it stocks from the pool. The room's stands are all consumed by the room rule
        // (stocked or left bare by its chance roll), so the map default never dresses a stand a
        // room rule chose to leave empty.
        private static void StockRooms(Map map, List<Building_OutfitStand> empty, Faction faction)
        {
            BuiltRoomRules.ForEach<OutfitStandRuleExtension>(map, (room, extensions, label) =>
            {
                OutfitStandRule roomRule = null;
                foreach (OutfitStandRuleExtension ext in extensions)
                {
                    if (ext.rule != null)
                    {
                        roomRule = ext.rule;
                        break;
                    }
                }
                if (roomRule == null)
                    return;

                List<Building_OutfitStand> inRoom = BuiltRoomRules.Inside(room, empty);
                if (inRoom.Count == 0)
                    return;

                Stock(OutfitStandRuleResolver.PlanForStands(roomRule, inRoom), empty, faction, roomRule);
                foreach (Building_OutfitStand stand in inRoom)
                    empty.Remove(stand);
            });
        }

        private void Stock(List<OutfitStandRuleResolver.Assignment<Building_OutfitStand, OutfitStandApparelSet>> plan, List<Building_OutfitStand> empty, Faction faction)
        {
            Stock(plan, empty, faction, rule);
        }

        private static void Stock(List<OutfitStandRuleResolver.Assignment<Building_OutfitStand, OutfitStandApparelSet>> plan, List<Building_OutfitStand> empty, Faction faction, OutfitStandRule source)
        {
            foreach (OutfitStandRuleResolver.Assignment<Building_OutfitStand, OutfitStandApparelSet> a in plan)
            {
                RoomOutfitStandHelper.FillOutfitStand(a.Stand, a.Set.apparel, source.minQuality, source.maxQuality, faction, a.Set.stuff);
                empty.Remove(a.Stand);
            }
        }

        // Every outfit stand on the map holding nothing.
        private static List<Building_OutfitStand> EmptyStands(Map map)
        {
            var result = new List<Building_OutfitStand>();
            foreach (Building_OutfitStand stand in map.listerThings.GetThingsOfType<Building_OutfitStand>())
            {
                if (stand.HeldItems.Count == 0)
                    result.Add(stand);
            }
            return result;
        }
    }
}
