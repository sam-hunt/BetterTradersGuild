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
    // QuestNode that creates the reward choices for the smuggler's den quest.
    //
    // Creates a QuestPart_Choice with 3 options:
    // - Option 1: Cargo vault stocked with Trader Type A + standard quest loot
    // - Option 2: Cargo vault stocked with Trader Type B + standard quest loot
    // - Option 3: Goodwill with TG (standard vanilla pattern, vault sealed)
    //
    // Hostile giver: vanilla's reward pipeline (QuestGen_Rewards.GiveRewards /
    // RewardsGenerator.DoGenerate) collapses a hostile giver's rewards to a single
    // pure-goodwill choice - no items, no picking between packages. Mirror that:
    // when the TG is hostile the only option is goodwill and the vault stays sealed.
    // If the player disabled goodwill rewards for the faction there is nothing
    // left to offer, so the quest is not generated at all.
    //
    // The 2 trader types are selected randomly with removal, weighted by commonality,
    // from the available orbital traders in the current world.
    //
    // Each cargo option includes a QuestPart_SetVaultTraderKind that writes the
    // trader defName to the site's WorldObjectComp_QuestVault on quest acceptance.
    // The goodwill option includes QuestPart_SetVaultTraderKind with null (vault sealed).
    //
    // NOTE: adding every option's QuestPart_SetVaultTraderKind to the quest is safe:
    // QuestPart_Choice.Choose() removes the unchosen options' parts from the quest
    // before the Initiate signal fires, so only the chosen part ever receives it.
    public class QuestNode_BTG_SmugglersDen_Rewards : QuestNode
    {
        public SlateRef<Site> site;
        public SlateRef<Faction> faction;
        public SlateRef<int> traderTypeCount = 2;

        // Quest reward value (the script derives it from siteThreatPoints via
        // vanilla's Util_GetDefaultRewardValueFromPoints subscript). Sizes the
        // goodwill option through Reward_Goodwill.InitFromValue, vanilla's own
        // value->goodwill conversion: RewardValueToGoodwillCurve for the base
        // amount, plus the hostile-giver boost of up to double (min(-PlayerGoodwill/2,
        // base)), clamped so goodwill can't exceed +100.
        public SlateRef<float> rewardValue;

        protected override bool TestRunInt(Slate slate)
        {
            // Site may not exist in slate during TestRunInt (created later in RunInt).
            // We only need to validate that enough orbital traders are available.
            Faction factionVal = faction.GetValue(slate);
            if (factionVal == null)
                return false;

            // Hostile giver: goodwill is the only reward on offer, so no traders
            // are needed - but the option must actually be payable.
            if (factionVal.HostileTo(Faction.OfPlayer))
                return factionVal.allowGoodwillRewards;

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

            // Hostile giver: goodwill only, vault sealed (see class comment). The
            // goodwill option is forced on even if allowGoodwillRewards is off:
            // TestRunInt gates normal generation, so this only happens dev-forced,
            // and an empty choice list would be worse than an unwanted goodwill card.
            // Otherwise: if goodwill is disabled, fill the slot with an extra cargo option.
            bool hostile = factionVal.HostileTo(Faction.OfPlayer);
            bool includeGoodwill = hostile || factionVal.allowGoodwillRewards;
            int cargoCount = hostile ? 0 : (factionVal.allowGoodwillRewards ? baseCount : baseCount + 1);

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

            // Add goodwill option if player's reward preferences allow it (always on
            // for a hostile giver, where it is the sole option)
            if (includeGoodwill)
            {
                QuestPart_Choice.Choice goodwillChoice = new QuestPart_Choice.Choice();

                // InitFromValue sets goodwillReward.amount from the reward value
                // (see rewardValue field comment); it reads only parms.giverFaction,
                // for the current goodwill clamp and the hostile boost. The computed
                // amount is the single source for the card and the payout part below.
                Reward_Goodwill goodwillReward = new Reward_Goodwill();
                goodwillReward.faction = factionVal;
                goodwillReward.InitFromValue(
                    rewardValue.GetValue(slate),
                    new RewardsGeneratorParams { giverFaction = factionVal },
                    out _);
                goodwillChoice.rewards.Add(goodwillReward);

                // QuestPart: actually pay the goodwill on quest success. Reward objects
                // are display-only; vanilla generates this part from the reward via
                // Reward_Goodwill.GenerateQuestParts in the QuestNode_GiveRewards flow,
                // which this hand-built choice bypasses - without it the option
                // advertised the goodwill and never granted it. Vanilla sets exactly
                // these three fields; the signal is the quest's success trigger.
                QuestPart_FactionGoodwillChange goodwillPart = new QuestPart_FactionGoodwillChange
                {
                    inSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.AllEnemiesDefeated"),
                    faction = factionVal,
                    change = goodwillReward.amount,
                };
                goodwillChoice.questParts.Add(goodwillPart);
                quest.AddPart(goodwillPart);

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

        // Selects distinct trader types using weighted random with removal.
        // Deterministic based on quest ID.
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

    // Custom Reward display for cargo vault claim in quest reward UI.
    // Modeled after vanilla Reward_CampLoot - shows a text label with icon
    // describing the trader type whose cargo will stock the vault.
    [StaticConstructorOnStartup]
    public class Reward_CargoClaim : Reward
    {
        public TraderKindDef traderKindDef;

        // Explicit null check instead of ?? - Texture2D is a UnityEngine.Object (see CLAUDE.md)
        private static readonly Texture2D Icon = ResolveIcon();

        private static Texture2D ResolveIcon()
        {
            Texture2D tex = ContentFinder<Texture2D>.Get("Things/Building/AncientHatch/AncientHatch_Closed", false);
            if (tex == null)
                return BaseContent.BadTex;
            return tex;
        }

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
