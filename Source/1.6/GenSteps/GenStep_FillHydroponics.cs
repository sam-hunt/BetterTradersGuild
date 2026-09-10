using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that sows every empty hydroponics basin on the map with a crop drawn at
    // random (per basin) from an XML list, at a random growth in an XML range.
    //
    // XML-configurable parameters:
    // - plants: hydroponic-compatible crop ThingDefs to pick from per basin
    // - growthRange: growth rolled per basin (0.0-1.0)
    //
    // The map-wide default for stations whose rooms BTG did not author (VGE2's Traders
    // Guild station prefabs place basins but leave them empty). BTG's own rooms keep
    // their room-specific choices in their RoomContentsWorkers (healroot in the medbay,
    // rice in the greenhouse); because the helper skips basins that already hold a plant,
    // this step is safe to add to those pipelines too as a catch-all. Being a GenStepDef,
    // both the crop list and the step's presence in a pipeline are XML-patchable.
    //
    // Basins need power or their plants rot; GenStep_PlaceWallConduits runs first in the
    // pipelines that use this step so the station's LifeSupportUnits reach them.
    public class GenStep_FillHydroponics : GenStep
    {
        public List<ThingDef> plants;
        public FloatRange growthRange = new FloatRange(0.1f, 0.9f);

        public override int SeedPart => 1287744061;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null || Things.HydroponicsBasin == null)
                return;

            List<ThingDef> crops = new List<ThingDef>();
            if (plants != null)
            {
                foreach (ThingDef plant in plants)
                {
                    if (plant == null)
                        continue;
                    if (!RoomPlantHelper.IsHydroponicCompatible(plant))
                    {
                        Log.Warning($"[Better Traders Guild] GenStep_FillHydroponics: '{plant.defName}' lacks the Hydroponic sow tag and is skipped.");
                        continue;
                    }
                    crops.Add(plant);
                }
            }

            if (crops.Count == 0)
            {
                Log.Warning("[Better Traders Guild] GenStep_FillHydroponics has no hydroponic-compatible plants configured; nothing sown.");
                return;
            }

            // Copy: spawning plants does not touch this lister, but iterate a snapshot anyway.
            List<Thing> basins = new List<Thing>(map.listerThings.ThingsOfDef(Things.HydroponicsBasin));
            foreach (Thing thing in basins)
            {
                if (thing is not Building_PlantGrower basin)
                    continue;

                RoomPlantHelper.TrySpawnPlantInGrower(map, basin, crops.RandomElement(), growthRange.RandomInRange);
            }
        }
    }
}
