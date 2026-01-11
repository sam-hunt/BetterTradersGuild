using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    /// <summary>
    /// Custom RoomContentsWorker for BTG Corridors.
    ///
    /// Extends vanilla orbital corridor generation with:
    /// - Replacement of broken AncientSecurityTurret with functional Turret_MiniTurret
    /// - Airlock walls with VacBarriers at blast door exits
    /// - Outfit stands with vacsuits near airlock exits (50% chance per airlock)
    /// - Outfit stands painted with BTG_OrbitalSteel color
    ///
    /// Ancient tile replacement is handled globally by GenStepOrbitalPlatformPostProcess.
    /// </summary>
    public class RoomContents_Corridor : RoomContents_Orbital_Corridor
    {
        /// <summary>
        /// Main room generation method.
        /// Calls vanilla implementation then adds airlocks at blast door exits
        /// and spawns outfit stands with vacsuits nearby.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call vanilla orbital corridor worker to spawn all content
            //    (walls, doors, blast doors, life support units, etc.)
            base.FillRoom(map, room, faction, threatPoints);

            // Ancient tiles are now replaced globally by GenStepOrbitalPlatformPostProcess
            // This catches tiles from corridors, Gauss cannons, exterior prefabs, etc.

            // 2. Replace decorative AncientSecurityTurret with functional Turret_MiniTurret
            CorridorTurretReplacer.ReplaceAncientTurrets(map, room, faction);

            // 3. Spawn airlock walls with VacBarriers at blast door exits
            //    This creates atmospheric separation at corridor exits to space
            CorridorAirlockSpawner.SpawnAirlocks(map, room);

            // 4. Spawn oxygen pumps on corridor walls inside airlock areas
            //    These provide atmospheric repressurization between the barrier and blast door
            CorridorOxygenPumpSpawner.SpawnOxygenPumps(map, room);

            // 5. Spawn outfit stands with vacsuits near airlocks (50% chance per airlock)
            //    Provides emergency vacuum protection gear at corridor exits
            List<Building_OutfitStand> vacsuitStands = CorridorVacsuitStandSpawner.SpawnVacsuitStands(map, room);

            // 6. Paint outfit stands with BTG_OrbitalSteel color to match station aesthetics
            foreach (Building_OutfitStand stand in vacsuitStands)
            {
                PaintableFurnitureHelper.TryPaint(stand, Colors.BTG_OrbitalSteel);
            }
        }
    }
}
