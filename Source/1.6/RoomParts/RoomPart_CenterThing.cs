using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    /// <summary>
    /// RoomPartWorker that places a Thing in the center of the room.
    /// Used for placing the cargo vault exit portal in the center of the vault.
    ///
    /// Usage in XML:
    /// <![CDATA[
    /// <RoomPart_ThingDef>
    ///   <defName>BTG_CargoVaultExit_Center</defName>
    ///   <workerClass>BetterTradersGuild.RoomParts.RoomPart_CenterThing</workerClass>
    ///   <thingDef>BTG_CargoVaultExit</thingDef>
    /// </RoomPart_ThingDef>
    /// ]]>
    /// </summary>
    public class RoomPart_CenterThing : RoomPartWorker
    {
        private RoomPart_ThingDef Def => (RoomPart_ThingDef)def;

        public RoomPart_CenterThing(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            if (Def.thingDef == null)
                return;

            if (room.rects == null || room.rects.Count == 0)
                return;

            // Get the center of the room's first rect (for single-rect rooms)
            // For multi-rect rooms, use the combined bounding rect
            CellRect boundingRect = room.rects[0];
            for (int i = 1; i < room.rects.Count; i++)
                boundingRect = boundingRect.Encapsulate(room.rects[i]);
            IntVec3 center = boundingRect.CenterCell;

            // For multi-cell things (like 3x3 exit), offset to get bottom-left corner
            IntVec2 thingSize = Def.thingDef.size;
            int offsetX = (thingSize.x - 1) / 2;
            int offsetZ = (thingSize.z - 1) / 2;
            IntVec3 spawnPos = new IntVec3(center.x - offsetX, 0, center.z - offsetZ);

            if (!spawnPos.InBounds(map))
                return;

            // Create and spawn the thing
            Thing thing = ThingMaker.MakeThing(Def.thingDef, Def.stuffDef);
            GenSpawn.Spawn(thing, spawnPos, map, WipeMode.VanishOrMoveAside);
        }
    }
}
