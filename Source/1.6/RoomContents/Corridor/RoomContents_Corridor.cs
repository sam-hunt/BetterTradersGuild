using System;
using System.Collections.Generic;
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

            // Clean checkpoint debris AND wallAttachment-spawned buildings (e.g.
            // LifeSupportUnit) from the reserved perimeter zone before placing
            // airlock defences. Checkpoints bypass IsValidCellBase via direct
            // GenSpawn.Spawn; wallAttachments are placed by vanilla LayoutWorker
            // before FillRoom is called.
            ClearCheckpointsFromReservedZone(map, room);

            try
            {
                CorridorAirlockDefenceSpawner.SpawnAirlockDefences(map, room, faction);
            }
            catch (Exception e)
            {
                Log.Warning($"[Better Traders Guild] Error spawning airlock defences: {e}");
            }

            try
            {
                CorridorTurretReplacer.ReplaceAncientTurrets(map, room, faction);
            }
            catch (Exception e)
            {
                Log.Warning($"[Better Traders Guild] Error replacing turrets: {e}");
            }
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
        /// Destroys checkpoint barricades, turrets, and wall-attached buildings that were
        /// placed in the reserved perimeter zone. Checks all things on each cell, not just
        /// the edifice, because wallAttachment-spawned buildings (e.g. LifeSupportUnit)
        /// share a cell with the wall and are not the edifice.
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

                    List<Thing> things = cell.GetThingList(map);
                    for (int i = things.Count - 1; i >= 0; i--)
                    {
                        if (IsCheckpointDebris(things[i].def))
                            things[i].Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        private static bool IsCheckpointDebris(ThingDef def)
        {
            return def == Things.Barricade
                || def == Things.AncientSecurityTurret
                || def == Things.Turret_AncientArmoredTurret
                || def == Things.LifeSupportUnit
                || def == Things.WallLamp;
        }

        private CellRect GetStructureBounds(Map map)
        {
            if (map.layoutStructureSketches != null && map.layoutStructureSketches.Count > 0)
                return map.layoutStructureSketches[0].structureLayout.container;

            return CellRect.Empty;
        }
    }
}
