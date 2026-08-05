using BetterTradersGuild.DefRefs;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace BetterTradersGuild.QuestNodes
{
    // QuestNode that creates a smuggler's den quest site.
    //
    // Creates a Site world object in memory with the BTG_SmugglersDenSite WorldObjectDef
    // (which has WorldObjectComp_QuestVault baked in). The site is placed on an orbital tile
    // near the nearest TG settlement.
    //
    // The site is NOT immediately added to the world. A QuestPart_SpawnWorldObject is created
    // that adds it when the quest is accepted (via quest.InitiateSignal). This ensures
    // the site only appears on the world map after the player accepts the quest.
    //
    // Also adds QuestPart_DestroyWorldObject for cleanup on quest end.
    public class QuestNode_BTG_SmugglersDen_CreateSite : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> storeAs;

        public SlateRef<Settlement> nearSettlement;

        protected override bool TestRunInt(Slate slate)
        {
            Settlement settlement = nearSettlement.GetValue(slate);
            if (settlement == null)
                return false;

            // Verify Salvagers faction exists
            Faction salvagers = Find.FactionManager.FirstFactionOfDef(Factions.Salvagers);
            if (salvagers == null)
                return false;

            // Verify we can find a tile for the site near the TG settlement
            return TileFinder.TryFindNewSiteTile(out _, settlement.Tile,
                minDist: 1, maxDist: 10, allowCaravans: false, canBeSpace: true);
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;

            Settlement settlement = nearSettlement.GetValue(slate);
            Faction salvagers = Find.FactionManager.FirstFactionOfDef(Factions.Salvagers);

            // Find orbital tile near the TG settlement
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile, settlement.Tile,
                minDist: 1, maxDist: 10, allowCaravans: false, canBeSpace: true))
            {
                Log.Error("[Better Traders Guild] QuestNode_BTG_SmugglersDen_CreateSite: Failed to find tile for site");
                return;
            }

            // Create site using custom WorldObjectDef (has WorldObjectComp_QuestVault)
            Site site = (Site)WorldObjectMaker.MakeWorldObject(WorldObjects.BTG_SmugglersDenSite);
            site.Tile = tile;
            site.SetFaction(salvagers);

            // Add the SitePart with threat points. SitePartParams has two points fields
            // and vanilla readers split between them: classic site code (GenStep_Outpost,
            // Site.ActualThreatPoints) reads threatPoints, while the Odyssey-era orbital
            // code (GenStep_OrbitalPlatform, BaseGenUtility.ScatterSentryDronesInMap)
            // reads points. Vanilla's Gravcore quest nodes set both to the same value;
            // mirror that so every reader sees the quest's budget.
            float threatPoints = slate.Get<float>("siteThreatPoints",
                StorytellerUtility.DefaultThreatPointsNow(Find.World));

            SitePartParams partParams = new SitePartParams
            {
                threatPoints = threatPoints,
                points = threatPoints,
            };
            site.AddPart(new SitePart(site, SiteParts.BTG_SmugglersDen, partParams));

            // Store in slate for other quest nodes
            string slotName = storeAs.GetValue(slate);
            if (!string.IsNullOrEmpty(slotName))
                slate.Set(slotName, site);

            // Defer site spawning to quest acceptance
            QuestPart_SpawnWorldObject spawnPart = new QuestPart_SpawnWorldObject
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID("Initiate"),
                worldObject = site
            };
            quest.AddPart(spawnPart);

            // Destroy site on quest end. Safe while the player is on the map:
            // QuestPart_DestroyWorldObject only flags forceRemoveWorldObjectWhenMapRemoved
            // for spawned MapParents with a loaded map.
            QuestPart_DestroyWorldObject destroyPart = new QuestPart_DestroyWorldObject();
            destroyPart.worldObject = site;
            destroyPart.inSignal = QuestGenUtility.HardcodedSignalWithQuestID("End");
            quest.AddPart(destroyPart);

            // Register the site's quest target tag so signals like "site.AllEnemiesDefeated"
            // and "site.Destroyed" reach this quest. (QuestGen also auto-tags slate objects
            // referenced by XML inSignals when generation finishes; this makes it explicit.)
            QuestUtility.AddQuestTag(site, QuestGenUtility.HardcodedTargetQuestTagWithQuestID("site"));
        }
    }
}
