using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.QuestParts
{
    /// <summary>
    /// Quest part that initiates a trade request on a BTG settlement.
    /// Mirrors vanilla QuestPart_InitiateTradeRequest behavior for consistency.
    /// </summary>
    public class QuestPart_BTG_InitiateTradeRequest : QuestPart
    {
        public string inSignal;
        public Settlement settlement;
        public ThingDef requestedThingDef;
        public int requestedCount;
        public int requestDuration;
        public bool keepAfterQuestEnds;

        // Track whether we've already activated to prevent double-activation
        private bool activated;

        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo target in base.QuestLookTargets)
                    yield return target;
                if (settlement != null)
                    yield return settlement;
            }
        }

        public override IEnumerable<Faction> InvolvedFactions
        {
            get
            {
                foreach (Faction faction in base.InvolvedFactions)
                    yield return faction;
                if (settlement?.Faction != null)
                    yield return settlement.Faction;
            }
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                foreach (Dialog_InfoCard.Hyperlink hyperlink in base.Hyperlinks)
                    yield return hyperlink;
                if (requestedThingDef != null)
                    yield return new Dialog_InfoCard.Hyperlink(requestedThingDef);
            }
        }

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag != inSignal)
                return;

            ActivateTradeRequest();
        }

        /// <summary>
        /// Activates the trade request on the settlement.
        /// Called via signal when the quest is accepted/initiated.
        /// </summary>
        private void ActivateTradeRequest()
        {
            // Prevent double-activation (can happen via both immediate call and signal)
            if (activated)
                return;

            TradeRequestComp tradeComp = settlement?.GetComponent<TradeRequestComp>();
            if (tradeComp == null)
                return;

            if (tradeComp.ActiveRequest)
            {
                Log.Error($"[BTG] Settlement {settlement.Label} already has an active trade request.");
                return;
            }

            tradeComp.requestThingDef = requestedThingDef;
            tradeComp.requestCount = requestedCount;
            tradeComp.expiration = Find.TickManager.TicksGame + requestDuration;
            activated = true;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if (keepAfterQuestEnds)
                return;

            TradeRequestComp tradeComp = settlement?.GetComponent<TradeRequestComp>();
            if (tradeComp?.ActiveRequest == true)
                tradeComp.Disable();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Defs.Look(ref requestedThingDef, "requestedThingDef");
            Scribe_Values.Look(ref requestedCount, "requestedCount");
            Scribe_Values.Look(ref requestDuration, "requestDuration");
            Scribe_Values.Look(ref keepAfterQuestEnds, "keepAfterQuestEnds");
            Scribe_Values.Look(ref activated, "activated");
        }
    }
}
