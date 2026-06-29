using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.LordJobs.Civilians;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Nursery
{
    // Spawns civilians (a caretaker and their children) sheltering inside the nursery crib subroom.
    // These represent non-combatants who have locked themselves behind the blast door.
    //
    // When entrenched-defender AI is enabled the spawned caretaker + walking children are
    // attached to a LordJob_BTGShelterCivilians (shelter -> escape -> stranded); see
    // CivilianLords. Infants/babies are left autonomous in their cribs (the lord's jobgivers
    // tend and, during escape, carry them by faction scan). When the setting is off they stay
    // lordless, but the crib-placement fix, spawn rebalance, and the caretaker's knife below
    // still apply - they are plain improvements, not AI.
    public static class ShelteringCivilianSpawner
    {
        // Walking children span the Child developmental stage (roughly 3-13 years).
        private static readonly FloatRange ChildAgeRange = new FloatRange(3f, 12.8f);

        // Weighted stages for the infants that need carrying (cannot walk). Mirrors the old
        // distribution's skew toward Babies over Newborns; Child is excluded here (children
        // are spawned separately as walkers).
        private static readonly List<(DevelopmentalStage stage, float weight, FloatRange ageRange)> InfantStageWeights =
            new List<(DevelopmentalStage, float, FloatRange)>
        {
            (DevelopmentalStage.Newborn, 1f, new FloatRange(0.1f, 0.9f)),    // ~3-36 days old
            (DevelopmentalStage.Baby, 2f, new FloatRange(1f, 2.8f)),         // 1-3 years
        };

        // Spawns civilians sheltering inside the crib subroom.
        //
        // Composition:
        // - 1 TradersGuild_Citizen caretaker (the carrier/pilot/guard), armed with a super-
        //   quality plasteel knife and wearing a masterwork shield belt.
        // - 1-3 TradersGuild_Child walking children.
        // - 0..(carriers) infants (Newborn/Baby), each tucked into a crib. Carriers = the lone
        //   caretaker + the children, so infants NEVER outnumber the carriers: during an escape
        //   every infant has a walker free to carry it. Also capped by available cribs.
        //
        // Returns every spawned pawn (caretaker + children + infants) so callers can tailor the
        // room to its occupants (e.g. stocking food to match who lives here).
        public static List<Pawn> SpawnShelteringCivilians(Map map, Faction faction, CellRect subroomRect)
        {
            List<Pawn> spawnedPawns = new List<Pawn>();
            // Lord members: the caretaker and walking children (NOT the autonomous infants).
            List<Pawn> walkers = new List<Pawn>();

            // Get standable cells inside the subroom (avoid walls, doors, furniture)
            List<IntVec3> standableCells = subroomRect.Cells
                .Where(c => c.InBounds(map) && c.Standable(map))
                .ToList();

            if (standableCells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No standable cells in nursery subroom for civilians.");
                return spawnedPawns;
            }

            // Find available cribs in the subroom (beds sized for babies)
            List<Building_Bed> availableCribs = subroomRect.Cells
                .Where(c => c.InBounds(map))
                .SelectMany(c => c.GetThingList(map))
                .OfType<Building_Bed>()
                .Where(bed => bed.ForHumanBabies && bed.AnyUnownedSleepingSlot)
                .Distinct()
                .ToList();

            // Spawn-count rule: carriers = 1 caretaker + children; infants <= carriers (so an
            // escape always has a walker free for every infant), and also <= free cribs.
            int childCount = Rand.RangeInclusive(1, 3);
            int carrierCount = 1 + childCount;
            int maxInfants = availableCribs.Count < carrierCount ? availableCribs.Count : carrierCount;
            int infantCount = maxInfants > 0 ? Rand.RangeInclusive(1, maxInfants) : 0;

            PawnKindDef citizenKind = PawnKinds.TradersGuild_Citizen;
            PawnKindDef childKind = PawnKinds.TradersGuild_Child;

            Pawn caretaker = null;

            // Caretaker (parent of the children), armed with a super-quality plasteel knife.
            if (citizenKind != null)
            {
                caretaker = GenerateCivilian(citizenKind, faction, map.Tile);
                if (caretaker != null)
                {
                    IntVec3 cell = standableCells.RandomElement();
                    standableCells.Remove(cell);
                    GenSpawn.Spawn(caretaker, cell, map);
                    EquipSuperPlasteelKnife(caretaker);
                    EquipMasterworkShieldBelt(caretaker);
                    spawnedPawns.Add(caretaker);
                    walkers.Add(caretaker);
                }
            }

            // Walking children (Child stage). Each child is assigned the caretaker as parent.
            if (childKind != null)
            {
                for (int i = 0; i < childCount; i++)
                {
                    if (standableCells.Count == 0)
                        break;

                    Pawn child = GenerateYoungPawn(childKind, faction, map.Tile, DevelopmentalStage.Child, ChildAgeRange);
                    if (child == null)
                        continue;

                    if (caretaker != null)
                        child.relations.AddDirectRelation(PawnRelations.Parent, caretaker);

                    IntVec3 cell = standableCells.RandomElement();
                    standableCells.Remove(cell);
                    GenSpawn.Spawn(child, cell, map);
                    spawnedPawns.Add(child);
                    walkers.Add(child);
                }
            }

            // Infants (Newborn/Baby) - cannot walk, so they are tucked into cribs.
            if (childKind != null)
            {
                for (int i = 0; i < infantCount; i++)
                {
                    if (availableCribs.Count == 0)
                        break;

                    var stageChoice = InfantStageWeights.RandomElementByWeight(x => x.weight);
                    Pawn infant = GenerateYoungPawn(childKind, faction, map.Tile, stageChoice.stage, stageChoice.ageRange);
                    if (infant == null)
                        continue;

                    if (caretaker != null)
                        infant.relations.AddDirectRelation(PawnRelations.Parent, caretaker);

                    if (TryTuckInfantIntoCrib(infant, availableCribs, map))
                    {
                        spawnedPawns.Add(infant);
                    }
                    else if (standableCells.Count > 0)
                    {
                        // Crib placement failed: fall back to a standable cell.
                        IntVec3 cell = standableCells.RandomElement();
                        standableCells.Remove(cell);
                        GenSpawn.Spawn(infant, cell, map);
                        spawnedPawns.Add(infant);
                    }
                    else
                    {
                        // No space left - destroy to avoid an orphaned, unspawned pawn.
                        infant.Destroy();
                    }
                }
            }

            // Attach the caretaker + children to the sheltering lord (gated on the entrenched-
            // defender setting). Infants stay autonomous in their cribs.
            CivilianLords.MakeShelterLordIfEnabled(map, faction, subroomRect.CenterCell, walkers);

            return spawnedPawns;
        }

        // Tucks an infant into an available crib so it spawns correctly slotted and resting.
        //
        // Uses RestUtility.TuckIntoBed, the canonical path (teleports to the sleeping slot,
        // sets PawnPosture.InBed, and starts the laydown with continueSleeping). This fixes the
        // old approach of spawning then issuing a raw LayDown job, which sometimes left the
        // infant on the floor or downed-on-crib instead of properly slotted.
        private static bool TryTuckInfantIntoCrib(Pawn infant, List<Building_Bed> availableCribs, Map map)
        {
            if (availableCribs.Count == 0)
                return false;

            Building_Bed crib = availableCribs.RandomElement();
            IntVec3 sleepPos = RestUtility.GetBedSleepingSlotPosFor(infant, crib);
            if (!sleepPos.InBounds(map))
                return false;

            GenSpawn.Spawn(infant, sleepPos, map);
            crib.TryGetComp<CompAssignableToPawn>()?.TryAssignPawn(infant);
            RestUtility.TuckIntoBed(crib, infant, infant, rescued: false);

            if (!crib.AnyUnownedSleepingSlot)
                availableCribs.Remove(crib);

            return true;
        }

        // Equips the caretaker with a super-quality plasteel knife, replacing any weapon the
        // pawnkind generated, so the lone guardian is the one who wields it.
        private static void EquipSuperPlasteelKnife(Pawn pawn)
        {
            if (pawn?.equipment == null)
                return;

            ThingDef knifeDef = Things.MeleeWeapon_Knife;
            ThingDef plasteel = Things.Plasteel;
            if (knifeDef == null || plasteel == null)
                return;

            ThingWithComps knife = (ThingWithComps)ThingMaker.MakeThing(knifeDef, plasteel);
            knife.TryGetComp<CompQuality>()?.SetQuality(QualityUtility.GenerateQualitySuper(), ArtGenerationContext.Outsider);

            if (pawn.equipment.Primary != null)
                pawn.equipment.DestroyAllEquipment();
            pawn.equipment.AddEquipment(knife);
        }

        // Equips the caretaker with a masterwork shield belt so the lone guardian can absorb
        // incoming fire while sheltering the children.
        private static void EquipMasterworkShieldBelt(Pawn pawn)
        {
            if (pawn?.apparel == null)
                return;

            ThingDef shieldBeltDef = Things.Apparel_ShieldBelt;
            if (shieldBeltDef == null)
                return;

            Apparel shieldBelt = (Apparel)ThingMaker.MakeThing(shieldBeltDef);
            shieldBelt.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Masterwork, ArtGenerationContext.Outsider);

            pawn.apparel.Wear(shieldBelt, dropReplacedApparel: false);
        }

        // Generates a single adult civilian pawn for the TradersGuild faction.
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
                    mustBeCapableOfViolence: true,
                    colonistRelationChanceFactor: 0f,
                    forceAddFreeWarmLayerIfNeeded: false,
                    allowGay: true,
                    allowPregnant: true,
                    allowFood: true,
                    allowAddictions: false,
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

        // Generates a young pawn at an explicit developmental stage and age range.
        //
        // Must explicitly specify DevelopmentalStage since the PawnKindDef's
        // pawnGroupDevelopmentStage field only applies to group generation, not individual spawns.
        private static Pawn GenerateYoungPawn(PawnKindDef kindDef, Faction faction, int tile, DevelopmentalStage stage, FloatRange ageRange)
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
                    developmentalStages: stage,
                    biologicalAgeRange: ageRange
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
