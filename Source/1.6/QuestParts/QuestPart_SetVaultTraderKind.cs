using BetterTradersGuild.Comps;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.QuestParts
{
    /// <summary>
    /// QuestPart that writes the chosen TraderKindDef to the quest site's
    /// WorldObjectComp_QuestVault when it receives a signal.
    ///
    /// Added to both the quest's parts list (for serialization) and a
    /// QuestPart_Choice.Choice.questParts list (for choice association).
    /// When the choice is selected and the quest accepted, the initiate signal
    /// fires and this part writes the trader defName to the site comp.
    ///
    /// For the goodwill reward option, traderKindDefName is null, which means
    /// the vault will be sealed (no cargo generated).
    /// </summary>
    public class QuestPart_SetVaultTraderKind : QuestPart
    {
        public string inSignal;
        public Site site;
        public string traderKindDefName;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                ApplyTraderKind();
            }
        }

        private void ApplyTraderKind()
        {
            if (site == null)
            {
                Log.Warning("[BTG] QuestPart_SetVaultTraderKind: site is null");
                return;
            }

            var comp = site.GetComponent<WorldObjectComp_QuestVault>();
            if (comp == null)
            {
                Log.Warning("[BTG] QuestPart_SetVaultTraderKind: WorldObjectComp_QuestVault not found on site");
                return;
            }

            comp.chosenTraderKindDefName = traderKindDefName;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_References.Look(ref site, "site");
            Scribe_Values.Look(ref traderKindDefName, "traderKindDefName");
        }
    }
}
