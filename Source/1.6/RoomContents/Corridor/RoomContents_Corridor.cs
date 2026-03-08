using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    public class RoomContents_Corridor : RoomContents_Orbital_Corridor
    {
        /// <summary>
        /// The structure interior contracted by 4 cells from the perimeter.
        /// Cells outside this rect are reserved for airlock defense prefabs.
        /// Set before base.FillRoom() so IsValidCellBase can use it.
        /// </summary>
        private CellRect validInterior;

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Compute the valid interior before base.FillRoom() so that
            // IsValidCellBase blocks prefab placement near perimeter blast doors
            // We want to reserve this space for SpawnAirlockDefences
            validInterior = GetStructureBounds(map).ContractedBy(4);

            base.FillRoom(map, room, faction, threatPoints);

            CorridorAirlockDefenceSpawner.SpawnAirlockDefences(map, room, faction);

            CorridorTurretReplacer.ReplaceAncientTurrets(map, room, faction);
        }

        /// <summary>
        /// Blocks base prefab placement within 4 cells of the structure perimeter,
        /// reserving that space for airlock defense prefabs spawned after base.FillRoom().
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            if (validInterior.Width > 0 && !validInterior.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        private CellRect GetStructureBounds(Map map)
        {
            if (map.layoutStructureSketches != null && map.layoutStructureSketches.Count > 0)
                return map.layoutStructureSketches[0].structureLayout.container;

            return CellRect.Empty;
        }
    }
}
