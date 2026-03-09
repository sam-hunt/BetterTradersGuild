using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.ScenParts
{
    /// <summary>
    /// ScenPart that configures starting pawns with explicit xenotype assignments per
    /// slot position. Each slot's xenotype is defined via <slotXenotypes> list items,
    /// giving full control over the ordering and distribution of xenotypes among both
    /// starting and optional pawns. Slots beyond the list length are padded with Baseliner.
    /// </summary>
    public class ScenPart_ConfigureStartingPawnsXenotypes : ScenPart_ConfigPage_ConfigureStartingPawnsBase
    {
        public int pawnCount = 3;
        public DevelopmentalStage allowedDevelopmentalStages = DevelopmentalStage.Adult | DevelopmentalStage.Child;
        public List<SkillDef> requiredSkills;
        public List<XenotypeDef> slotXenotypes;

        private string pawnCountBuf;
        private string pawnChoiceCountBuf;

        protected override int TotalPawnCount => pawnCount;

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            bool biotechActive = ModsConfig.BiotechActive;
            int xenoRows = biotechActive ? pawnChoiceCount : 0;
            float height = RowHeight * (2 + xenoRows);
            Rect rect = listing.GetScenPartRect(this, height);

            Rect countRect = new Rect(rect.x, rect.y, rect.width, RowHeight);
            Rect choiceRect = new Rect(rect.x, rect.y + RowHeight, rect.width, RowHeight);

            // "chosen from" label in left column (vanilla pattern)
            Rect chosenFromLabel = new Rect(rect.x - 200f, choiceRect.y, 200f, RowHeight);
            chosenFromLabel.xMax -= 4f;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(chosenFromLabel, "ScenPart_StartWithPawns_OutOf".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.TextFieldNumeric(countRect, ref pawnCount, ref pawnCountBuf, 1f, 10f);
            Widgets.TextFieldNumeric(choiceRect, ref pawnChoiceCount, ref pawnChoiceCountBuf, pawnCount, 10f);

            if (pawnChoiceCount < pawnCount)
                pawnChoiceCount = pawnCount;

            if (biotechActive)
            {
                if (slotXenotypes == null)
                    slotXenotypes = new List<XenotypeDef>();
                while (slotXenotypes.Count < pawnChoiceCount)
                    slotXenotypes.Add(XenotypeDefOf.Baseliner);
                while (slotXenotypes.Count > pawnChoiceCount)
                    slotXenotypes.RemoveAt(slotXenotypes.Count - 1);

                // "Xenotypes" label in left column next to first slot
                Rect xenotypesLabel = new Rect(rect.x - 200f, rect.y + RowHeight * 2f, 200f, RowHeight);
                xenotypesLabel.xMax -= 4f;
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(xenotypesLabel, "Xenotypes");
                Text.Anchor = TextAnchor.UpperLeft;

                for (int i = 0; i < pawnChoiceCount; i++)
                {
                    Rect xenoRect = new Rect(rect.x, rect.y + RowHeight * (2 + i), rect.width, RowHeight);
                    int index = i;
                    XenotypeDef current = slotXenotypes[i] ?? XenotypeDefOf.Baseliner;
                    if (Widgets.ButtonText(xenoRect, (i + 1) + ". " + current.LabelCap))
                    {
                        FloatMenuUtility.MakeMenu(
                            DefDatabase<XenotypeDef>.AllDefs,
                            x => x.LabelCap,
                            x => delegate { slotXenotypes[index] = x; }
                        );
                    }
                }
            }
        }

        // Replaces vanilla's starting pawns config; both should not exist in the same scenario.
        public override bool CanCoexistWith(ScenPart other)
        {
            return !(other is ScenPart_ConfigPage_ConfigureStartingPawns);
        }

        public override void Randomize()
        {
            pawnCount = Rand.RangeInclusive(1, 5);
            pawnChoiceCount = pawnCount + Rand.RangeInclusive(0, 3);
            slotXenotypes = new List<XenotypeDef>();
            for (int i = 0; i < pawnChoiceCount; i++)
            {
                if (ModsConfig.BiotechActive)
                    slotXenotypes.Add(DefDatabase<XenotypeDef>.AllDefs.RandomElement());
                else
                    slotXenotypes.Add(null);
            }
        }

        private XenotypeDef GetXenotypeForSlot(int index)
        {
            if (slotXenotypes != null && index < slotXenotypes.Count && slotXenotypes[index] != null)
                return slotXenotypes[index];
            return ModsConfig.BiotechActive ? XenotypeDefOf.Baseliner : null;
        }

        protected override void GenerateStartingPawns()
        {
            int attempts = 0;
            do
            {
                StartingPawnUtility.ClearAllStartingPawns();
                for (int i = 0; i < pawnChoiceCount; i++)
                {
                    PawnGenerationRequest request = StartingPawnUtility.GetGenerationRequest(i);
                    request.ForcedXenotype = GetXenotypeForSlot(i);
                    StartingPawnUtility.SetGenerationRequest(i, request);
                    StartingPawnUtility.AddNewPawn(i);
                }
                attempts++;
            }
            while (attempts <= 20 && !StartingPawnUtility.WorkTypeRequirementsSatisfied());
        }

        public override void PostIdeoChosen()
        {
            Find.GameInitData.allowedDevelopmentalStages = allowedDevelopmentalStages;
            Find.GameInitData.startingSkillsRequired = requiredSkills;
            base.PostIdeoChosen();
        }

        public override string Summary(Scenario scen)
        {
            if (pawnCount == 1)
                return "ScenPart_StartWithPawn".Translate();
            return "ScenPart_StartWithPawns".Translate(pawnCount);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pawnCount, "pawnCount");
            Scribe_Values.Look(ref allowedDevelopmentalStages, "allowedDevelopmentalStages");
            Scribe_Collections.Look(ref requiredSkills, "requiredSkills", LookMode.Def);
            Scribe_Collections.Look(ref slotXenotypes, "slotXenotypes", LookMode.Def);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ pawnCount;
        }
    }
}
