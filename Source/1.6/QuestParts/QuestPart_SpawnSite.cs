using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.QuestParts
{
    /// <summary>
    /// QuestPart that adds a pre-created Site to the world when a signal fires.
    ///
    /// Used to defer site creation until quest acceptance (quest.InitiateSignal).
    /// The Site is created in memory during QuestNode.RunInt() but not added to the
    /// world until the player accepts the quest.
    /// </summary>
    public class QuestPart_SpawnSite : QuestPart
    {
        public string inSignal;
        public Site site;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal && site != null && !site.Spawned)
            {
                Find.WorldObjects.Add(site);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref site, "site");
        }
    }
}
