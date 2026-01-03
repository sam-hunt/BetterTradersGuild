using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using BetterTradersGuild.Helpers.RoomContents;

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
                (3f,  (spot, map, faction) => ReplaceWithMech(spot, CrewQuartersHelpers.HunterDroneDef, map)),
                (3f,  (spot, map, faction) => ReplaceWithMech(spot, CrewQuartersHelpers.WaspDroneDef, map)),
                (5f,  (spot, map, faction) => TrySpawnHeater(spot, map)),
                (7f,  (spot, map, faction) => SpawnPetWithKibble(spot, map)),
                (1f,  (spot, map, faction) => TrySpawnGameOfUr(spot, map)),
                (2f,  (spot, map, faction) => TrySpawnHorseshoePin(spot, map)),
                (4f,  (spot, map, faction) => TrySpawnPlantPot(spot, map)),
                (33f, (spot, map, faction) => { }) // Keep as-is
            };

            // Biotech DLC - Militor
            if (CrewQuartersHelpers.MilitorKind != null)
                outcomes.Add((2f, (spot, map, faction) => SpawnMechAtPosition(spot, CrewQuartersHelpers.MilitorKind, map, faction)));

            // Anomaly DLC - Shambler
            if (CrewQuartersHelpers.ShamblerKind != null)
                outcomes.Add((2f, (spot, map, faction) => SpawnShamblerAtPosition(spot, CrewQuartersHelpers.ShamblerKind, map)));

            // VFE Spacer - Interactive Table 1x1
            if (CrewQuartersHelpers.InteractiveTableDef != null)
                outcomes.Add((5f, (spot, map, faction) => CrewQuartersHelpers.ReplaceThingAt(spot, CrewQuartersHelpers.InteractiveTableDef, null, map)));

            // VFE Spacer - Air Purifier
            if (CrewQuartersHelpers.AirPurifierDef != null)
                outcomes.Add((5f, (spot, map, faction) => CrewQuartersHelpers.ReplaceThingAt(spot, CrewQuartersHelpers.AirPurifierDef, null, map)));

            return outcomes;
        }

        /// <summary>
        /// Finds and customizes meditation spots in subrooms.
        /// Each spot has various replacement/removal chances.
        /// </summary>
        internal static void Customize(Map map, List<CellRect> subroomRects, Faction faction)
        {
            ThingDef meditationSpotDef = DefDatabase<ThingDef>.GetNamed("MeditationSpot", false);
            if (meditationSpotDef == null) return;

            // Find all meditation spots in subroom areas
            List<Thing> meditationSpots = new List<Thing>();
            foreach (CellRect subroomRect in subroomRects)
            {
                foreach (IntVec3 cell in subroomRect)
                {
                    if (!cell.InBounds(map)) continue;
                    foreach (Thing thing in cell.GetThingList(map).ToList())
                    {
                        if (thing.def == meditationSpotDef)
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
            ThingDef heaterDef = DefDatabase<ThingDef>.GetNamed("Heater", false);
            if (heaterDef == null) return;
            CrewQuartersHelpers.ReplaceThingAt(spot, heaterDef, null, map);
        }

        private static void TrySpawnGameOfUr(Thing spot, Map map)
        {
            ThingDef gameOfUrDef = DefDatabase<ThingDef>.GetNamed("GameOfUrBoard", false);
            if (gameOfUrDef == null) return;
            CrewQuartersHelpers.ReplaceThingAt(spot, gameOfUrDef, CrewQuartersHelpers.SteelDef, map);
        }

        private static void TrySpawnHorseshoePin(Thing spot, Map map)
        {
            ThingDef horseshoeDef = DefDatabase<ThingDef>.GetNamed("HorseshoesPin", false);
            if (horseshoeDef == null) return;
            CrewQuartersHelpers.ReplaceThingAt(spot, horseshoeDef, CrewQuartersHelpers.SteelDef, map);
        }

        private static void TrySpawnPlantPot(Thing spot, Map map)
        {
            ThingDef plantPotDef = DefDatabase<ThingDef>.GetNamed("PlantPot", false);
            if (plantPotDef == null) return;
            CrewQuartersHelpers.ReplaceThingAt(spot, plantPotDef, CrewQuartersHelpers.SteelDef, map);
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
        /// Replaces a meditation spot with an animal bed and spawns a pet.
        /// Adds kibble to the nearest reachable small shelf.
        /// </summary>
        private static void SpawnPetWithKibble(Thing spot, Map map)
        {
            IntVec3 pos = spot.Position;
            Rot4 rot = spot.Rotation;
            spot.Destroy(DestroyMode.Vanish);

            // Spawn animal bed
            ThingDef animalBedDef = DefDatabase<ThingDef>.GetNamed("AnimalBed", false);
            if (animalBedDef != null)
            {
                Thing bed = ThingMaker.MakeThing(animalBedDef, CrewQuartersHelpers.SteelDef);
                GenSpawn.Spawn(bed, pos, map, rot);
            }

            // Spawn random cat or vanilla dog (factionless)
            string[] petKinds = { "Cat", "Husky", "Labrador", "YorkshireTerrier" };
            PawnKindDef petKind = DefDatabase<PawnKindDef>.GetNamed(petKinds.RandomElement(), false);
            if (petKind != null)
            {
                Pawn pet = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind: petKind,
                    faction: null,
                    context: PawnGenerationContext.NonPlayer,
                    tile: map.Tile));

                GenSpawn.Spawn(pet, pos, map);
            }

            // Find nearest reachable small shelf and add kibble
            if (CrewQuartersHelpers.ShelfSmallDef != null)
            {
                Thing nearestShelf = GenClosest.ClosestThingReachable(
                    pos,
                    map,
                    ThingRequest.ForDef(CrewQuartersHelpers.ShelfSmallDef),
                    PathEndMode.Touch,
                    TraverseParms.For(TraverseMode.PassDoors),
                    maxDistance: 20f);

                if (nearestShelf is Building_Storage shelf)
                {
                    RoomShelfHelper.AddItemsToShelf(map, shelf, "Kibble", Rand.RangeInclusive(45, 75));
                }
            }
        }
    }
}
