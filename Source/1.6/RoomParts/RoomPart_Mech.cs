using System;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    // Generic RoomPartWorker that spawns a mech based on XML-defined parameters.
    //
    // PURPOSE:
    // Single worker class that handles all mech types via parameterization.
    // The mech type and behavior are specified in the RoomPart_MechDef XML.
    //
    // TECHNICAL APPROACH:
    // - Casts def to RoomPart_MechDef to access pawnKindDef and behavior
    // - Follows vanilla RoomPart_SentryDrone pattern for spawning
    // - Uses RoomMechLordHelper to assign mechs to room-specific Lords
    //
    // USAGE:
    // <BetterTradersGuild.RoomParts.RoomPart_MechDef MayRequire="Ludeon.RimWorld.Biotech">
    //   <defName>BTG_Mech_Cleansweeper</defName>
    //   <workerClass>BetterTradersGuild.RoomParts.RoomPart_Mech</workerClass>
    //   <pawnKindDef>Mech_Cleansweeper</pawnKindDef>
    //   <behavior>Clean</behavior>
    // </BetterTradersGuild.RoomParts.RoomPart_MechDef>
    public class RoomPart_Mech : RoomPartWorker
    {
        private RoomPart_MechDef Def => (RoomPart_MechDef)def;

        public RoomPart_Mech(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            if (Def.pawnKindDef == null)
                return;

            // Find standable cell in room
            if (!room.TryGetRandomCellInRoom(map, out IntVec3 cell, 0, 0, c => c.Standable(map), false))
                return;

            // Pin the age in the generation request (vanilla rolls mechs into the hundreds/thousands
            // of years). Setting it here lands before the renderer resolves the life-stage graphic.
            // The range deliberately straddles age 100: mechs swap to a grimier "*Ancient" body
            // texture at that life-stage boundary, so 60-120 yields a mixed fleet of clean and
            // weathered units. Do not cap below 100 unless you want them all clean.
            float age = Rand.Range(60f, 120f);
            Pawn mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: Def.pawnKindDef,
                faction: faction,
                context: PawnGenerationContext.NonPlayer,
                tile: map.Tile,
                fixedBiologicalAge: age,
                fixedChronologicalAge: age));

            // Replace the default numeric name ("Cleansweeper 1") with a NamerMech roll.
            // Guard the call: a foreign patch on the name generator that throws should leave
            // the vanilla numeric name intact rather than abort mech spawning.
            if (mech.RaceProps.IsMechanoid)
            {
                try
                {
                    mech.Name = PawnBioAndNameGenerator.GeneratePawnName(mech, NameStyle.Full);
                }
                catch (Exception e)
                {
                    Log.Warning($"[Better Traders Guild] Error generating mech name, keeping default: {e}");
                }
            }

            GenSpawn.Spawn(mech, cell, map, WipeMode.Vanish);

            // Add to room's Lord with configured behavior
            RoomMechLordHelper.AddMechToRoomLord(mech, map, room, faction, Def.behavior);
        }
    }
}
