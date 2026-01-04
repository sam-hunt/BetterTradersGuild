using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Helper for spawning pets in rooms.
    /// Provides weighted random pet selection (50% cats, 50% dogs).
    /// </summary>
    internal static class RoomPetHelper
    {
        /// <summary>
        /// Weighted pet kinds for spawning.
        /// Cat has weight 6, each dog subtype has weight 2, giving 50/50 cat/dog split.
        /// Lazily built to filter out null PawnKindDefs.
        /// </summary>
        private static List<(float weight, PawnKindDef kind)> _weightedPetKinds;
        private static List<(float weight, PawnKindDef kind)> WeightedPetKinds =>
            _weightedPetKinds ?? (_weightedPetKinds = BuildWeightedPetKindsList());

        private static List<(float weight, PawnKindDef kind)> BuildWeightedPetKindsList()
        {
            var candidates = new List<(float weight, PawnKindDef kind)>
            {
                (6f, PawnKinds.Cat),
                (2f, PawnKinds.Husky),
                (2f, PawnKinds.LabradorRetriever),
                (2f, PawnKinds.YorkshireTerrier)
            };

            return candidates.Where(x => x.kind != null).ToList();
        }

        /// <summary>
        /// Spawns a random pet (cat or dog) at the specified position.
        /// Pet is spawned factionless.
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="position">The position to spawn the pet.</param>
        /// <returns>The spawned pet, or null if no valid pet kinds available.</returns>
        public static Pawn SpawnPetAtPosition(Map map, IntVec3 position)
        {
            if (WeightedPetKinds.Count == 0)
                return null;

            var (_, petKind) = WeightedPetKinds.RandomElementByWeight(x => x.weight);

            Pawn pet = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: petKind,
                faction: null,
                context: PawnGenerationContext.NonPlayer,
                tile: map.Tile));

            GenSpawn.Spawn(pet, position, map);
            return pet;
        }

        /// <summary>
        /// Adds kibble to the nearest reachable small shelf within range.
        /// </summary>
        /// <param name="map">The map to search.</param>
        /// <param name="position">The position to search from.</param>
        /// <param name="amount">Amount of kibble to add. If null, uses random 45-75.</param>
        /// <param name="maxDistance">Maximum search distance (default 10).</param>
        public static void AddKibbleToNearestShelf(Map map, IntVec3 position, int? amount = null, float maxDistance = 10f)
        {
            if (Things.ShelfSmall == null || Things.Kibble == null)
                return;

            Thing nearestShelf = GenClosest.ClosestThingReachable(
                position,
                map,
                ThingRequest.ForDef(Things.ShelfSmall),
                PathEndMode.Touch,
                TraverseParms.For(TraverseMode.PassDoors),
                maxDistance: maxDistance);

            if (nearestShelf is Building_Storage shelf)
            {
                int kibbleAmount = amount ?? Rand.RangeInclusive(45, 75);
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Kibble, kibbleAmount);
            }
        }
    }
}
