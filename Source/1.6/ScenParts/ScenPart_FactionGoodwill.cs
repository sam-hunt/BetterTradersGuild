using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.ScenParts
{
    /// <summary>
    /// ScenPart that sets a target faction's goodwill toward the player faction
    /// to a specific value at game start.
    /// </summary>
    public class ScenPart_FactionGoodwill : ScenPart
    {
        private const string SummaryTag = "FactionGoodwill";

        public FactionDef factionDef;
        public int goodwill;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref factionDef, "factionDef");
            Scribe_Values.Look(ref goodwill, "goodwill");
        }

        public override void PostWorldGenerate()
        {
            Faction target = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (target == null || target.IsPlayer)
            {
                return;
            }

            int current = target.PlayerGoodwill;
            int delta = goodwill - current;
            if (delta != 0)
            {
                target.TryAffectGoodwillWith(
                    Faction.OfPlayer,
                    delta,
                    canSendMessage: false,
                    canSendHostilityLetter: false);
            }
        }

        public override string Summary(Scenario scen)
        {
            return ScenSummaryList.SummaryWithList(scen, SummaryTag, "Start with goodwill");
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == SummaryTag)
            {
                yield return factionDef.label.CapitalizeFirst() + ": " + goodwill;
            }
        }
    }
}
