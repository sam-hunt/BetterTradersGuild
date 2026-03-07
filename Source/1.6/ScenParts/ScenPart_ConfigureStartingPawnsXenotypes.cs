using System.Collections.Generic;
using RimWorld;
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

        protected override int TotalPawnCount => pawnCount;

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
