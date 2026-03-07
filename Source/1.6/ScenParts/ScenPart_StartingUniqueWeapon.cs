using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
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
        public QualityCategory quality = QualityCategory.Normal;
        public List<WeaponTraitDef> requiredTraits;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look(ref quality, "quality", QualityCategory.Normal);
            Scribe_Collections.Look(ref requiredTraits, "requiredTraits", LookMode.Def);
        }

        public override IEnumerable<Thing> PlayerStartingThings()
        {
            Thing weapon = ThingMaker.MakeThing(thingDef);

            CompQuality qualityComp = weapon.TryGetComp<CompQuality>();
            qualityComp?.SetQuality(quality, ArtGenerationContext.Outsider);

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
            if (tag == "PlayerStartsWith")
            {
                yield return thingDef.label.CapitalizeFirst();
            }
        }
    }
}
