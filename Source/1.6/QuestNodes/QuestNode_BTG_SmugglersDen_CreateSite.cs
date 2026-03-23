using BetterTradersGuild.DefRefs;
using BetterTradersGuild.QuestParts;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace BetterTradersGuild.QuestNodes
{
    /// <summary>
    /// QuestNode that creates a smuggler's den quest site.
    ///
    /// Creates a Site world object in memory with the BTG_SmugglersDenSite WorldObjectDef
    /// (which has WorldObjectComp_QuestVault baked in). The site is placed on an orbital tile
    /// near the nearest TG settlement.
    ///
    /// The site is NOT immediately added to the world. A QuestPart_SpawnSite is created
    /// that adds it when the quest is accepted (via quest.InitiateSignal). This ensures
    /// the site only appears on the world map after the player accepts the quest.
    ///
    /// Also adds QuestPart_DestroyWorldObject for cleanup on quest end.
    /// </summary>
    public class QuestNode_BTG_SmugglersDen_CreateSite : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> storeAs;

        public SlateRef<Settlement> nearSettlement;

        protected override bool TestRunInt(Slate slate)
        {
            Settlement settlement = nearSettlement.GetValue(slate);
            if (settlement == null)
            {
                Log.Message("[BTG SmugglersDen] TestRunInt: nearSettlement is null in slate");
                return false;
            }

            // Verify Salvagers faction exists
            Faction salvagers = Find.FactionManager.FirstFactionOfDef(Factions.Salvagers);
            if (salvagers == null)
            {
                Log.Message("[BTG SmugglersDen] TestRunInt: Salvagers faction not found");
                return false;
            }

            // Verify we can find a tile for the site near the TG settlement
            bool foundTile = TileFinder.TryFindNewSiteTile(out _, settlement.Tile,
                minDist: 1, maxDist: 10, allowCaravans: false, canBeSpace: true);
            if (!foundTile)
            {
                Log.Message($"[BTG SmugglersDen] TestRunInt: TryFindNewSiteTile failed near {settlement.Label} (tile {settlement.Tile})");
            }
            return foundTile;
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
                Log.Error("[BTG] QuestNode_BTG_SmugglersDen_CreateSite: Failed to find tile for site");
                return;
            }

            // Create site using custom WorldObjectDef (has WorldObjectComp_QuestVault)
            Site site = (Site)WorldObjectMaker.MakeWorldObject(
                DefDatabase<WorldObjectDef>.GetNamed("BTG_SmugglersDenSite"));
            site.Tile = tile;
            site.SetFaction(salvagers);

            // Add the SitePart with threat points
            float threatPoints = slate.Get<float>("siteThreatPoints",
                StorytellerUtility.DefaultThreatPointsNow(Find.World));

            SitePartDef sitePartDef = DefDatabase<SitePartDef>.GetNamed("BTG_SmugglersDen");
            SitePartParams partParams = new SitePartParams { threatPoints = threatPoints };
            site.AddPart(new SitePart(site, sitePartDef, partParams));

            // Store in slate for other quest nodes
            string slotName = storeAs.GetValue(slate);
            if (!string.IsNullOrEmpty(slotName))
                slate.Set(slotName, site);

            // Defer site spawning to quest acceptance
            QuestPart_SpawnSite spawnPart = new QuestPart_SpawnSite
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID("Initiate"),
                site = site
            };
            quest.AddPart(spawnPart);

            // Destroy site on quest end (timeout, failure, or completion after player leaves)
            QuestPart_DestroyWorldObject destroyPart = new QuestPart_DestroyWorldObject();
            destroyPart.worldObject = site;
            destroyPart.inSignal = QuestGenUtility.HardcodedSignalWithQuestID("End");
            quest.AddPart(destroyPart);

            // Signal registration for site-related events
            QuestUtility.AddQuestTag(site, quest.id.ToString());
        }
    }
}
