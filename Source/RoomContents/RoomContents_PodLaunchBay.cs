using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom room contents worker for the Pod Launch Bay.
    /// Handles specialized spawning logic for pod launch facilities.
    /// </summary>
    public class RoomContents_PodLaunchBay : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method for the pod launch bay.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // TODO: Wire up VE chemfuel pipe integration once mod support is added
            // This will connect pod launchers to the station's fuel network

            // Base implementation handles standard prefab spawning
            base.FillRoom(map, room, faction, threatPoints);
        }
    }
}
