using BetterTradersGuild.QuestParts;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace BetterTradersGuild.QuestNodes
{
    /// <summary>
    /// Quest node that initiates a trade request on a BTG settlement.
    /// Creates a QuestPart_BTG_InitiateTradeRequest following vanilla's pattern.
    /// Validation is delegated to upstream QuestNode_GetNearestTGSettlement.
    /// </summary>
    public class QuestNode_BTG_TradeRequest_Initiate : QuestNode
    {
        public SlateRef<Settlement> settlement;
        public SlateRef<ThingDef> requestedThingDef;
        public SlateRef<int> requestedThingCount;
        public SlateRef<int> duration;

        protected override bool TestRunInt(Slate slate)
        {
            // Validate all required parameters are present in slate
            // This matches vanilla QuestNode_TradeRequest_Initiate behavior
            return settlement.GetValue(slate) != null
                && requestedThingCount.GetValue(slate) > 0
                && requestedThingDef.GetValue(slate) != null
                && duration.GetValue(slate) > 0;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            // Create and configure the QuestPart (matches vanilla pattern)
            QuestPart_BTG_InitiateTradeRequest questPart = new QuestPart_BTG_InitiateTradeRequest
            {
                settlement = settlement.GetValue(slate),
                requestedThingDef = requestedThingDef.GetValue(slate),
                requestedCount = requestedThingCount.GetValue(slate),
                requestDuration = duration.GetValue(slate),
                keepAfterQuestEnds = false,
                // Get the inSignal from slate - this is set by QuestGen infrastructure
                // and fires when the quest starts/is accepted
                inSignal = slate.Get<string>("inSignal")
            };

            QuestGen.quest.AddPart(questPart);

            // Trade request activates when player accepts the quest via the inSignal
            // (which is set to quest.InitiateSignal by QuestGen infrastructure)
        }
    }
}
