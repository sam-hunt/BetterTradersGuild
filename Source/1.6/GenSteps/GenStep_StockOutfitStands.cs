using System.Collections.Generic;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that stocks empty outfit stands across the map with an XML apparel set,
    // each stand independently rolling an XML chance.
    //
    // XML-configurable parameters:
    // - apparel: ThingDefs every stocked stand receives one of each
    // - chancePerStand: probability a given empty stand is stocked (default 1)
    // - minQuality / maxQuality: uniform quality roll per item
    //
    // The map-wide default for stations whose rooms BTG did not author (VGE2's Traders
    // Guild station prefabs place steel stands but leave them empty). BTG's own rooms keep
    // their room-specific stocking in their RoomContentsWorkers (marine armour in the
    // armory, vacsuits at airlocks); stands that already hold anything are left alone, so
    // this step is safe to add to those pipelines as a catch-all. Being a GenStepDef, the
    // set, the odds and the step's presence in a pipeline are all XML-patchable.
    //
    // Apparel is tinted in the map owner's faction colour when VEF is active, as in the
    // room workers.
    public class GenStep_StockOutfitStands : GenStep
    {
        public List<ThingDef> apparel;
        public float chancePerStand = 1f;
        public QualityCategory minQuality = QualityCategory.Normal;
        public QualityCategory maxQuality = QualityCategory.Excellent;

        public override int SeedPart => 913350217;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null || apparel == null || apparel.Count == 0)
                return;

            Faction faction = map.ParentFaction;

            // Snapshot: filling a stand does not change the lister, but iterate a copy anyway.
            List<Building_OutfitStand> stands = new List<Building_OutfitStand>(map.listerThings.GetThingsOfType<Building_OutfitStand>());
            foreach (Building_OutfitStand stand in stands)
            {
                if (stand.HeldItems.Count > 0)
                    continue;

                if (!Rand.Chance(chancePerStand))
                    continue;

                RoomOutfitStandHelper.FillOutfitStand(stand, apparel, minQuality, maxQuality, faction);
            }
        }
    }
}
