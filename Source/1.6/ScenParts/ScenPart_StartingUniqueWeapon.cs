using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.ScenParts
{
    /// <summary>
    /// Generic ScenPart that generates a unique weapon with specific quality and guaranteed
    /// weapon traits. Handles canGenerateAlone=false traits by ensuring a standalone trait
    /// exists first (generating a random one if needed).
    /// </summary>
    public class ScenPart_StartingUniqueWeapon : ScenPart
    {
        public ThingDef thingDef;
        public QualityCategory? quality;
        public List<WeaponTraitDef> requiredTraits;

        private const int MaxTraits = 3;

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            int traitCount = requiredTraits?.Count ?? 0;
            bool canAddMore = traitCount < MaxTraits;
            // Rows: weapon, quality, trait rows, add button (if room)
            float height = RowHeight * (2 + traitCount + (canAddMore ? 1 : 0));
            Rect rect = listing.GetScenPartRect(this, height);

            // Weapon row (full width, no label needed - ScenPart title serves as label)
            Rect weaponRect = new Rect(rect.x, rect.y, rect.width, RowHeight);
            string weaponLabel = thingDef != null ? thingDef.LabelCap.ToString() : "Choose weapon...";
            if (Widgets.ButtonText(weaponRect, weaponLabel))
            {
                FloatMenuUtility.MakeMenu(
                    PossibleWeaponDefs(),
                    d => d.LabelCap,
                    d => delegate
                    {
                        thingDef = d;
                        RemoveIncompatibleTraits();
                    }
                );
            }

            // Quality row - label in left column (vanilla pattern), button full width
            Rect qualityLabelRect = new Rect(rect.x - 200f, rect.y + RowHeight, 200f, RowHeight);
            qualityLabelRect.xMax -= 4f;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(qualityLabelRect, "Quality");
            Text.Anchor = TextAnchor.UpperLeft;
            Rect qualityBtnRect = new Rect(rect.x, rect.y + RowHeight, rect.width, RowHeight);
            string qualityLabel = quality.HasValue
                ? quality.Value.GetLabel().CapitalizeFirst()
                : "Default".Translate().CapitalizeFirst();
            if (Widgets.ButtonText(qualityBtnRect, qualityLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(
                    "Default".Translate().CapitalizeFirst(),
                    delegate { quality = null; }));
                foreach (QualityCategory q in QualityUtility.AllQualityCategories)
                {
                    QualityCategory local = q;
                    options.Add(new FloatMenuOption(
                        local.GetLabel().CapitalizeFirst(),
                        delegate { quality = local; }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Trait rows - labels in left column (vanilla pattern), buttons full width
            int removeIndex = -1;
            float removeButtonWidth = RowHeight;
            for (int i = 0; i < traitCount; i++)
            {
                float rowY = rect.y + RowHeight * (2 + i);

                Rect traitLabelRect = new Rect(rect.x - 200f, rowY, 200f, RowHeight);
                traitLabelRect.xMax -= 4f;
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(traitLabelRect, "Trait " + (i + 1));
                Text.Anchor = TextAnchor.UpperLeft;

                Rect traitBtnRect = new Rect(rect.x, rowY, rect.width - removeButtonWidth, RowHeight);
                Rect removeBtnRect = new Rect(rect.xMax - removeButtonWidth, rowY, removeButtonWidth, RowHeight);

                int index = i;
                WeaponTraitDef current = requiredTraits[i];
                string traitLabel = current != null ? current.LabelCap.ToString() : "Choose trait...";
                if (Widgets.ButtonText(traitBtnRect, traitLabel))
                {
                    FloatMenuUtility.MakeMenu(
                        CompatibleTraitDefs(index),
                        t => t.LabelCap,
                        t => delegate { requiredTraits[index] = t; }
                    );
                }

                if (Widgets.ButtonText(removeBtnRect, "x"))
                {
                    removeIndex = index;
                }
            }
            if (removeIndex >= 0 && requiredTraits != null)
            {
                requiredTraits.RemoveAt(removeIndex);
            }

            // Add trait button (hidden at max)
            if (canAddMore)
            {
                Rect addRect = new Rect(rect.x, rect.y + RowHeight * (2 + traitCount), rect.width, RowHeight);
                if (Widgets.ButtonText(addRect, "Add trait..."))
                {
                    FloatMenuUtility.MakeMenu(
                        CompatibleTraitDefs(-1),
                        t => t.LabelCap,
                        t => delegate
                        {
                            if (requiredTraits == null) requiredTraits = new List<WeaponTraitDef>();
                            requiredTraits.Add(t);
                        }
                    );
                }
            }
        }

        public override void Randomize()
        {
            thingDef = PossibleWeaponDefs().RandomElementWithFallback();
            quality = null;
            requiredTraits = null;
        }

        private IEnumerable<ThingDef> PossibleWeaponDefs()
        {
            return DefDatabase<ThingDef>.AllDefs
                .Where(d => d.comps != null && d.comps.Any(c => c.compClass == typeof(CompUniqueWeapon)));
        }

        /// <summary>
        /// Returns trait defs compatible with the currently selected weapon and existing trait
        /// selections. excludeIndex allows the trait at that slot to be replaced (pass -1 when
        /// adding a new trait). Filters by weapon category and exclusion tag overlap.
        /// </summary>
        private IEnumerable<WeaponTraitDef> CompatibleTraitDefs(int excludeIndex)
        {
            List<WeaponCategoryDef> weaponCategories = GetWeaponCategories();

            return DefDatabase<WeaponTraitDef>.AllDefs.Where(candidate =>
            {
                if (weaponCategories != null && !weaponCategories.Contains(candidate.weaponCategory))
                    return false;

                if (requiredTraits == null)
                    return true;

                for (int i = 0; i < requiredTraits.Count; i++)
                {
                    if (i == excludeIndex || requiredTraits[i] == null) continue;
                    if (TraitsOverlap(candidate, requiredTraits[i])) return false;
                }
                return true;
            });
        }

        private void RemoveIncompatibleTraits()
        {
            if (requiredTraits == null) return;
            List<WeaponCategoryDef> weaponCategories = GetWeaponCategories();
            if (weaponCategories == null) return;
            requiredTraits.RemoveAll(t => t != null && !weaponCategories.Contains(t.weaponCategory));
        }

        private List<WeaponCategoryDef> GetWeaponCategories()
        {
            if (thingDef == null) return null;
            var props = thingDef.comps?.OfType<CompProperties_UniqueWeapon>().FirstOrDefault();
            return props?.weaponCategories;
        }

        private static bool TraitsOverlap(WeaponTraitDef a, WeaponTraitDef b)
        {
            if (a == b) return true;
            if (a.exclusionTags.NullOrEmpty() || b.exclusionTags.NullOrEmpty()) return false;
            return a.exclusionTags.Any(tag => b.exclusionTags.Contains(tag));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look(ref quality, "quality");
            Scribe_Collections.Look(ref requiredTraits, "requiredTraits", LookMode.Def);
        }

        public override IEnumerable<Thing> PlayerStartingThings()
        {
            Thing weapon = ThingMaker.MakeThing(thingDef);

            if (quality.HasValue)
            {
                CompQuality qualityComp = weapon.TryGetComp<CompQuality>();
                qualityComp?.SetQuality(quality.Value, ArtGenerationContext.Outsider);
            }

            CompUniqueWeapon uniqueComp = weapon.TryGetComp<CompUniqueWeapon>();
            if (uniqueComp != null && requiredTraits != null && requiredTraits.Count > 0)
            {
                // Clear randomly-generated traits from ThingMaker.MakeThing()
                uniqueComp.TraitsListForReading.Clear();

                // Partition required traits by canGenerateAlone
                List<WeaponTraitDef> standalone = requiredTraits
                    .Where(t => t != null && t.canGenerateAlone)
                    .ToList();
                List<WeaponTraitDef> dependent = requiredTraits
                    .Where(t => t != null && !t.canGenerateAlone)
                    .ToList();

                // Add standalone traits first
                foreach (WeaponTraitDef trait in standalone)
                {
                    if (uniqueComp.CanAddTrait(trait))
                    {
                        uniqueComp.AddTrait(trait);
                    }
                }

                // If dependent traits exist but no traits added yet, generate a random companion
                if (dependent.Count > 0 && uniqueComp.TraitsListForReading.Count == 0)
                {
                    List<WeaponTraitDef> compatible = DefDatabase<WeaponTraitDef>.AllDefs
                        .Where(t => uniqueComp.CanAddTrait(t))
                        .ToList();

                    if (compatible.Count > 0)
                    {
                        uniqueComp.AddTrait(compatible.RandomElement());
                    }
                }

                // Add dependent traits
                foreach (WeaponTraitDef trait in dependent)
                {
                    if (uniqueComp.CanAddTrait(trait))
                    {
                        uniqueComp.AddTrait(trait);
                    }
                }

                UniqueWeaponNameColorRegenerator.RegenerateNameAndColor(weapon, uniqueComp);
            }

            yield return weapon;
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "PlayerStartsWith" && thingDef != null)
            {
                yield return thingDef.label.CapitalizeFirst();
            }
        }

        public override bool HasNullDefs()
        {
            return base.HasNullDefs() || thingDef == null;
        }
    }
}
