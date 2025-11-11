using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for BTG Orbital Corridors.
    ///
    /// Currently extends vanilla orbital corridor generation without modifications.
    /// Ancient tile replacement is handled globally by GenStepOrbitalPlatformPostProcess.
    ///
    /// ARCHITECTURE NOTE: Originally contained local terrain replacement logic, but
    /// this was moved to a global post-processing pass for better maintainability
    /// and to catch ancient tiles from all sources (cannons, prefabs, etc.).
    ///
    /// LEARNING NOTE: This demonstrates how sometimes the simplest approach is to
    /// inherit vanilla behavior directly and handle aesthetic changes at a higher level.
    /// </summary>
    public class RoomContents_BTG_OrbitalCorridor : RoomContents_Orbital_Corridor
    {
        /// <summary>
        /// Main room generation method - currently just calls vanilla implementation.
        /// Ancient tile replacement is handled globally after all generation completes.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Call vanilla orbital corridor worker to spawn all content
            // (walls, doors, blast doors, life support units, etc.)
            base.FillRoom(map, room, faction, threatPoints);

            // Ancient tiles are now replaced globally by GenStepOrbitalPlatformPostProcess
            // This catches tiles from corridors, Gauss cannons, exterior prefabs, etc.

            // TODO (Phase 3.3+): Add airlock placement at corridor exits to space
            //
            // Planned approach:
            // 1. Detect corridor exits (cells adjacent to vacuum/space)
            // 2. For each exit, define a 3x3 zone in front of the exit
            // 3. Despawn any turrets/barricades/prefabs in the 3x3 zone
            // 4. Spawn custom airlock prefab (autodoor + reinforced walls)
            //
            // This ensures airlocks are positioned correctly even when defensive
            // corridor prefabs (CorridorBarricade, etc.) would otherwise block them.
        }
    }
}
