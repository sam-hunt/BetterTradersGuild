using System;
using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Handles customization of meditation spots in CrewQuarters subrooms.
    /// Each meditation spot has various replacement/removal chances including
    /// drones, mechs, shamblers, pets, furniture, and decorative items.
    /// </summary>
    internal static class MeditationSpotCustomizer
    {
        /// <summary>
        /// Weighted outcomes for meditation spot customization.
        /// Lazily built to filter out DLC-gated outcomes when those DLCs aren't present.
        /// </summary>
        private static List<(float weight, Action<Thing, Map, Faction> action)> _outcomes;
        private static List<(float weight, Action<Thing, Map, Faction> action)> Outcomes => _outcomes ?? (_outcomes = BuildOutcomes());

        private static List<(float weight, Action<Thing, Map, Faction> action)> BuildOutcomes()
        {
            var outcomes = new List<(float weight, Action<Thing, Map, Faction> action)>
            {
                (40f, (spot, map, faction) => spot.Destroy(DestroyMode.Vanish)),
                (3f,  (spot, map, faction) => ReplaceWithMech(spot, Things.HunterDroneTrap, map)),
                (3f,  (spot, map, faction) => ReplaceWithMech(spot, Things.WaspDroneTrap, map)),
                (5f,  (spot, map, faction) => TrySpawnHeater(spot, map)),
                (7f,  (spot, map, faction) => SpawnPetWithKibble(spot, map)),
                (1f,  (spot, map, faction) => TrySpawnGameOfUr(spot, map)),
                (2f,  (spot, map, faction) => TrySpawnHorseshoePin(spot, map)),
                (4f,  (spot, map, faction) => TrySpawnPlantPot(spot, map)),
                (33f, (spot, map, faction) => { }) // Keep as-is
            };

            // Biotech DLC - Militor
            if (PawnKinds.Mech_Militor != null)
                outcomes.Add((2f, (spot, map, faction) => SpawnMechAtPosition(spot, PawnKinds.Mech_Militor, map, faction)));

            // Anomaly DLC - Shambler
            if (PawnKinds.ShamblerSwarmer != null)
                outcomes.Add((2f, (spot, map, faction) => SpawnShamblerAtPosition(spot, PawnKinds.ShamblerSwarmer, map)));

            // VFE Spacer - Interactive Table 1x1
            if (Things.Table_interactive_1x1c != null)
                outcomes.Add((5f, (spot, map, faction) => CrewQuartersHelpers.ReplaceThingAt(spot, Things.Table_interactive_1x1c, null, map)));

            // VFE Spacer - Air Purifier
            if (Things.VFES_AirPurifier != null)
                outcomes.Add((5f, (spot, map, faction) => CrewQuartersHelpers.ReplaceThingAt(spot, Things.VFES_AirPurifier, null, map)));

            return outcomes;
        }

        /// <summary>
        /// Finds and customizes meditation spots in subrooms.
        /// Each spot has various replacement/removal chances.
        /// </summary>
        internal static void Customize(Map map, List<CellRect> subroomRects, Faction faction)
        {
            if (Things.MeditationSpot == null) return;

            // Find all meditation spots in subroom areas
            List<Thing> meditationSpots = new List<Thing>();
            foreach (CellRect subroomRect in subroomRects)
            {
                foreach (IntVec3 cell in subroomRect)
                {
                    if (!cell.InBounds(map)) continue;
                    foreach (Thing thing in cell.GetThingList(map).ToList())
                    {
                        if (thing.def == Things.MeditationSpot)
                        {
                            meditationSpots.Add(thing);
                        }
                    }
                }
            }

            foreach (Thing spot in meditationSpots)
            {
                var (_, action) = Outcomes.RandomElementByWeight(x => x.weight);
                action(spot, map, faction);
            }
        }

        private static void TrySpawnHeater(Thing spot, Map map)
        {
            if (Things.Heater == null) return;
            CrewQuartersHelpers.ReplaceThingAt(spot, Things.Heater, null, map);
        }

        private static void TrySpawnGameOfUr(Thing spot, Map map)
        {
            if (Things.GameOfUrBoard == null) return;
            CrewQuartersHelpers.ReplaceThingAt(spot, Things.GameOfUrBoard, Things.Steel, map);
        }

        private static void TrySpawnHorseshoePin(Thing spot, Map map)
        {
            if (Things.HorseshoesPin == null) return;
            CrewQuartersHelpers.ReplaceThingAt(spot, Things.HorseshoesPin, Things.Steel, map);
        }

        private static void TrySpawnPlantPot(Thing spot, Map map)
        {
            if (Things.PlantPot == null) return;
            CrewQuartersHelpers.ReplaceThingAt(spot, Things.PlantPot, Things.Steel, map);
        }

        /// <summary>
        /// Replaces a meditation spot with a mech (drone).
        /// </summary>
        private static void ReplaceWithMech(Thing spot, ThingDef mechDef, Map map)
        {
            IntVec3 pos = spot.Position;
            spot.Destroy(DestroyMode.Vanish);

            // Spawn mech thing (dormant drone)
            Thing mech = ThingMaker.MakeThing(mechDef);
            GenSpawn.Spawn(mech, pos, map);
        }

        /// <summary>
        /// Replaces a meditation spot with a mech pawn.
        /// </summary>
        private static void SpawnMechAtPosition(Thing spot, PawnKindDef mechKind, Map map, Faction faction)
        {
            IntVec3 pos = spot.Position;
            spot.Destroy(DestroyMode.Vanish);

            Pawn mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: mechKind,
                faction: faction,
                context: PawnGenerationContext.NonPlayer,
                tile: map.Tile));

            GenSpawn.Spawn(mech, pos, map);
        }

        /// <summary>
        /// Replaces a meditation spot with a shambler (factionless).
        /// </summary>
        private static void SpawnShamblerAtPosition(Thing spot, PawnKindDef shamblerKind, Map map)
        {
            IntVec3 pos = spot.Position;
            spot.Destroy(DestroyMode.Vanish);

            Pawn shambler = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: shamblerKind,
                faction: null,
                context: PawnGenerationContext.NonPlayer,
                tile: map.Tile));

            GenSpawn.Spawn(shambler, pos, map);
        }

        /// <summary>
        /// Available pet kinds for spawning. Filtered at static construction to remove nulls.
        /// </summary>
        private static List<PawnKindDef> _petKinds;
        private static List<PawnKindDef> PetKinds => _petKinds ?? (_petKinds = BuildPetKindsList());

        private static List<PawnKindDef> BuildPetKindsList()
        {
            return new List<PawnKindDef>
            {
                PawnKinds.Cat,
                PawnKinds.Husky,
                PawnKinds.Labrador,
                PawnKinds.YorkshireTerrier
            }.Where(k => k != null).ToList();
        }

        /// <summary>
        /// Replaces a meditation spot with an animal bed and spawns a pet.
        /// Adds kibble to the nearest reachable small shelf.
        /// </summary>
        private static void SpawnPetWithKibble(Thing spot, Map map)
        {
            IntVec3 pos = spot.Position;
            Rot4 rot = spot.Rotation;
            spot.Destroy(DestroyMode.Vanish);

            // Spawn animal bed
            if (Things.AnimalBed != null)
            {
                Thing bed = ThingMaker.MakeThing(Things.AnimalBed, Things.Steel);
                GenSpawn.Spawn(bed, pos, map, rot);
            }

            // Spawn random cat or vanilla dog (factionless)
            if (PetKinds.Count > 0)
            {
                PawnKindDef petKind = PetKinds.RandomElement();
                Pawn pet = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind: petKind,
                    faction: null,
                    context: PawnGenerationContext.NonPlayer,
                    tile: map.Tile));

                GenSpawn.Spawn(pet, pos, map);
            }

            // Find nearest reachable small shelf and add kibble
            if (Things.ShelfSmall != null)
            {
                Thing nearestShelf = GenClosest.ClosestThingReachable(
                    pos,
                    map,
                    ThingRequest.ForDef(Things.ShelfSmall),
                    PathEndMode.Touch,
                    TraverseParms.For(TraverseMode.PassDoors),
                    maxDistance: 20f);

                if (nearestShelf is Building_Storage shelf)
                {
                    RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Kibble, Rand.RangeInclusive(45, 75));
                }
            }
        }
    }
}
