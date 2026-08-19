using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.LordJobs;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.MapGeneration
{
    // Drop-in replacement for vanilla GenStep_SettlementPawnsLoot that attaches
    // the generated defender group to a bounded LordJob_BTGDefendStructure
    // instead of LordJob_DefendBase, and owns the garrison's points budget
    // (quest-driven on sites, settings-driven on settlements).
    //
    // Vanilla bakes the LordJob into GenStep_SettlementPawnsLoot.Generate with no
    // virtual hook, so the only way to swap the defenders' lord is to override
    // Generate wholesale. The body mirrors vanilla exactly (same SpawnRect lookup,
    // same MapGenUtility.GeneratePawns / GenerateLoot calls, same loot gating) so
    // pawn generation, placement, and loot behaviour are unchanged — the
    // differences are the LordJob handed to the spawned defenders and the
    // explicit points budget.
    //
    // SeedPart is inherited unchanged: neither LordJob ctor nor LordMaker.MakeNewLord
    // consumes Rand before GeneratePawns runs, so the generated pawns are identical
    // to what vanilla would produce for the same seed.
    //
    // This GenStep runs in two pipelines: the BTG settlement pipeline
    // (BTG_SettlementMapGenerator) and, via a linkWithSite GenStepDef, on the
    // smuggler's den quest site. Neither needs a TradersGuild-map guard.
    public class GenStep_BTGSettlementPawns : GenStep_SettlementPawnsLoot
    {
        public override void Generate(Map map, GenStepParams parms)
        {
            // Quest sites deliver their difficulty budget here: a GenStepDef linked to
            // a SitePartDef (linkWithSite) receives that part in parms, carrying the
            // threatPoints the quest computed. Steps listed in a MapGeneratorDef's own
            // genSteps always get a null sitePart, so this stays null for settlements,
            // whose budget instead comes from the defender settings below.
            float? sitePoints = parms.sitePart?.parms?.threatPoints;
            if (sitePoints.HasValue && sitePoints.Value <= 0f)
                sitePoints = null;

            float? points = sitePoints ?? SettingsScaledPoints();

            // Opt-out: when entrenched defenders are disabled and nothing overrides
            // the points budget, defer entirely to vanilla GenStep_SettlementPawnsLoot,
            // which attaches the garrison to LordJob_DefendBase. base.Generate IS the
            // vanilla path this override otherwise mirrors, so pawn/loot output is
            // byte-for-byte identical. With an explicit budget this path is unusable
            // (vanilla would drop the points); the local body runs instead and only
            // swaps the lord back to LordJob_DefendBase.
            if (!BetterTradersGuildMod.Settings.useEntrenchedDefenders && !points.HasValue)
            {
                base.Generate(map, parms);
                return;
            }

            if (!MapGenerator.TryGetVar("SpawnRect", out CellRect spawnRect))
            {
                Log.Error("[Better Traders Guild] GenStep_BTGSettlementPawns tried to execute but no SpawnRect was found in the map generator. This CellRect must be set.");
                return;
            }

            Faction faction = GetFaction(map);

            if (generatePawns)
            {
                // The defender lord, routed through the shared factory so this and
                // the gestator-reinforcement site can't drift: BTG's bounded lord
                // (never assaults, never paths outside the structure footprint), or
                // vanilla's LordJob_DefendBase when entrenched defenders are off.
                LordJob lordJob = DefenderLords.MakeDefenderLordJob(faction, spawnRect.CenterCell);
                Lord lord = LordMaker.MakeNewLord(faction, lordJob, map);
                MapGenUtility.GeneratePawns(map, spawnRect, faction, lord, PawnGroupKindDefOf.Settlement, points: points, requiresRoof: requiresRoof);
            }

            // Mirror vanilla loot gating: a zero-width lootMarketValue (the BTG default)
            // skips loot entirely, but honour any non-zero range if it is ever set.
            if (!lootMarketValue.HasValue || !lootMarketValue.Value.IsZeros)
            {
                ThingSetMakerDef setMakerDef = lootThingSetMaker ?? faction.def.settlementLootMaker ?? ThingSetMakerDefOf.MapGen_AbandonedColonyStockpile;
                MapGenUtility.GenerateLoot(map, spawnRect, setMakerDef, lootMarketValue, null, faction, requiresRoof);
            }
        }

        // Settlement garrison points from the defender settings, or null when they are
        // all at vanilla defaults (a null lets MapGenUtility.GeneratePawns roll its own
        // flat range internally, exactly as vanilla would).
        //
        // WHY VANILLA NEVER SCALES SETTLEMENT DEFENDERS:
        // GenStep_SettlementPawnsLoot passes no points to MapGenUtility.GeneratePawns,
        // which then rolls the flat DefaultPawnsPoints range (1150-1600) - storyteller,
        // difficulty and colony wealth are never consulted. (Classic ground settlements
        // do the same via SymbolResolver_Settlement; only quest sites get real threat
        // points, via GenStepParams.sitePart.) So a late-game colony assaults the same
        // ~1400-point garrison a young colony would. That flat roll is intentional
        // vanilla design, not a bug - which is why the threat-level scaling here is
        // opt-in (scaleDefendersToThreatLevel, default off), preserving long-shipped
        // BTG behaviour for existing players.
        //
        // THE SCALING BASE MUST COME FROM THE WORLD, NOT THE MAP:
        // Same trap as the sentry drone fix (GenStep_SpawnSentryDrones): at generation
        // time the settlement map has no player pawns on it, so
        // DefaultThreatPointsNow(map) floors to the storyteller minimum.
        // DefaultThreatPointsNow(Find.World) sums player wealth across all maps and
        // caravans, giving the storyteller's real threat level.
        //
        // The points value determines both the total pawn budget AND MaxPawnCost (which
        // filters expensive pawn kinds), so raising it increases count and unlocks
        // stronger kinds.
        //
        // These settings only ever reach custom-layout settlements: this GenStep runs
        // in the BTG settlement pipeline (active only when useCustomLayouts is on) and
        // on quest sites, which take the sitePoints path instead. That scoping is
        // deliberate: custom layouts carry a higher total lootable value than vanilla
        // mapgen - stocked room contents, prefabs, and the cargo vault, rather than
        // vanilla's stacks of loot sprinkled on the ground - so stronger defenses are
        // priced against richer maps.
        private static float? SettingsScaledPoints()
        {
            bool scaleToThreatLevel = BetterTradersGuildMod.Settings.scaleDefendersToThreatLevel;
            float multiplier = BetterTradersGuildMod.Settings.threatPointsMultiplier;

            if (!scaleToThreatLevel && multiplier == 1.0f)
                return null;

            // Same base roll vanilla would make, then: optional floor-raise to the
            // world's threat points (max, so an early-game garrison never drops below
            // the vanilla baseline), then the multiplier.
            float points = MapGenUtilityReflection.DefaultPawnsPoints.RandomInRange;

            if (scaleToThreatLevel)
                points = System.Math.Max(points, StorytellerUtility.DefaultThreatPointsNow(Find.World));

            points *= multiplier;

            return points;
        }

        // Reimplementation of the base class's private GetFaction. Identical logic,
        // using the public factionDef field: explicit faction if set, otherwise the
        // map's parent faction, falling back to a random enemy faction.
        private Faction GetFaction(Map map)
        {
            if (factionDef != null)
                return Find.FactionManager.FirstFactionOfDef(factionDef);

            if (map.ParentFaction == null || map.ParentFaction == Faction.OfPlayer)
                return Find.FactionManager.RandomEnemyFaction();

            return map.ParentFaction;
        }
    }
}
