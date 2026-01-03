using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    /// <summary>
    /// RoomPartWorker that spawns a Mech_Cleansweeper pawn in the room.
    ///
    /// PURPOSE:
    /// Adds thematic cleaning mechs to TradersGuild settlement rooms like
    /// MessHall, CommandersQuarters, RecRoom, and Storeroom.
    ///
    /// TECHNICAL APPROACH:
    /// Follows vanilla RoomPart_SentryDrone pattern exactly:
    /// 1. Check DLC requirement (Biotech for mechs)
    /// 2. Find random standable cell in room
    /// 3. Generate pawn with PawnGenerator
    /// 4. Spawn pawn assigned to room's faction
    ///
    /// USAGE IN XML:
    /// <![CDATA[
    /// <parts>
    ///   <BTG_Mech_Cleansweeper MayRequire="Ludeon.RimWorld.Biotech">0.5</BTG_Mech_Cleansweeper>
    /// </parts>
    /// ]]>
    ///
    /// EXPECTED BEHAVIOR:
    /// Mech spawns assigned to TradersGuild faction. Default mech AI should
    /// idle/wander until detecting hostile pawns, then attack.
    ///
    /// LEARNING NOTE (RoomPartWorker):
    /// RoomPartWorker.FillRoom() is called during room content generation.
    /// The probability (0.5) is handled by the layout system before calling
    /// this worker - we always spawn when FillRoom is called.
    /// </summary>
    public class RoomPart_MechCleansweeper : RoomPartWorker
    {
        /// <summary>
        /// Cached PawnKindDef for Mech_Cleansweeper to avoid repeated lookups.
        /// </summary>
        private static PawnKindDef cachedMechKind;

        public RoomPart_MechCleansweeper(RoomPartDef def) : base(def) { }

        /// <summary>
        /// Spawns a Mech_Cleansweeper pawn in the room.
        /// </summary>
        /// <param name="map">The map being generated</param>
        /// <param name="room">The room to fill</param>
        /// <param name="faction">Faction to assign the mech to (TradersGuild)</param>
        /// <param name="threatPoints">Threat budget (unused for this spawner)</param>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            // STEP 1: Require Biotech DLC (Mech_Cleansweeper is Biotech-only)
            if (!ModsConfig.BiotechActive)
            {
                return;
            }

            // STEP 2: Cache PawnKindDef lookup
            if (cachedMechKind == null)
            {
                cachedMechKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mech_Cleansweeper");
                if (cachedMechKind == null)
                {
                    Log.ErrorOnce("[Better Traders Guild] Could not find PawnKindDef 'Mech_Cleansweeper'. " +
                                  "This should not happen if Biotech is active.", 94712385);
                    return;
                }
            }

            // STEP 3: Find standable cell in room
            // Using same pattern as RoomPart_SentryDrone
            if (!room.TryGetRandomCellInRoom(map, out IntVec3 cell, 0, 0,
                c => c.Standable(map), false))
            {
                // Silent fail - room may be too small or fully occupied
                return;
            }

            // STEP 4: Generate mech assigned to room's faction
            Pawn mech = PawnGenerator.GeneratePawn(cachedMechKind, faction);

            // STEP 5: Spawn mech at location
            GenSpawn.Spawn(mech, cell, map, WipeMode.Vanish);
        }
    }
}
