using System.Collections.Generic;
using BetterTradersGuild.AI;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that sets the glow color of wall lamps on the map.
    //
    // XML-configurable parameters:
    // - wallLampDef: ThingDef of the wall lamp to modify
    // - glowColor: ColorInt for the glow color (r, g, b)
    // - roomOverrides: optional per-room recolors; each entry names LayoutRoomDefs
    //   and the glow color lamps inside those rooms get instead of glowColor
    //
    // Example usage in GenStepDef:
    // <genStep Class="BetterTradersGuild.MapGeneration.GenStep_SetWallLampColor">
    //   <wallLampDef>WallLamp</wallLampDef>
    //   <glowColor>(187, 187, 221)</glowColor>
    //   <roomOverrides>
    //     <li>
    //       <rooms><li>BTG_Armory</li></rooms>
    //       <glowColor>(224, 54, 54)</glowColor>
    //     </li>
    //   </roomOverrides>
    // </genStep>
    //
    // Used to give TradersGuild settlements a consistent blue-tinted lighting
    // aesthetic that matches AncientEmergencyLight_Blue, with the smuggler's den
    // variant recoloring corridor and armory lamps to emergency-light red.
    public class GenStep_SetWallLampColor : GenStep
    {
        // ThingDef of the wall lamp to modify. Set via XML.
        public ThingDef wallLampDef;

        // Glow color to apply to lamps. Set via XML.
        // Format: (r, g, b) where each value is 0-255.
        public ColorInt glowColor;

        // Optional per-room recolors. Set via XML.
        public List<RoomGlowOverride> roomOverrides;

        public class RoomGlowOverride
        {
            public List<LayoutRoomDef> rooms;
            public ColorInt glowColor;
        }

        // Deterministic seed for this GenStep.
        public override int SeedPart => 847291004;

        // Sets the glow color of all wall lamps matching wallLampDef.
        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null || wallLampDef == null)
                return;

            foreach (Thing lamp in map.listerThings.ThingsOfDef(wallLampDef))
            {
                CompGlower glower = lamp.TryGetComp<CompGlower>();
                if (glower != null)
                {
                    glower.GlowColor = ColorFor(map, lamp);
                }
            }
        }

        // The override color of the layout room containing the lamp, or the base
        // glowColor when no override matches. A wall attachment's Position is the
        // interior floor cell beside its wall (rotation faces the wall; the near
        // full-cell drawOffset only RENDERS it on the wall tile), so Position alone
        // identifies the lamp's room unambiguously.
        private ColorInt ColorFor(Map map, Thing lamp)
        {
            if (roomOverrides.NullOrEmpty())
                return glowColor;

            LayoutRoom room = StructureRoomLocator.RoomContaining(map, lamp.Position);
            if (room == null)
                return glowColor;

            foreach (RoomGlowOverride roomOverride in roomOverrides)
            {
                if (roomOverride.rooms == null)
                    continue;
                foreach (LayoutRoomDef roomDef in roomOverride.rooms)
                {
                    if (StructureRoomLocator.IsOfDef(room, roomDef))
                        return roomOverride.glowColor;
                }
            }
            return glowColor;
        }
    }
}
