using System.Collections.Generic;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.QuestParts;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace BetterTradersGuild.QuestNodes
{
    /// <summary>
    /// QuestNode that creates the reward choices for the smuggler's den quest.
    ///
    /// Creates a QuestPart_Choice with 3 options:
    /// - Option 1: Cargo vault stocked with Trader Type A + standard quest loot
    /// - Option 2: Cargo vault stocked with Trader Type B + standard quest loot
    /// - Option 3: Goodwill with TG (standard vanilla pattern, vault sealed)
    ///
    /// The 2 trader types are selected randomly with removal, weighted by commonality,
    /// from the available orbital traders in the current world.
    ///
    /// Each cargo option includes a QuestPart_SetVaultTraderKind that writes the
    /// trader defName to the site's WorldObjectComp_QuestVault on quest acceptance.
    /// The goodwill option includes QuestPart_SetVaultTraderKind with null (vault sealed).
    /// </summary>
    public class QuestNode_BTG_SmugglersDen_Rewards : QuestNode
    {
        public SlateRef<Site> site;
        public SlateRef<Faction> faction;
        public SlateRef<int> traderTypeCount = 2;

        protected override bool TestRunInt(Slate slate)
        {
            // Site may not exist in slate during TestRunInt (created later in RunInt).
            // We only need to validate that enough orbital traders are available.
            Faction factionVal = faction.GetValue(slate);
            if (factionVal == null)
                return false;

            // Need enough traders for all cargo slots (base count + 1 if goodwill disabled)
            int needed = factionVal.allowGoodwillRewards
                ? traderTypeCount.GetValue(slate)
                : traderTypeCount.GetValue(slate) + 1;
            List<TraderKindDef> traders = OrbitalTraderHelper.GetAvailableOrbitalTraders(factionVal);
            return traders.Count >= needed;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;

            Site siteVal = site.GetValue(slate);
            Faction factionVal = faction.GetValue(slate);
            int baseCount = traderTypeCount.GetValue(slate);

            // If goodwill is disabled, fill the slot with an extra cargo option
            bool includeGoodwill = factionVal.allowGoodwillRewards;
            int cargoCount = includeGoodwill ? baseCount : baseCount + 1;

            // Select distinct trader types using weighted random with removal
            List<TraderKindDef> available = OrbitalTraderHelper.GetAvailableOrbitalTraders(factionVal);
            List<TraderKindDef> selectedTraders = SelectDistinctTraders(available, cargoCount, quest.id);




            // Create the reward choice
            string initiateSignal = QuestGenUtility.HardcodedSignalWithQuestID("Initiate");

            QuestPart_Choice choicePart = new QuestPart_Choice();
            choicePart.inSignalChoiceUsed = initiateSignal;

            // Add cargo vault options
            foreach (TraderKindDef traderKind in selectedTraders)
            {
                QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice();

                // Reward display: trader cargo label
                Reward_CargoClaim cargoReward = new Reward_CargoClaim();
                cargoReward.traderKindDef = traderKind;
                choice.rewards.Add(cargoReward);

                // QuestPart: set the vault trader type on the site
                // Must be added to both the choice AND the quest (for serialization)
                QuestPart_SetVaultTraderKind setTraderPart = new QuestPart_SetVaultTraderKind
                {
                    inSignal = initiateSignal,
                    site = siteVal,
                    traderKindDefName = traderKind.defName
                };
                choice.questParts.Add(setTraderPart);
                quest.AddPart(setTraderPart);

                choicePart.choices.Add(choice);
            }

            // Add goodwill option if player's reward preferences allow it
            if (factionVal.allowGoodwillRewards)
            {
                QuestPart_Choice.Choice goodwillChoice = new QuestPart_Choice.Choice();

                Reward_Goodwill goodwillReward = new Reward_Goodwill();
                goodwillReward.faction = factionVal;
                goodwillReward.amount = 15;
                goodwillChoice.rewards.Add(goodwillReward);

                // QuestPart: set null trader (vault will be sealed)
                QuestPart_SetVaultTraderKind sealVaultPart = new QuestPart_SetVaultTraderKind
                {
                    inSignal = initiateSignal,
                    site = siteVal,
                    traderKindDefName = null
                };
                goodwillChoice.questParts.Add(sealVaultPart);
                quest.AddPart(sealVaultPart);

                choicePart.choices.Add(goodwillChoice);
            }

            quest.AddPart(choicePart);
        }

        /// <summary>
        /// Selects distinct trader types using weighted random with removal.
        /// Deterministic based on quest ID.
        /// </summary>
        private List<TraderKindDef> SelectDistinctTraders(
            List<TraderKindDef> available,
            int count,
            int questId)
        {
            var selected = new List<TraderKindDef>();
            var pool = new List<TraderKindDef>(available);

            Rand.PushState(questId);
            try
            {
                for (int i = 0; i < count && pool.Count > 0; i++)
                {
                    TraderKindDef chosen = pool.RandomElementByWeight(t => t.commonality);
                    selected.Add(chosen);
                    pool.Remove(chosen);
                }
            }
            finally
            {
                Rand.PopState();
            }

            return selected;
        }
    }

    /// <summary>
    /// Custom Reward display for cargo vault claim in quest reward UI.
    /// Modeled after vanilla Reward_CampLoot - shows a text label with icon
    /// describing the trader type whose cargo will stock the vault.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Reward_CargoClaim : Reward
    {
        public TraderKindDef traderKindDef;

        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("Things/Building/AncientHatch/AncientHatch_Closed", false)
            ?? BaseContent.BadTex;

        public override IEnumerable<GenUI.AnonymousStackElement> StackElements
        {
            get
            {
                string label = traderKindDef != null
                    ? "BTG_Reward_CargoClaim".Translate(traderKindDef.label).Resolve()
                    : "BTG_Reward_CargoClaim_Unknown".Translate().Resolve();

                yield return QuestPartUtility.GetStandardRewardStackElement(label, Icon, () => GetDescription(default));
            }
        }

        public override string GetDescription(RewardsGeneratorParams parms)
        {
            if (traderKindDef == null)
                return "BTG_Reward_CargoClaim_Unknown".Translate();

            return "BTG_Reward_CargoClaim".Translate(traderKindDef.label);
        }

        public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
        {
            valueActuallyUsed = rewardValue;
        }

        public override float TotalMarketValue => 0f;

        public override IEnumerable<QuestPart> GenerateQuestParts(
            int index,
            RewardsGeneratorParams parms,
            string customLetterLabel,
            string customLetterText,
            RulePack customLetterLabelRules,
            RulePack customLetterTextRules)
        {
            yield break;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref traderKindDef, "traderKindDef");
        }
    }
}
