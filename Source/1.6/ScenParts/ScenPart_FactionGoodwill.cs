using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
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

        private string goodwillBuf;

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect rect = listing.GetScenPartRect(this, RowHeight * 2f);
            Rect factionRect = new Rect(rect.x, rect.y, rect.width, RowHeight);
            Rect goodwillRect = new Rect(rect.x, rect.y + RowHeight, rect.width, RowHeight);

            string factionLabel = factionDef != null ? factionDef.LabelCap.ToString() : "Choose faction...";
            if (Widgets.ButtonText(factionRect, factionLabel))
            {
                FloatMenuUtility.MakeMenu(
                    DefDatabase<FactionDef>.AllDefs.Where(d => !d.isPlayer),
                    d => d.LabelCap,
                    d => delegate { factionDef = d; }
                );
            }

            Widgets.TextFieldNumeric(goodwillRect, ref goodwill, ref goodwillBuf, -100f, 100f);
        }

        public override void Randomize()
        {
            factionDef = DefDatabase<FactionDef>.AllDefs
                .Where(d => !d.isPlayer)
                .RandomElementWithFallback();
            goodwill = Rand.RangeInclusive(-100, 100);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref factionDef, "factionDef");
            Scribe_Values.Look(ref goodwill, "goodwill");
        }

        public override void PostGameStart()
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
            if (tag == SummaryTag && factionDef != null)
            {
                yield return factionDef.label.CapitalizeFirst() + ": " + goodwill;
            }
        }

        public override bool HasNullDefs()
        {
            return base.HasNullDefs() || factionDef == null;
        }
    }
}
