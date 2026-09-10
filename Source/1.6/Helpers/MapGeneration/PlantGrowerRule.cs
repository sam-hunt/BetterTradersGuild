using System.Collections.Generic;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    // One XML-authored planting rule: which growers it covers, what they may grow, and how
    // grown the plant spawns. Rules are consumed in order by PlantGrowerRuleResolver; a
    // grower takes the first rule that matches it, so list specific rules (basins) before
    // catch-alls (no grower filter).
    //
    // XML fields:
    // - grower: the Building_PlantGrower ThingDef this rule covers (omit = any grower)
    // - plants: explicit candidate plant ThingDefs
    // - sowTag: adds every sowable plant carrying this sow tag (e.g. Decorative) to the
    //   candidates, so mod-added flowers join automatically; may be combined with plants
    // - growthRange: growth of the spawned plant (0.0-1.0); default fully grown
    // - perGrower: true = roll plant and growth independently per grower;
    //   false (default) = one roll shared by every grower the rule covers, and only
    //   plants every one of them can grow are eligible
    public class PlantGrowerRule
    {
        public ThingDef grower;
        public List<ThingDef> plants;
        public string sowTag;
        public FloatRange growthRange = FloatRange.One;
        public bool perGrower;
    }

    // DefModExtension carrying planting rules for a LayoutRoomDef: GenStep_SowPlantGrowers
    // applies them to the growers inside each room the def built, before its own map-wide
    // defaults reach anything left over. Room-specific plant choices (healroot in the
    // medbay, roses for the commander) live here instead of in RoomContentsWorker code.
    public class PlantGrowerRulesExtension : DefModExtension
    {
        public List<PlantGrowerRule> rules;
    }
}
