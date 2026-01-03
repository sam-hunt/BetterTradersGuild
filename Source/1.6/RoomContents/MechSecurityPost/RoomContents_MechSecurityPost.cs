using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.MechSecurityPost
{
    /// <summary>
    /// Custom room contents worker for the Mech Security Post.
    ///
    /// Post-processes spawned gestator tanks to change their glow color from
    /// hostile orange to friendly green ("systems operational"). This visually
    /// differentiates TradersGuild security gestators from hostile ancient sites.
    ///
    /// Color choice: (0, 235, 31) - Same green used by AncientMachineTerminal,
    /// conveying "everything is working properly" status.
    ///
    /// The glow color is persisted via CompGlower.glowColorOverride which is
    /// saved in PostExposeData, so this only needs to run once during generation.
    /// </summary>
    public class RoomContents_MechSecurityPost : RoomContentsWorker
    {
        // "Systems operational" green - same as AncientMachineTerminal
        private static readonly ColorInt TradersGuildGlowColor = new ColorInt(0, 235, 31, 0);

        /// <summary>
        /// Main room generation method for the mech security post.
        /// Spawns XML-defined prefabs and vanilla parts (GestationTanks),
        /// then post-processes gestator tanks to set friendly glow color.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs and vanilla parts (GestationTanks)
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Post-process spawned gestator tanks
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                SetGestatorGlowColors(map, roomRect);
            }
        }

        /// <summary>
        /// Finds all gestator tanks in the room and sets their glow color to friendly green.
        /// </summary>
        private void SetGestatorGlowColors(Map map, CellRect roomRect)
        {
            foreach (IntVec3 cell in roomRect)
            {
                foreach (Thing thing in cell.GetThingList(map))
                {
                    // Check for CompMechGestatorTank
                    CompMechGestatorTank gestatorComp = thing.TryGetComp<CompMechGestatorTank>();
                    if (gestatorComp == null)
                        continue;

                    // Get the glower component on the same building
                    CompGlower glower = thing.TryGetComp<CompGlower>();
                    if (glower == null)
                        continue;

                    // Set the glow color to friendly green
                    glower.GlowColor = TradersGuildGlowColor;
                }
            }
        }
    }
}
