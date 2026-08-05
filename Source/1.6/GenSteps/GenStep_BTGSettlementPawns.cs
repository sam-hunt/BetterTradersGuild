using BetterTradersGuild.LordJobs;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.MapGeneration
{
    // Drop-in replacement for vanilla GenStep_SettlementPawnsLoot that attaches
    // the generated defender group to a bounded LordJob_BTGDefendStructure
    // instead of LordJob_DefendBase.
    //
    // Vanilla bakes the LordJob into GenStep_SettlementPawnsLoot.Generate with no
    // virtual hook, so the only way to swap the defenders' lord is to override
    // Generate wholesale. The body mirrors vanilla exactly (same SpawnRect lookup,
    // same MapGenUtility.GeneratePawns / GenerateLoot calls, same loot gating) so
    // pawn generation, placement, and loot behaviour are unchanged — the single
    // difference is the LordJob handed to the spawned defenders.
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
            // genSteps always get a null sitePart, so this stays null for settlements
            // and the flat vanilla points roll applies as before.
            float? sitePoints = parms.sitePart?.parms?.threatPoints;
            if (sitePoints.HasValue && sitePoints.Value <= 0f)
                sitePoints = null;

            // Opt-out: when entrenched defenders are disabled, defer entirely to
            // vanilla GenStep_SettlementPawnsLoot, which attaches the garrison to
            // LordJob_DefendBase. base.Generate IS the vanilla path this override
            // otherwise mirrors, so pawn/loot output is byte-for-byte identical.
            // Quest sites can't take this path (vanilla would drop sitePoints); they
            // keep the local body and only swap the lord back to LordJob_DefendBase.
            if (!BetterTradersGuildMod.Settings.useEntrenchedDefenders && !sitePoints.HasValue)
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
                // The bounded defender lord: never assaults, never paths outside the
                // structure footprint to chase intruders. Routed through the shared
                // factory so this and the gestator-reinforcement site can't drift.
                // With entrenched defenders off, mirror vanilla's lord exactly.
                LordJob lordJob = BetterTradersGuildMod.Settings.useEntrenchedDefenders
                    ? DefenderLords.MakeDefenderLordJob(faction, spawnRect.CenterCell)
                    : new LordJob_DefendBase(faction, spawnRect.CenterCell, 25000);
                Lord lord = LordMaker.MakeNewLord(faction, lordJob, map);
                MapGenUtility.GeneratePawns(map, spawnRect, faction, lord, PawnGroupKindDefOf.Settlement, points: sitePoints, requiresRoof: requiresRoof);
            }

            // Mirror vanilla loot gating: a zero-width lootMarketValue (the BTG default)
            // skips loot entirely, but honour any non-zero range if it is ever set.
            if (!lootMarketValue.HasValue || !lootMarketValue.Value.IsZeros)
            {
                ThingSetMakerDef setMakerDef = lootThingSetMaker ?? faction.def.settlementLootMaker ?? ThingSetMakerDefOf.MapGen_AbandonedColonyStockpile;
                MapGenUtility.GenerateLoot(map, spawnRect, setMakerDef, lootMarketValue, null, faction, requiresRoof);
            }
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
