using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    // One outfit an outfit stand may display: the apparel defs it holds, one of each, and
    // an optional stuff every stuffable piece in the set is made from (omit = default stuff).
    public class OutfitStandApparelSet
    {
        public List<ThingDef> apparel;
        public ThingDef stuff;
    }

    // XML-authored stocking rule for outfit stands, consumed by GenStep_StockOutfitStands:
    // each empty stand the rule covers rolls chancePerStand, and the stocked ones receive
    // one of the sets.
    //
    // XML fields:
    // - sets: candidate outfits; a stand receives every item of exactly one set
    // - chancePerStand: probability a given empty stand is stocked at all (default 1)
    // - minQuality / maxQuality: uniform quality roll per item (default Normal..Excellent)
    // - perStand: true (default) = each stocked stand picks its own set;
    //   false = one set is rolled and shared by every stand the rule stocks
    public class OutfitStandRule
    {
        public List<OutfitStandApparelSet> sets;
        public float chancePerStand = 1f;
        public QualityCategory minQuality = QualityCategory.Normal;
        public QualityCategory maxQuality = QualityCategory.Excellent;
        public bool perStand = true;
    }

    // DefModExtension carrying the stocking rule for a LayoutRoomDef: GenStep_StockOutfitStands
    // applies it to the empty stands inside each room the def built, before its own map-wide
    // default reaches anything left over. Room-specific outfits (marine armour in the armory,
    // vacsuits at the corridor airlocks) live here instead of in RoomContentsWorker code.
    public class OutfitStandRuleExtension : DefModExtension
    {
        public OutfitStandRule rule;
    }
}
