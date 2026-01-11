using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    /// <summary>
    /// Replaces decorative AncientSecurityTurret buildings with functional Turret_MiniTurret.
    ///
    /// Vanilla orbital corridors spawn AncientSecurityTurret which are non-functional
    /// decorative turrets. This class finds and replaces them with working mini turrets
    /// assigned to the station's faction.
    /// </summary>
    public static class CorridorTurretReplacer
    {
        /// <summary>
        /// Finds all AncientSecurityTurret (broken decorative turrets) in the room
        /// and replaces them with functional Turret_MiniTurret assigned to the faction.
        /// </summary>
        /// <param name="map">The map containing the room.</param>
        /// <param name="room">The corridor LayoutRoom to scan for turrets.</param>
        /// <param name="faction">The faction to assign the new turrets to.</param>
        public static void ReplaceAncientTurrets(Map map, LayoutRoom room, Faction faction)
        {
            // Collect turrets to replace (can't modify collection while iterating)
            var turretsToReplace = new List<(IntVec3 position, Rot4 rotation)>();

            foreach (IntVec3 cell in room.Cells)
            {
                var turret = cell.GetFirstThing<Building>(map);
                if (turret != null && turret.def == Things.AncientSecurityTurret)
                {
                    turretsToReplace.Add((cell, turret.Rotation));
                }
            }

            // Replace each turret
            foreach (var (position, rotation) in turretsToReplace)
            {
                // Destroy the old decorative turret
                var oldTurret = position.GetFirstThing<Building>(map);
                oldTurret?.Destroy(DestroyMode.Vanish);

                // Spawn functional mini turret (made from steel)
                var newTurret = ThingMaker.MakeThing(Things.Turret_MiniTurret, Things.Steel);
                newTurret.SetFaction(faction);
                GenSpawn.Spawn(newTurret, position, map, rotation);
            }
        }
    }
}
