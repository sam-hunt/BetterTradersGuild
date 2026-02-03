using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    public class RoomContents_Corridor : RoomContents_Orbital_Corridor
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            base.FillRoom(map, room, faction, threatPoints);

            CorridorTurretReplacer.ReplaceAncientTurrets(map, room, faction);

            CorridorAirlockSpawner.SpawnAirlocks(map, room);

            CorridorOxygenPumpSpawner.SpawnOxygenPumps(map, room);

            List<Building_OutfitStand> vacsuitStands = CorridorVacsuitStandSpawner.SpawnVacsuitStands(map, room, faction);

            foreach (Building_OutfitStand stand in vacsuitStands)
                PaintableFurnitureHelper.TryPaint(stand, Colors.BTG_OrbitalSteel);
        }
    }
}
