using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Armory
{
    public class RoomContents_Armory : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0) return;

            // Outfit stands are stocked later by BTG_StockOutfitStands from this room def's
            // OutfitStandRuleExtension (Armory.xml).
            foreach (CellRect roomRect in room.rects)
                ArmoryShelfFiller.FillWeaponShelves(map, roomRect);
        }
    }
}
