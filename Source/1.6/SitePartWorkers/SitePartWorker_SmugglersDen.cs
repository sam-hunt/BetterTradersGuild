using RimWorld;

namespace BetterTradersGuild.SitePartWorkers
{
    // SitePartWorker for smuggler's den quest sites.
    //
    // Minimal implementation - the vanilla SitePartWorker base class handles
    // most Site behavior; quest completion is driven by the QuestScriptDef's
    // AllEnemiesDefeated signal. The den sends no recurring raids of its own:
    // Site.PostMapGenerate skips the inherited TimedDetectionRaids countdown on
    // gravship-landed maps, and the den is only reachable by gravship (signal
    // jammer). Hostiles can still arrive via storyteller threats (gravship-landed
    // maps are tagged Map_PlayerHome) or a starving garrison's resupply call
    // (ResupplyRaidUtility).
    public class SitePartWorker_SmugglersDen : SitePartWorker
    {
    }
}
