using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using BetterTradersGuild.DefRefs;

namespace BetterTradersGuild.RoomContents.Nursery
{
    /// <summary>
    /// Spawns civilians (a caretaker and their children) sheltering inside the nursery crib subroom.
    /// These represent non-combatants who have locked themselves behind the blast door.
    /// </summary>
    public static class ShelteringCivilianSpawner
    {
        /// <summary>
        /// Weighted developmental stages for young pawn generation.
        /// Slight skew towards younger stages for variety - pure random by age
        /// would give ~77% Child (10-year span) vs ~23% Baby/Newborn (~1 year combined).
        ///
        /// Distribution: ~17% Newborn, ~33% Baby, ~50% Child
        /// </summary>
        private static readonly List<(DevelopmentalStage stage, float weight, FloatRange ageRange)> YoungStageWeights =
            new List<(DevelopmentalStage, float, FloatRange)>
        {
            (DevelopmentalStage.Newborn, 1f, new FloatRange(0.01f, 0.9f)),    // 3-36 days old
            (DevelopmentalStage.Baby, 2f, new FloatRange(1f, 2.8f)),     // 1-3 years
            (DevelopmentalStage.Child, 3f, new FloatRange(3f, 12.8f)),        // 3-13 years
        };

        /// <summary>
        /// Spawns civilians sheltering inside the crib subroom.
        ///
        /// Spawn counts:
        /// - 1 TradersGuild_Citizen (caretaker/parent)
        /// - 2-4 young pawns (Newborn/Baby/Child mix, requires Biotech DLC)
        ///
        /// Placement strategy:
        /// - Newborns and Babies are placed in cribs with a LayDown job
        /// - Children (who can walk) are spawned at standable cells
        /// - Caretaker is spawned at a standable cell
        ///
        /// Each young pawn is assigned the caretaker as their parent for realism.
        /// </summary>
        public static void SpawnShelteringCivilians(Map map, Faction faction, CellRect subroomRect)
        {
            // Get standable cells inside the subroom (avoid walls, doors, furniture)
            List<IntVec3> standableCells = subroomRect.Cells
                .Where(c => c.InBounds(map) && c.Standable(map))
                .ToList();

            if (standableCells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No standable cells in nursery subroom for civilians.");
                return;
            }

            // Find available cribs in the subroom (beds sized for babies)
            List<Building_Bed> availableCribs = subroomRect.Cells
                .Where(c => c.InBounds(map))
                .SelectMany(c => c.GetThingList(map))
                .OfType<Building_Bed>()
                .Where(bed => bed.ForHumanBabies && bed.AnyUnownedSleepingSlot)
                .Distinct()
                .ToList();

            // Determine spawn counts
            int childCount = Rand.RangeInclusive(2, 4);

            // Get PawnKindDefs from centralized DefRefs
            PawnKindDef citizenKind = PawnKinds.TradersGuild_Citizen;
            PawnKindDef childKind = PawnKinds.TradersGuild_Child;

            int spawned = 0;
            Pawn caretaker = null;

            // Spawn caretaker (parent of the children)
            if (citizenKind != null && standableCells.Count > 0)
            {
                caretaker = GenerateCivilian(citizenKind, faction, map.Tile);
                if (caretaker != null)
                {
                    IntVec3 cell = standableCells.RandomElement();
                    GenSpawn.Spawn(caretaker, cell, map);
                    standableCells.Remove(cell);
                    spawned++;
                }
            }

            // Spawn young pawns (Newborn/Baby/Child) - requires Biotech DLC
            // Each child is assigned the caretaker as their parent
            if (childKind != null)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Pawn youngPawn = GenerateYoungPawn(childKind, faction, map.Tile);
                    if (youngPawn == null)
                        continue;

                    // Assign caretaker as parent for realism
                    if (caretaker != null)
                    {
                        youngPawn.relations.AddDirectRelation(PawnRelations.Parent, caretaker);
                    }

                    // Check if this is a newborn or baby (can't walk - should be in crib)
                    bool needsCrib = youngPawn.DevelopmentalStage == DevelopmentalStage.Newborn ||
                                     youngPawn.DevelopmentalStage == DevelopmentalStage.Baby;

                    if (needsCrib && availableCribs.Count > 0)
                    {
                        // Place in crib
                        if (TrySpawnPawnInCrib(youngPawn, availableCribs, map))
                        {
                            spawned++;
                            continue;
                        }
                        // If crib placement failed, fall through to standable cell
                    }

                    // Spawn at standable cell (for children who can walk, or if no cribs available)
                    if (standableCells.Count > 0)
                    {
                        IntVec3 cell = standableCells.RandomElement();
                        GenSpawn.Spawn(youngPawn, cell, map);
                        standableCells.Remove(cell);
                        spawned++;
                    }
                    else
                    {
                        // No space left - destroy the pawn to avoid orphaned pawns
                        youngPawn.Destroy();
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to spawn a pawn in an available crib with a LayDown job.
        /// The pawn is spawned at the crib's sleeping position and given a resting job.
        /// </summary>
        /// <param name="pawn">The pawn to place in the crib (should be a newborn or baby)</param>
        /// <param name="availableCribs">List of cribs with available slots (will be modified)</param>
        /// <param name="map">The map to spawn on</param>
        /// <returns>True if successfully placed in a crib, false otherwise</returns>
        private static bool TrySpawnPawnInCrib(Pawn pawn, List<Building_Bed> availableCribs, Map map)
        {
            if (availableCribs.Count == 0)
                return false;

            // Select a random available crib
            Building_Bed crib = availableCribs.RandomElement();

            // Get the sleeping position for this crib
            IntVec3 sleepPos = RestUtility.GetBedSleepingSlotPosFor(pawn, crib);

            // Verify the position is valid
            if (!sleepPos.InBounds(map)) return false;

            // Spawn the pawn at the sleeping position
            GenSpawn.Spawn(pawn, sleepPos, map);

            // Assign the crib to this pawn (ownership)
            var compAssignable = crib.TryGetComp<CompAssignableToPawn>();
            compAssignable?.TryAssignPawn(pawn);

            // Start a LayDown job so the pawn appears to be lying in the crib
            Job layDownJob = JobMaker.MakeJob(Jobs.LayDownResting, crib);
            pawn.jobs.StartJob(layDownJob, JobCondition.None, null, resumeCurJobAfterwards: false, cancelBusyStances: true);

            // Remove crib from available list if it no longer has free slots
            if (!crib.AnyUnownedSleepingSlot)
            {
                availableCribs.Remove(crib);
            }

            return true;
        }

        /// <summary>
        /// Generates a single adult civilian pawn for the TradersGuild faction.
        /// </summary>
        private static Pawn GenerateCivilian(PawnKindDef kindDef, Faction faction, int tile)
        {
            if (kindDef == null || faction == null)
                return null;

            try
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: kindDef,
                    faction: faction,
                    context: PawnGenerationContext.NonPlayer,
                    tile: tile,
                    forceGenerateNewPawn: false,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: false,
                    colonistRelationChanceFactor: 0f,
                    forceAddFreeWarmLayerIfNeeded: false,
                    allowGay: true,
                    allowPregnant: true,
                    allowFood: true,
                    allowAddictions: true,
                    inhabitant: true
                );

                return PawnGenerator.GeneratePawn(request);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Better Traders Guild] Failed to generate civilian ({kindDef.defName}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a young pawn (Newborn, Baby, or Child) for the TradersGuild faction.
        /// Uses weighted random selection to provide variety while slightly favoring younger stages.
        ///
        /// Must explicitly specify DevelopmentalStage since the PawnKindDef's
        /// pawnGroupDevelopmentStage field only applies to group generation, not individual spawns.
        /// </summary>
        private static Pawn GenerateYoungPawn(PawnKindDef kindDef, Faction faction, int tile)
        {
            if (kindDef == null || faction == null)
                return null;

            try
            {
                // Select developmental stage with weighted random
                var stageChoice = YoungStageWeights.RandomElementByWeight(x => x.weight);

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: kindDef,
                    faction: faction,
                    context: PawnGenerationContext.NonPlayer,
                    tile: tile,
                    forceGenerateNewPawn: false,
                    allowDead: false,
                    allowDowned: true,
                    canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: false,
                    colonistRelationChanceFactor: 0f,
                    forceAddFreeWarmLayerIfNeeded: false,
                    allowGay: true,
                    allowPregnant: false,
                    allowFood: true,
                    allowAddictions: false,
                    inhabitant: true,
                    developmentalStages: stageChoice.stage,
                    biologicalAgeRange: stageChoice.ageRange
                );

                return PawnGenerator.GeneratePawn(request);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Better Traders Guild] Failed to generate young pawn ({kindDef.defName}): {ex.Message}");
                return null;
            }
        }
    }
}
