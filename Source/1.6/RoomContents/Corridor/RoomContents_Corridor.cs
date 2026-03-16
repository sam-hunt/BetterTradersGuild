using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    public class RoomContents_Corridor : RoomContents_Orbital_Corridor
    {
        /// <summary>
        /// The structure interior contracted by 4 cells from the perimeter.
        /// Cells outside this rect are reserved for airlock defense prefabs.
        /// </summary>
        private CellRect validInterior;

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            validInterior = GetStructureBounds(map).ContractedBy(4);

            base.FillRoom(map, room, faction, threatPoints);

            // base.FillRoom() triggers Checkpoint_Corridor.SpawnCheckpoints() which
            // spawns barricades and turrets via GenSpawn.Spawn directly, bypassing
            // IsValidCellBase. Clean them out of the reserved perimeter zone before
            // placing airlock defences.
            ClearCheckpointsFromReservedZone(map, room);

            CorridorAirlockDefenceSpawner.SpawnAirlockDefences(map, room, faction);

            CorridorTurretReplacer.ReplaceAncientTurrets(map, room, faction);
        }

        /// <summary>
        /// Blocks base prefab placement within 4 cells of the structure perimeter,
        /// reserving that space for airlock defense prefabs spawned after base.FillRoom().
        /// Note: This only guards against XML prefab placement. Checkpoint barricades
        /// bypass this entirely (see ClearCheckpointsFromReservedZone).
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            if (validInterior.Width > 0 && !validInterior.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Destroys checkpoint barricades and turrets that were placed in the reserved
        /// perimeter zone by RoomContents_Checkpoint_Corridor.SpawnCheckpoints().
        /// </summary>
        private void ClearCheckpointsFromReservedZone(Map map, LayoutRoom room)
        {
            if (validInterior.Width <= 0)
                return;

            foreach (CellRect rect in room.rects)
            {
                foreach (IntVec3 cell in rect)
                {
                    if (validInterior.Contains(cell) || !cell.InBounds(map))
                        continue;

                    Building edifice = cell.GetEdifice(map);
                    if (edifice != null && IsCheckpointDebris(edifice.def))
                        edifice.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static bool IsCheckpointDebris(ThingDef def)
        {
            return def == Things.Barricade
                || def == Things.AncientSecurityTurret
                || def == Things.Turret_AncientArmoredTurret;
        }

        private CellRect GetStructureBounds(Map map)
        {
            if (map.layoutStructureSketches != null && map.layoutStructureSketches.Count > 0)
                return map.layoutStructureSketches[0].structureLayout.container;

            return CellRect.Empty;
        }
    }
}
