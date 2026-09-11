using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that replaces some of the platform step's corner cannons with a live
    // turret def owned by the map's faction.
    //
    // GenStep_OrbitalPlatform spawns cannonDef at all four corners of the platform
    // rect, factionless. Vanilla's GaussCannon is scenery (no verbs, not targetable),
    // so that is harmless there, but a real turret spawned the same way can never
    // acquire a target: GenHostility.HostileTo is false whenever either side has no
    // faction. This step therefore swaps N of the spawned cannons for toDef at the
    // same cell and rotation and claims the replacement for map.ParentFaction (the
    // faction the platform step built for under useSiteFaction). The corners not
    // picked keep the original inert cannon, so a partial swap needs no extra
    // bookkeeping. Terrain pads are untouched: they are sized from cannonDef.size,
    // so toDef must share fromDef's footprint.
    //
    // XML-configurable parameters:
    // - fromDef: the cannon def the platform step spawned (vanilla GaussCannon)
    // - toDef: the live replacement (VGE2's VGE_EnemyGaussCannon)
    // - count: how many cannons to swap, chosen at random among those present
    //
    // Runs right after the platform step (order 205), before anything that could care
    // about the map's turrets. The vanilla cannon is despawned rather than destroyed:
    // GaussCannon is destroyable=false, and Thing.Destroy refuses (and logs) for it.
    public class GenStep_SwapPlatformCannons : GenStep
    {
        // Cannon def to look for. Set via XML.
        public ThingDef fromDef;

        // Replacement def. Set via XML.
        public ThingDef toDef;

        // Number of cannons to swap. Set via XML.
        public int count = 2;

        public override int SeedPart => 1407252189;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (fromDef == null || toDef == null || count <= 0)
                return;

            Faction faction = map.ParentFaction;
            if (faction == null)
            {
                // A factionless replacement would be as inert as the original, and
                // would also carry the replacement's reveal letter: keep the vanilla
                // cannons instead.
                Log.Warning("[Better Traders Guild] GenStep_SwapPlatformCannons: map has no parent faction, keeping the "
                    + fromDef.defName + " cannons.");
                return;
            }

            List<Thing> cannons = map.listerThings.ThingsOfDef(fromDef).ToList();
            if (cannons.Count == 0)
                return;

            foreach (Thing cannon in cannons.InRandomOrder().Take(count).ToList())
            {
                IntVec3 position = cannon.Position;
                Rot4 rotation = cannon.Rotation;
                cannon.DeSpawn();

                Thing replacement = ThingMaker.MakeThing(toDef);
                replacement.SetFaction(faction);
                GenSpawn.Spawn(replacement, position, map, rotation);
            }
        }
    }
}
