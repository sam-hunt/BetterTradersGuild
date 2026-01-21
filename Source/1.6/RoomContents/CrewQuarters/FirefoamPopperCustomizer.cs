using System;
using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Handles customization of firefoam poppers used as markers in CrewQuarters subrooms.
    /// Each marker has various replacement/removal chances including
    /// drones, mechs, shamblers, pets, furniture, and decorative items.
    /// </summary>
    internal static class FirefoamPopperCustomizer
    {
        /// <summary>
        /// Weighted outcomes for firefoam popper customization.
        /// Lazily built to filter out DLC-gated outcomes when those DLCs aren't present.
        /// </summary>
        private static List<(float weight, Action<Thing, Map, Faction> action)> _outcomes;
        private static List<(float weight, Action<Thing, Map, Faction> action)> Outcomes => _outcomes ?? (_outcomes = BuildOutcomes());

        private static List<(float weight, Action<Thing, Map, Faction> action)> BuildOutcomes()
        {
            var outcomes = new List<(float weight, Action<Thing, Map, Faction> action)>
            {
                (40f, (marker, map, faction) => marker.Destroy(DestroyMode.Vanish)),
                (3f,  (marker, map, faction) => ReplaceWithTrap(marker, Things.HunterDroneTrap, map, faction)),
                (3f,  (marker, map, faction) => ReplaceWithTrap(marker, Things.WaspDroneTrap, map, faction)),
                (5f,  (marker, map, faction) => TrySpawnHeater(marker, map)),
                (7f,  (marker, map, faction) => SpawnPetWithKibble(marker, map)),
                (1f,  (marker, map, faction) => TrySpawnGameOfUr(marker, map)),
                (2f,  (marker, map, faction) => TrySpawnHorseshoePin(marker, map)),
                (4f,  (marker, map, faction) => TrySpawnPlantPot(marker, map)),
                (10f, (marker, map, faction) => { }), // Keep firefoam popper
                (3f,  (marker, map, faction) => ReplaceWithPartySpot(marker, map)),
                (8f,  (marker, map, faction) => SpawnTrashPile(marker, map))
            };

            // Royalty - Meditation Spot
            if (Things.MeditationSpot != null)
                outcomes.Add((20f, (marker, map, faction) => ReplaceWithMeditationSpot(marker, map)));

            // Biotech DLC - Militor
            if (PawnKinds.Mech_Militor != null)
                outcomes.Add((2f, (marker, map, faction) => SpawnMechAtPosition(marker, PawnKinds.Mech_Militor, map, faction)));

            // Anomaly DLC - Shambler
            if (PawnKinds.ShamblerSwarmer != null)
                outcomes.Add((2f, (marker, map, faction) => SpawnShamblerAtPosition(marker, PawnKinds.ShamblerSwarmer, map)));

            // VFE Spacer - Interactive Table 1x1
            if (Things.Table_interactive_1x1c != null)
                outcomes.Add((5f, (marker, map, faction) => CrewQuartersHelpers.ReplaceThingAt(marker, Things.Table_interactive_1x1c, Things.Steel, map)));

            // VFE Spacer - Air Purifier
            if (Things.VFES_AirPurifier != null)
                outcomes.Add((5f, (marker, map, faction) => CrewQuartersHelpers.ReplaceThingAt(marker, Things.VFES_AirPurifier, null, map)));

            return outcomes;
        }

        /// <summary>
        /// Finds and customizes firefoam poppers (used as markers) in subrooms.
        /// Each marker has various replacement/removal chances.
        /// </summary>
        internal static void Customize(Map map, List<CellRect> subroomRects, Faction faction)
        {
            // Find all FirefoamPoppers in subroom areas (used as customization markers)
            List<Thing> markers = new List<Thing>();
            foreach (CellRect subroomRect in subroomRects)
            {
                foreach (IntVec3 cell in subroomRect)
                {
                    if (!cell.InBounds(map)) continue;
                    foreach (Thing thing in cell.GetThingList(map).ToList())
                    {
                        if (thing.def == Things.FirefoamPopper)
                        {
                            markers.Add(thing);
                        }
                    }
                }
            }

            foreach (Thing marker in markers)
            {
                var (_, action) = Outcomes.RandomElementByWeight(x => x.weight);
                action(marker, map, faction);
            }
        }

        private static void TrySpawnHeater(Thing marker, Map map)
        {
            if (Things.Heater == null) return;
            CrewQuartersHelpers.ReplaceThingAt(marker, Things.Heater, null, map);
        }

        private static void TrySpawnGameOfUr(Thing marker, Map map)
        {
            if (Things.GameOfUrBoard == null) return;
            CrewQuartersHelpers.ReplaceThingAt(marker, Things.GameOfUrBoard, Things.Steel, map);
        }

        private static void TrySpawnHorseshoePin(Thing marker, Map map)
        {
            if (Things.HorseshoesPin == null) return;
            CrewQuartersHelpers.ReplaceThingAt(marker, Things.HorseshoesPin, Things.Steel, map);
        }

        private static void TrySpawnPlantPot(Thing marker, Map map)
        {
            if (Things.PlantPot == null) return;
            CrewQuartersHelpers.ReplaceThingAt(marker, Things.PlantPot, Things.Steel, map);
        }

        /// <summary>
        /// Replaces a marker with a pile of trash filth.
        /// Spawns moldy uniform and trash at the marker position, plus more trash at a nearby cell.
        /// Also attempts to replace the nearest PlantPot with an AncientPlantPot.
        /// </summary>
        private static void SpawnTrashPile(Thing marker, Map map)
        {
            if (Things.Filth_Trash == null) return;

            IntVec3 pos = marker.Position;
            marker.Destroy(DestroyMode.Vanish);

            // Spawn moldy uniform filth at the marker position
            if (Things.Filth_MoldyUniform != null)
                FilthMaker.TryMakeFilth(pos, map, Things.Filth_MoldyUniform, 1);

            // Spawn 10 trash filth at the marker position
            FilthMaker.TryMakeFilth(pos, map, Things.Filth_Trash, 10);

            // Find first empty nearby cell and spawn 5 more trash there
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(new TargetInfo(pos, map)))
            {
                if (cell.InBounds(map) && cell.Standable(map) && !cell.GetThingList(map).Any(t => t is Filth))
                {
                    FilthMaker.TryMakeFilth(cell, map, Things.Filth_Trash, 5);
                    break;
                }
            }

            // Replace nearest PlantPot with AncientPlantPot (cracked/dead variant)
            TryReplaceNearestPlantPot(pos, map);
        }

        /// <summary>
        /// Finds the nearest PlantPot building and replaces it with an AncientPlantPot.
        /// </summary>
        private static void TryReplaceNearestPlantPot(IntVec3 searchOrigin, Map map)
        {
            if (Things.PlantPot == null || Things.AncientPlantPot == null) return;

            // Search for nearest PlantPot using expanding radius
            Thing nearestPot = null;
            float nearestDistSq = float.MaxValue;

            foreach (Thing thing in map.listerThings.ThingsOfDef(Things.PlantPot))
            {
                float distSq = thing.Position.DistanceToSquared(searchOrigin);
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearestPot = thing;
                }
            }

            if (nearestPot == null) return;

            // Replace with AncientPlantPot
            IntVec3 potPos = nearestPot.Position;
            Rot4 potRot = nearestPot.Rotation;
            nearestPot.Destroy(DestroyMode.Vanish);

            Thing ancientPot = ThingMaker.MakeThing(Things.AncientPlantPot);
            GenSpawn.Spawn(ancientPot, potPos, map, potRot);
        }

        /// <summary>
        /// Replaces a marker with a dormant drone trap.
        /// </summary>
        private static void ReplaceWithTrap(Thing marker, ThingDef trapDef, Map map, Faction faction)
        {
            IntVec3 pos = marker.Position;
            marker.Destroy(DestroyMode.Vanish);

            Thing trap = ThingMaker.MakeThing(trapDef);
            GenSpawn.Spawn(trap, pos, map);
            trap.SetFactionDirect(faction);
        }

        /// <summary>
        /// Replaces a marker with a mech pawn.
        /// </summary>
        private static void SpawnMechAtPosition(Thing marker, PawnKindDef mechKind, Map map, Faction faction)
        {
            IntVec3 pos = marker.Position;
            marker.Destroy(DestroyMode.Vanish);

            Pawn mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: mechKind,
                faction: faction,
                context: PawnGenerationContext.NonPlayer,
                tile: map.Tile));

            GenSpawn.Spawn(mech, pos, map);
        }

        /// <summary>
        /// Replaces a marker with a shambler (factionless).
        /// </summary>
        private static void SpawnShamblerAtPosition(Thing marker, PawnKindDef shamblerKind, Map map)
        {
            IntVec3 pos = marker.Position;
            marker.Destroy(DestroyMode.Vanish);

            Pawn shambler = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: shamblerKind,
                faction: null,
                context: PawnGenerationContext.NonPlayer,
                tile: map.Tile));

            GenSpawn.Spawn(shambler, pos, map);
        }

        /// <summary>
        /// Replaces a marker with an animal bed and spawns a pet.
        /// Adds kibble to the nearest reachable small shelf.
        /// </summary>
        private static void SpawnPetWithKibble(Thing marker, Map map)
        {
            IntVec3 pos = marker.Position;
            Rot4 rot = marker.Rotation;
            marker.Destroy(DestroyMode.Vanish);

            // Spawn animal bed
            if (Things.AnimalBed != null)
            {
                Thing bed = ThingMaker.MakeThing(Things.AnimalBed, Things.Steel);
                GenSpawn.Spawn(bed, pos, map, rot);
            }

            // Spawn random pet (cat or dog) using weighted selection
            RoomPetHelper.SpawnPetAtPosition(map, pos);

            // Add kibble to nearest small shelf
            RoomPetHelper.AddKibbleToNearestShelf(map, pos);
        }

        /// <summary>
        /// Replaces marker with a meditation spot (Royalty DLC).
        /// </summary>
        private static void ReplaceWithMeditationSpot(Thing marker, Map map)
        {
            CrewQuartersHelpers.ReplaceThingAt(marker, Things.MeditationSpot, null, map);
        }

        /// <summary>
        /// Replaces marker with a party spot.
        /// </summary>
        private static void ReplaceWithPartySpot(Thing marker, Map map)
        {
            IntVec3 pos = marker.Position;
            marker.Destroy(DestroyMode.Vanish);

            Thing partySpot = ThingMaker.MakeThing(ThingDefOf.PartySpot);
            GenSpawn.Spawn(partySpot, pos, map);
        }
    }
}
