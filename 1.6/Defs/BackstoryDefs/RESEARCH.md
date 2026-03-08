# BackstoryDef Research - RimWorld 1.6

Comprehensive reference for creating BackstoryDefs for the Better Traders Guild mod.
Based on analysis of Core, Royalty, Odyssey, and Anomaly backstory definitions.

---

## Table of Contents

1. [XML Schema Reference](#xml-schema-reference)
2. [C# API Surface](#c-api-surface)
3. [Writing Conventions](#writing-conventions)
4. [Mechanical Design Patterns](#mechanical-design-patterns)
5. [BTG Faction Context](#btg-faction-context)
6. [Complete Examples from Core Game](#complete-examples-from-core-game)

---

## XML Schema Reference

### Full BackstoryDef Template

```xml
<BackstoryDef>
  <!-- IDENTITY (Required) -->
  <defName>UniqueIdentifier99</defName>        <!-- Unique def ID -->
  <slot>Adulthood</slot>                        <!-- Childhood | Adulthood -->
  <title>job title</title>                      <!-- Full title (lowercase) -->
  <titleShort>title</titleShort>                <!-- Abbreviated title for UI -->
  <description>Narrative text with [PAWN_nameDef] tokens.</description>

  <!-- GENDER VARIANTS (Optional - only when female form differs) -->
  <titleFemale>female job title</titleFemale>
  <titleShortFemale>female title</titleShortFemale>

  <!-- SKILLS (Optional) -->
  <skillGains>
    <Shooting>4</Shooting>                      <!-- Range: typically -5 to +8 -->
    <Melee>3</Melee>
    <Construction>0</Construction>
    <Mining>0</Mining>
    <Cooking>0</Cooking>
    <Plants>0</Plants>
    <Animals>0</Animals>
    <Crafting>0</Crafting>
    <Artistic>0</Artistic>
    <Medicine>0</Medicine>
    <Social>0</Social>
    <Intellectual>0</Intellectual>
  </skillGains>

  <!-- WORK RESTRICTIONS (Optional) -->
  <!-- Format A: comma-separated string -->
  <workDisables>Hauling, Mining</workDisables>
  <!-- Format B: list elements (prefer this) -->
  <workDisables>
    <li>Hauling</li>
    <li>Mining</li>
  </workDisables>

  <!-- REQUIRED WORK CAPABILITIES (Optional) -->
  <requiredWorkTags>
    <li>Violent</li>
    <li>Intellectual</li>
  </requiredWorkTags>

  <!-- BODY TYPE (Optional) -->
  <bodyTypeGlobal>Male</bodyTypeGlobal>         <!-- Overrides gender-specific -->
  <bodyTypeMale>Male</bodyTypeMale>             <!-- Male | Female | Thin | Fat | Hulk -->
  <bodyTypeFemale>Female</bodyTypeFemale>

  <!-- TRAITS (Optional) -->
  <forcedTraits>
    <Psychopath>0</Psychopath>                  <!-- TraitDef name + degree -->
    <Industriousness>2</Industriousness>         <!-- Degree: specific level -->
    <Nimble />                                   <!-- Self-closing = degree 0 -->
  </forcedTraits>
  <disallowedTraits>
    <Abrasive>0</Abrasive>
    <ShootingAccuracy>-1</ShootingAccuracy>
  </disallowedTraits>

  <!-- POSSESSIONS (Optional - starting items) -->
  <possessions>
    <Silver>50</Silver>                         <!-- ThingDef name + count -->
    <Novel>1</Novel>
    <Apparel_Duster>1</Apparel_Duster>
    <BlocksVacstone MayRequire="Ludeon.RimWorld.Odyssey">20</BlocksVacstone>
  </possessions>

  <!-- SPAWN CONTROL -->
  <spawnCategories>
    <li>Offworld</li>                           <!-- Primary for TradersGuild -->
    <li>Outlander</li>
  </spawnCategories>
  <shuffleable>True</shuffleable>              <!-- Default True; False for named/solid -->
  <requiresSpawnCategory>False</requiresSpawnCategory>  <!-- Restrict to explicit spawning -->

  <!-- INHERITANCE (XML attributes on the element) -->
  <!-- <BackstoryDef Abstract="True" Name="MyBase"> for abstract parents -->
  <!-- <BackstoryDef ParentName="MyBase"> for children -->

  <!-- MISC -->
  <ignoreIllegalLabelCharacterConfigError>True</ignoreIllegalLabelCharacterConfigError>
  <!-- modExtensions available from base Def class -->
</BackstoryDef>
```

### WorkTags Enum Values

All valid values for `workDisables` and `requiredWorkTags`:

| WorkTag         | Description                               |
| --------------- | ----------------------------------------- |
| `None`          | No restrictions                           |
| `ManualDumb`    | Hauling, cleaning                         |
| `ManualSkilled` | Construction, cooking, crafting, smithing |
| `Violent`       | Combat, hunting                           |
| `Caring`        | Doctoring, wardening                      |
| `Social`        | Wardening, trading, recruiting            |
| `Intellectual`  | Research, scanning, operating             |
| `Animals`       | Taming, training, handling                |
| `Artistic`      | Sculpting, art creation                   |
| `Crafting`      | Smithing, tailoring, crafting             |
| `Cooking`       | Cooking, brewing                          |
| `Firefighting`  | Firefighting                              |
| `Cleaning`      | Cleaning                                  |
| `Hauling`       | Hauling                                   |
| `PlantWork`     | Growing, plant cutting                    |
| `Mining`        | Mining, deep drilling                     |
| `Hunting`       | Hunting                                   |
| `Constructing`  | Construction                              |
| `Shooting`      | Ranged combat specifically                |

### Pawn Pronoun Tokens

Use these in `<description>` text. Never hardcode pronouns.

| Token               | Resolves To          | Example               |
| ------------------- | -------------------- | --------------------- |
| `[PAWN_nameDef]`    | Pawn's name          | "Jamie"               |
| `[PAWN_possessive]` | Possessive adjective | "his", "her", "their" |
| `[PAWN_pronoun]`    | Nominative pronoun   | "he", "she", "they"   |
| `[PAWN_objective]`  | Objective pronoun    | "him", "her", "them"  |

**Reflexive form:** Concatenate `[PAWN_objective]self` to produce "himself", "herself", "themself".

**Line breaks:** Use `\n\n` to separate paragraphs within description text. Single `\n` for soft breaks.

### BodyTypeDef Values

| Value    | Typical Usage                                  |
| -------- | ---------------------------------------------- |
| `Male`   | Default male, average build                    |
| `Female` | Default female, average build                  |
| `Thin`   | Scholars, intellectuals, malnourished, dancers |
| `Fat`    | Well-fed, sedentary, commanding presence       |
| `Hulk`   | Laborers, warriors, physically demanding roles |

---

## C# API Surface

### BackstoryDef Class (extends Verse.Def)

**Key Fields:**

| Field                   | Type                                 | XML Tag                   |
| ----------------------- | ------------------------------------ | ------------------------- |
| `slot`                  | `BackstorySlot` enum                 | `<slot>`                  |
| `title`                 | `string` [MustTranslate]             | `<title>`                 |
| `titleFemale`           | `string` [MustTranslate]             | `<titleFemale>`           |
| `titleShort`            | `string` [MustTranslate]             | `<titleShort>`            |
| `titleShortFemale`      | `string` [MustTranslate]             | `<titleShortFemale>`      |
| `skillGains`            | `List<SkillGain>`                    | `<skillGains>`            |
| `workDisables`          | `WorkTags` [Flags]                   | `<workDisables>`          |
| `requiredWorkTags`      | `WorkTags` [Flags]                   | `<requiredWorkTags>`      |
| `spawnCategories`       | `List<string>`                       | `<spawnCategories>`       |
| `bodyTypeGlobal`        | `BodyTypeDef`                        | `<bodyTypeGlobal>`        |
| `bodyTypeMale`          | `BodyTypeDef`                        | `<bodyTypeMale>`          |
| `bodyTypeFemale`        | `BodyTypeDef`                        | `<bodyTypeFemale>`        |
| `forcedTraits`          | `List<BackstoryTrait>`               | `<forcedTraits>`          |
| `disallowedTraits`      | `List<BackstoryTrait>`               | `<disallowedTraits>`      |
| `nameMaker`             | `RulePackDef`                        | `<nameMaker>`             |
| `possessions`           | `List<PossessionThingDefCountClass>` | `<possessions>`           |
| `shuffleable`           | `bool`                               | `<shuffleable>`           |
| `requiresSpawnCategory` | `bool`                               | `<requiresSpawnCategory>` |

**Key Methods:**

| Method                             | Purpose                                                              |
| ---------------------------------- | -------------------------------------------------------------------- |
| `TitleFor(Gender)`                 | Gender-appropriate title with fallback                               |
| `TitleShortFor(Gender)`            | Gender-appropriate short title with fallback chain                   |
| `BodyTypeFor(Gender)`              | Gender-appropriate body type, falls back to global                   |
| `FullDescriptionFor(Pawn)`         | Full description including skills, disabled work, meditation focuses |
| `DisallowsTrait(TraitDef, degree)` | Check trait compatibility                                            |

**Dependent Types:**

- `BackstorySlot`: `Childhood` (0), `Adulthood` (1)
- `BackstoryTrait`: `def` (TraitDef) + `degree` (int) - loaded from XML via `LoadDataFromXmlCustom`
- `SkillGain`: `skill` (SkillDef) + `amount` (int) - loaded from XML via `LoadDataFromXmlCustom`
- `PossessionThingDefCountClass`: `key` (ThingDef) + `value` (IntRange) - supports `MayRequire`
- `Gender`: `None` (0), `Male` (1), `Female` (2)

**No hediff fields exist on BackstoryDef.** Hediffs are assigned via traits (which can grant hediffs) or via PawnKindDef's `techHediffs*` fields. Backstories influence pawns indirectly through traits and skills only.

### Adding Hediff Support via DefModExtension (BTG Custom Feature)

BackstoryDef inherits from `Verse.Def`, which has a `modExtensions` field. We can use this to attach hediff data to backstories and apply them during pawn generation.

**Implementation (~50-70 lines C#):**

1. `BackstoryHediffExtension : DefModExtension` — data class with `List<BackstoryHediffRecord>`
2. `BackstoryHediffRecord` — holds `HediffDef hediff` + optional `BodyPartDef bodyPart`
3. Harmony postfix on `PawnGenerator.GeneratePawn` — checks both childhood and adulthood backstories for the extension, applies hediffs

**XML Usage:**

```xml
<BackstoryDef>
  <defName>BTG_ShuttlePilot01</defName>
  <slot>Adulthood</slot>
  <title>shuttle pilot</title>
  <titleShort>pilot</titleShort>
  <description>...</description>
  <!-- ... normal backstory fields ... -->
  <modExtensions>
    <li Class="BetterTradersGuild.BackstoryHediffExtension">
      <hediffs>
        <li>
          <hediff>PilotAssistantImplant</hediff>
          <bodyPart>Brain</bodyPart>
        </li>
      </hediffs>
    </li>
  </modExtensions>
</BackstoryDef>
```

**Design Considerations:**

- **Body part targeting:** Implants need a `BodyPartDef` (e.g., `Brain` for neurocalculator, `Shoulder` for drill arm). Field is optional for whole-body hediffs.
- **Duplicate prevention:** Check pawn doesn't already have the hediff before adding (e.g., from PawnKindDef techHediffs).
- **DLC gating:** Use `MayRequire` on `<li>` elements for DLC-specific implants (e.g., Royalty neurocalculator).
- **Addable vs regular hediffs:** Implants with `HediffDef.addedPartProps` (bionics, prosthetics) use `AddImplant`; regular hediffs use `AddHediff`. The patch should handle both paths.
- **Thematic examples:**
  - Shuttle pilot → `PilotAssistantImplant` (Brain)
  - Market analyst → `Neurocalculator` (Brain, Royalty DLC)
  - Space miner → `DrillArm` (Shoulder)
  - Station medic → `HealingEnhancer` (Torso)
  - Security chief → `ArmorskinGland` (Torso)

---

## Writing Conventions

### Description Length

| Category | Word Count | Sentences | When to Use                                     |
| -------- | ---------- | --------- | ----------------------------------------------- |
| Minimal  | 20-35      | 1-2       | Simple, clear roles                             |
| Standard | 40-80      | 2-3       | Most backstories                                |
| Extended | 80-150     | 3-5       | Complex narratives, trauma, transformation arcs |
| Maximum  | 150-200    | 4-6       | Rare; rich multi-event narratives               |

**Sweet spot for most backstories: 40-80 words, 2-3 sentences.**

### Narrative Voice

- **Third-person past tense** for events: "[PAWN_nameDef] worked as..."
- **Third-person present** for ongoing traits: "[PAWN_pronoun] is a sociointellectual machine."
- **Mixed tense** within a backstory is common and natural.

### Tone Spectrum (with examples)

**Straightforward/Professional:**

> [PAWN_nameDef] was a well-known civil engineer. [PAWN_possessive] job involved designing and maintaining rock fortification structures.

**Casual/Characterful:**

> [PAWN_pronoun] got to play with weapons, murder-drones, and other 'fun' stuff.

**Dark/Cynical:**

> In the urbworlds, most suffer. But someone has to run the corporations.

**Poetic/Vivid:**

> [PAWN_pronoun] wielded [PAWN_possessive] axe with the finesse of an artist and the force of a charging muffalo.

**Traumatic/Consequential:**

> One mission left [PAWN_nameDef] with pyrophobia and a strong desire to avoid plants.

**Implied/Subtle:**

> [PAWN_nameDef] occasionally offered something extra, if the lord or lady was in the mood.

### Key Writing Rules

1. **Show, don't tell** personality - use word choice and described behavior rather than adjectives
2. **Every skill gain/loss must be narratively justified** in the description
3. **Work disables should follow logically** from the character's history
4. **Forced traits need narrative setup** - explain why the character has that trait
5. **Keep it RimWorld-flavored**: mix sci-fi setting details with grounded human experiences
6. **Avoid modern Earth references** - use RimWorld terminology (urbworld, glitterworld, rimworld, midworld, etc.)
7. **Balance darkness with dry humor** - RimWorld's tone is darkly comic, not grimdark
8. **Use paragraph breaks (`\n\n`)** to separate distinct narrative beats or time periods
9. **Titles are always lowercase** in the `<title>` field

### RimWorld World-Terminology

| Term         | Meaning                             |
| ------------ | ----------------------------------- |
| Glitterworld | High-tech utopian planet            |
| Urbworld     | Dense urban planet, often dystopian |
| Rimworld     | Frontier planet, low-tech           |
| Midworld     | Average development planet          |
| Spacer       | Lives in space / on ships           |
| Cryptosleep  | Stasis/cryogenic sleep              |
| Mechanoid    | AI robot                            |
| Archotech    | Godlike AI technology               |
| Plasteel     | Advanced alloy                      |
| Synthread    | Synthetic fabric                    |
| Go-juice     | Combat stimulant drug               |

---

## Mechanical Design Patterns

### Skill-to-Narrative Correlation

Skills should directly reflect described activities:

| Narrative Element               | Skill(s)              |
| ------------------------------- | --------------------- |
| Trading, negotiation, diplomacy | Social                |
| Bookkeeping, research, analysis | Intellectual          |
| Ship repair, construction       | Construction          |
| Weapon handling, security       | Shooting and/or Melee |
| Medical training, first aid     | Medicine              |
| Tinkering, manufacturing        | Crafting              |
| Food preparation                | Cooking               |
| Farming, botany                 | Plants                |
| Animal husbandry                | Animals               |
| Excavation, drilling            | Mining                |
| Art, decoration, design         | Artistic              |

**Negative skills** indicate aversion, trauma, or neglect:

- Social -3: "spending so long alone severely dampened [PAWN_possessive] conversational abilities"
- Cooking -2: Military background with no domestic experience
- Intellectual -3: "never learned to read"

### Work Disable Justifications

| Work Disable    | Narrative Reasons                                      |
| --------------- | ------------------------------------------------------ |
| `ManualDumb`    | Too elite/specialized, physical inability, boss status |
| `ManualSkilled` | Intellectual-only background, physical disability      |
| `Violent`       | Trauma, pacifism, religious/ethical conviction         |
| `Caring`        | Psychopathy, self-centeredness, emotional damage       |
| `Social`        | Isolation damage, autism-coded, hermit background      |
| `Intellectual`  | Never educated, anti-tech, manual-labor exclusive      |
| `Artistic`      | Utilitarian mindset, cultural suppression              |
| `Cooking`       | Boss/elite status, aversion, never learned             |
| `Firefighting`  | Pyrophobia (explicitly from trauma)                    |
| `PlantWork`     | Urban/space exclusive background, phobia               |
| `Hauling`       | Elite/intellectual status, physical frailty            |
| `Mining`        | Surface-only background, claustrophobia                |
| `Cleaning`      | Boss/elite status                                      |

### Forced Trait Patterns

| Trait             | Degree | Narrative Context                                    |
| ----------------- | ------ | ---------------------------------------------------- |
| `Psychopath`      | 0      | Assassins, crime bosses, corporate fixers            |
| `Ascetic`         | 0      | Monks, hermits, survivalists                         |
| `Greedy`          | 0      | Politicians, moguls, treasure hunters                |
| `Undergrounder`   | 0      | Cave dwellers, miners, bunker-raised                 |
| `Beauty`          | 2      | Models, courtesans, performers                       |
| `Nimble`          | 0      | Dancers, acrobats, scouts                            |
| `Abrasive`        | 0      | Inquisitors, drill sergeants, critics                |
| `Kind`            | 0      | Healers, teachers, caretakers                        |
| `DrugDesire`      | -1     | Religious figures, teetotalers (negative = teetotal) |
| `Industriousness` | 2      | Reformed characters, workaholics                     |
| `Transhumanist`   | 0      | Tech enthusiasts, cyborg backgrounds                 |
| `Brawler`         | 0      | Melee specialists, pit fighters                      |
| `Tough`           | 0      | Hardened survivors, veterans                         |
| `FastLearner`     | 0      | Prodigies, polymaths                                 |
| `TorturedArtist`  | 0      | Troubled creatives                                   |

### Body Type Assignment

| Body Type | Character Archetype                                   |
| --------- | ----------------------------------------------------- |
| `Thin`    | Scholars, analysts, dancers, malnourished, ascetics   |
| `Male`    | Average males, generic builds                         |
| `Female`  | Average females, generic builds                       |
| `Fat`     | Well-fed bosses, sedentary workers, comfortable lives |
| `Hulk`    | Manual laborers, soldiers, heavy lifters              |

**Gender-differentiated body types** are common:

- `bodyTypeMale: Hulk` / `bodyTypeFemale: Female` for crime boss archetype
- `bodyTypeMale: Male` / `bodyTypeFemale: Thin` for entrepreneur archetype

### Inheritance Pattern

For shared properties across multiple backstories:

```xml
<BackstoryDef Abstract="True" Name="BTG_TraderBase">
  <spawnCategories>
    <li>Offworld</li>
  </spawnCategories>
</BackstoryDef>

<BackstoryDef ParentName="BTG_TraderBase">
  <defName>BTG_CargoHandler01</defName>
  <!-- inherits spawnCategories from parent -->
</BackstoryDef>
```

### Possessions with DLC Gating

```xml
<possessions>
  <Silver>25</Silver>
  <BlocksVacstone MayRequire="Ludeon.RimWorld.Odyssey">10</BlocksVacstone>
  <Apparel_PackTurret MayRequire="Ludeon.RimWorld.Anomaly">1</Apparel_PackTurret>
</possessions>
```

---

## BTG Faction Context

### How Backstories Reach TradersGuild Pawns

The TradersGuild faction (defined in Odyssey DLC) uses these backstory filters:

```xml
<backstoryFilters>
  <li>
    <categories>
      <li>Offworld</li>
    </categories>
    <commonality>0.9</commonality>
  </li>
  <li>
    <categories>
      <li>Outlander</li>
    </categories>
    <commonality>0.1</commonality>
  </li>
</backstoryFilters>
```

**This means: Backstories with `Offworld` in their spawnCategories have a 90% weight; `Outlander` has 10%.**

To have our backstories appear on TradersGuild pawns, they MUST include `Offworld` (strongly preferred) or `Outlander` in their `spawnCategories`. Since these are shared categories, our backstories will also appear on other Offworld factions (which is appropriate - they describe spacer/orbital life).

The BTG player faction (`BTG_IndependentTraders`) filters exclusively on `Offworld`.

### TradersGuild Pawn Kinds

| PawnKind                | Role             | Weight | Notes                                |
| ----------------------- | ---------------- | ------ | ------------------------------------ |
| `TradersGuild_Child`    | Child civilian   | 4      | Biotech DLC only                     |
| `TradersGuild_Citizen`  | Adult civilian   | 12     | Highest weight - most common         |
| `TradersGuild_Gunner`   | Ranged soldier   | 10     | Marine armor, charge weapons         |
| `TradersGuild_Elite`    | Elite guard      | 8      | Cataphract/power armor               |
| `TradersGuild_Slasher`  | Melee fighter    | 4      | Shield belt, ultratech melee         |
| `TradersGuild_Heavy`    | Heavy weapons    | 4      | Power armor, heavy guns              |
| `TradersGuild_Magister` | Commander/leader | 3      | Prestige cataphract, neurocalculator |

### Settlement Rooms (Context for Backstory Ideas)

The BTG orbital settlements contain these specialized rooms:

| Room                 | Implications for Backstories                               |
| -------------------- | ---------------------------------------------------------- |
| Command Center       | Station commanders, comms officers, logistics coordinators |
| Cargo Hold / Vault   | Cargo handlers, inventory managers, security, appraisers   |
| Crew Quarters        | General crew, off-duty workers                             |
| Commander's Quarters | Station leadership, senior officers                        |
| Medical Bay          | Medics, ship doctors, trauma surgeons                      |
| Dining Hall          | Cooks, mess staff, nutritionists                           |
| Workshop             | Mechanics, technicians, fabricators                        |
| Hydroponics          | Farmers, botanists, life-support techs                     |
| Landing Pad          | Shuttle pilots, docking coordinators, EVA techs            |
| Entrance / Perimeter | Security, sentries, customs officers                       |
| Power / Utilities    | Engineers, power techs, pipe fitters                       |

### Tech Level

TradersGuild is **Spacer** tech level. Backstories should reflect:

- Familiarity with space travel, orbital habitats, vacuum environments
- Access to advanced technology (charge weapons, bionics, synthread)
- Multi-cultural exposure from trade across many worlds
- Professional specialization typical of technological societies

### Xenotype Distribution

TradersGuild pawns include Hussars (combat), Genies (intellectual), and Starjacks (spacer). Backstories should be compatible with these enhanced human types without requiring them.

---

## Complete Examples from Core Game

### Adulthood - Professional/Technical

```xml
<BackstoryDef>
  <defName>CivilEngineer2</defName>
  <slot>Adulthood</slot>
  <title>civil engineer</title>
  <titleShort>engineer</titleShort>
  <description>[PAWN_nameDef] was a well-known civil engineer. [PAWN_possessive] job involved designing and maintaining rock fortification structures. [PAWN_pronoun] did enough statistical analysis to keep [PAWN_possessive] mind sharp.</description>
  <skillGains>
    <Construction>7</Construction>
    <Intellectual>3</Intellectual>
    <Mining>2</Mining>
    <Social>-3</Social>
  </skillGains>
  <workDisables>None</workDisables>
  <shuffleable>False</shuffleable>
</BackstoryDef>
```

### Adulthood - Combat/Trauma

```xml
<BackstoryDef>
  <defName>CombatEngineer30</defName>
  <slot>Adulthood</slot>
  <title>combat engineer</title>
  <titleShort>engineer</titleShort>
  <description>[PAWN_nameDef] was recruited by the military as an engineer and set to work improving the navy's space shuttles. The harsh training taught [PAWN_objective] how to build and repair military vehicles and structures.\n\nOne mission left [PAWN_nameDef] with pyrophobia and a strong desire to avoid plants.</description>
  <skillGains>
    <Construction>6</Construction>
    <Intellectual>3</Intellectual>
    <Shooting>4</Shooting>
    <Social>-3</Social>
    <Cooking>-2</Cooking>
    <Medicine>2</Medicine>
    <Crafting>2</Crafting>
  </skillGains>
  <workDisables>Artistic, Firefighting, PlantWork</workDisables>
  <shuffleable>False</shuffleable>
</BackstoryDef>
```

### Adulthood - Social/Political

```xml
<BackstoryDef>
  <defName>UrbworldEntrepreneur14</defName>
  <slot>Adulthood</slot>
  <title>urbworld entrepreneur</title>
  <titleShort>entrepreneur</titleShort>
  <description>In the urbworlds, most suffer. But someone has to run the corporations.\n\n[PAWN_nameDef] learned the skills of the trade - greasing palms and technical analysis. [PAWN_pronoun] is a sociointellectual machine.</description>
  <skillGains>
    <Social>6</Social>
    <Intellectual>3</Intellectual>
  </skillGains>
  <spawnCategories>
    <li>Offworld</li>
    <li>Researcher</li>
  </spawnCategories>
  <requiredWorkTags>
    <li>Social</li>
  </requiredWorkTags>
  <bodyTypeMale>Male</bodyTypeMale>
  <bodyTypeFemale>Thin</bodyTypeFemale>
</BackstoryDef>
```

### Adulthood - Crime/Dark

```xml
<BackstoryDef>
  <defName>MafiaBoss17</defName>
  <slot>Adulthood</slot>
  <title>mafia boss</title>
  <titleShort>boss</titleShort>
  <description>[PAWN_nameDef] was a high-ranking member of an urbworld crime syndicate.\n\n[PAWN_pronoun] bribed officials, maintained the loyalty of [PAWN_possessive] subordinates, and extracted overdue payments - by any means necessary.</description>
  <workDisables>
    <li>ManualDumb</li>
    <li>Caring</li>
    <li>Cooking</li>
  </workDisables>
  <requiredWorkTags>
    <li>Social</li>
  </requiredWorkTags>
  <skillGains>
    <Shooting>4</Shooting>
    <Melee>3</Melee>
    <Social>4</Social>
  </skillGains>
  <spawnCategories>
    <li>Offworld</li>
    <li>Pirate</li>
    <li>Cult</li>
  </spawnCategories>
  <bodyTypeMale>Hulk</bodyTypeMale>
  <bodyTypeFemale>Female</bodyTypeFemale>
</BackstoryDef>
```

### Adulthood - Survival/Isolation

```xml
<BackstoryDef>
  <defName>Castaway57</defName>
  <slot>Adulthood</slot>
  <title>castaway</title>
  <titleShort>castaway</titleShort>
  <description>[PAWN_nameDef] was the only survivor of a ship crash on an unhabited animal world. For many years until [PAWN_possessive] rescue [PAWN_pronoun] scrounged an existence out of whatever [PAWN_pronoun] could find.\n\n[PAWN_possessive] survival skills became razor-sharp, but spending so long alone severely dampened [PAWN_possessive] conversational abilities.</description>
  <workDisables>
    <li>Intellectual</li>
    <li>Social</li>
    <li>Artistic</li>
  </workDisables>
  <requiredWorkTags>
    <li>ManualDumb</li>
    <li>PlantWork</li>
  </requiredWorkTags>
  <skillGains>
    <Melee>5</Melee>
    <Animals>3</Animals>
    <Construction>4</Construction>
  </skillGains>
  <spawnCategories>
    <li>Offworld</li>
    <li>Pirate</li>
    <li>Madman</li>
    <li>Cult</li>
  </spawnCategories>
  <bodyTypeMale>Hulk</bodyTypeMale>
  <bodyTypeFemale>Hulk</bodyTypeFemale>
</BackstoryDef>
```

### Adulthood - Forced Trait + Possession

```xml
<BackstoryDef>
  <defName>CaveworldIlluminator95</defName>
  <slot>Adulthood</slot>
  <title>caveworld illuminator</title>
  <titleShort>illuminator</titleShort>
  <description>Among tunnel-dwellers, those with vision as strong as [PAWN_nameDef]'s are revered as sages. [PAWN_pronoun] would lead the way, marking spots to dig with bioluminescent fungus and warning others of impending danger.</description>
  <requiredWorkTags>
    <li>Mining</li>
    <li>ManualSkilled</li>
  </requiredWorkTags>
  <skillGains>
    <Mining>3</Mining>
    <Social>2</Social>
  </skillGains>
  <spawnCategories>
    <li>Offworld</li>
    <li>Pirate</li>
    <li>Miner</li>
  </spawnCategories>
  <bodyTypeMale>Male</bodyTypeMale>
  <bodyTypeFemale>Female</bodyTypeFemale>
  <forcedTraits>
    <Undergrounder>0</Undergrounder>
  </forcedTraits>
</BackstoryDef>
```

### Adulthood - Imperial/Hierarchical

```xml
<BackstoryDef>
  <defName>ImperialInquisitor53</defName>
  <slot>Adulthood</slot>
  <title>imperial inquisitor</title>
  <titleShort>inquisitor</titleShort>
  <titleShortFemale>inquisitress</titleShortFemale>
  <description>[PAWN_nameDef] was an inquisitor in the imperial church's anti-heresy school.\n\n[PAWN_nameDef] hunted unorthodox thoughts wherever they could be found - art, music, code, even private conversations. Upon finding deviance, [PAWN_pronoun] exposed it to bring on the punishment of the collective. And [PAWN_pronoun] could always find the deviance if [PAWN_pronoun] looked hard enough.</description>
  <bodyTypeMale>Thin</bodyTypeMale>
  <bodyTypeFemale>Thin</bodyTypeFemale>
  <spawnCategories><li>ImperialCommon</li></spawnCategories>
  <skillGains>
    <Social>4</Social>
    <Intellectual>4</Intellectual>
  </skillGains>
  <requiredWorkTags>
    <li>Social</li>
    <li>Intellectual</li>
  </requiredWorkTags>
  <forcedTraits>
    <Abrasive>0</Abrasive>
  </forcedTraits>
</BackstoryDef>
```

### Adulthood - Mining/Space Labor

```xml
<BackstoryDef>
  <defName>DeepSpaceMiner3</defName>
  <slot>Adulthood</slot>
  <title>deep space miner</title>
  <titleShort>miner</titleShort>
  <description>[PAWN_nameDef] did the sweaty, grimy work of pulling metal out of asteroids on a deep space rig. [PAWN_pronoun] used [PAWN_possessive] hands-on industrial skills daily - and wasn't bad in a bar fight either.</description>
  <skillGains>
    <Mining>7</Mining>
    <Construction>3</Construction>
    <Melee>2</Melee>
  </skillGains>
  <spawnCategories>
    <li>Offworld</li>
    <li>Pirate</li>
    <li>Miner</li>
    <li>Researcher</li>
  </spawnCategories>
  <requiredWorkTags>
    <li>ManualDumb</li>
    <li>ManualSkilled</li>
  </requiredWorkTags>
  <bodyTypeMale>Fat</bodyTypeMale>
  <bodyTypeFemale>Female</bodyTypeFemale>
  <possessions>
    <BlocksVacstone MayRequire="Ludeon.RimWorld.Odyssey">20</BlocksVacstone>
  </possessions>
</BackstoryDef>
```

### Adulthood - Menial Labor

```xml
<BackstoryDef>
  <defName>FactoryWorker58</defName>
  <slot>Adulthood</slot>
  <title>factory worker</title>
  <titleShort>worker</titleShort>
  <description>[PAWN_nameDef] did menial, unskilled work in an industrial factory. [PAWN_possessive] job also included caring for the mules and horses which transported the goods.</description>
  <workDisables>
    <li>Intellectual</li>
    <li>Artistic</li>
    <li>Cooking</li>
  </workDisables>
  <requiredWorkTags>
    <li>ManualDumb</li>
    <li>ManualSkilled</li>
    <li>Animals</li>
  </requiredWorkTags>
  <skillGains>
    <Animals>3</Animals>
    <Construction>3</Construction>
    <Crafting>2</Crafting>
  </skillGains>
  <spawnCategories>
    <li>Offworld</li>
    <li>Outlander</li>
    <li>Pirate</li>
  </spawnCategories>
  <bodyTypeMale>Hulk</bodyTypeMale>
  <bodyTypeFemale>Hulk</bodyTypeFemale>
</BackstoryDef>
```

### Adulthood - Psychological/Damaged

```xml
<BackstoryDef>
  <defName>PsychiatricPatient94</defName>
  <slot>Adulthood</slot>
  <title>psychiatric patient</title>
  <titleShort>patient</titleShort>
  <description>[PAWN_nameDef] spent most of [PAWN_possessive] adult life in an insane asylum. [PAWN_possessive] industrial homeworld had a poor understanding of mental illness, and [PAWN_pronoun] was treated more like an animal than a person.\n\nThough [PAWN_pronoun] eventually recovered and was released, [PAWN_possessive] experience dampened many of [PAWN_possessive] basic life skills.</description>
  <workDisables>
    <li>Social</li>
    <li>Caring</li>
    <li>Violent</li>
  </workDisables>
  <skillGains>
    <Cooking>-2</Cooking>
    <Crafting>-2</Crafting>
  </skillGains>
  <spawnCategories>
    <li>Offworld</li>
    <li>Outlander</li>
    <li>Madman</li>
    <li>Cult</li>
  </spawnCategories>
  <bodyTypeMale>Thin</bodyTypeMale>
  <bodyTypeFemale>Thin</bodyTypeFemale>
</BackstoryDef>
```

### Childhood - Tribal

```xml
<BackstoryDef>
  <defName>TribeChild40</defName>
  <slot>Childhood</slot>
  <title>tribe child</title>
  <titleShort>tribal</titleShort>
  <description>[PAWN_nameDef] grew up in a tribe, running around the village, moving with the muffalo herds, learning essential skills from [PAWN_possessive] parents.\n\n[PAWN_pronoun] never learned to read and never saw a machine that wasn't an ancient ruin.</description>
  <skillGains>
    <Plants>2</Plants>
    <Melee>2</Melee>
    <Shooting>2</Shooting>
    <Intellectual>-3</Intellectual>
  </skillGains>
  <spawnCategories><li>Tribal</li></spawnCategories>
</BackstoryDef>
```

### Childhood - Intellectual

```xml
<BackstoryDef>
  <defName>Bookworm19</defName>
  <slot>Childhood</slot>
  <title>bookworm</title>
  <titleShort>bookworm</titleShort>
  <description>Rather than socialize with the other children, [PAWN_nameDef] preferred to get lost in literature. [PAWN_pronoun] taught [PAWN_objective]self to read at an early age with books bought from passing traders.</description>
  <workDisables>
    <li>ManualDumb</li>
  </workDisables>
  <skillGains>
    <Intellectual>6</Intellectual>
    <Artistic>2</Artistic>
    <Social>-3</Social>
  </skillGains>
  <spawnCategories><li>Tribal</li></spawnCategories>
  <possessions>
    <Novel>1</Novel>
  </possessions>
</BackstoryDef>
```

### Childhood - Traumatic

```xml
<BackstoryDef>
  <defName>AbandonedChild23</defName>
  <slot>Childhood</slot>
  <title>abandoned child</title>
  <titleShort>abandoned</titleShort>
  <description>[PAWN_nameDef] was born sickly. Thinking that [PAWN_pronoun] would only burden the tribe, [PAWN_possessive] parents exposed [PAWN_objective] to the elements. Somehow, [PAWN_pronoun] survived.</description>
  <skillGains>
    <Melee>3</Melee>
    <Crafting>3</Crafting>
    <Social>-2</Social>
  </skillGains>
  <spawnCategories>
    <li>Tribal</li>
    <li>Cult</li>
  </spawnCategories>
</BackstoryDef>
```

### Adulthood - Performer with Gender Variant Title

```xml
<BackstoryDef>
  <defName>BalletDancer81</defName>
  <slot>Adulthood</slot>
  <title>ballet dancer</title>
  <titleShort>dancer</titleShort>
  <titleShortFemale>ballerina</titleShortFemale>
  <description>[PAWN_nameDef] was a dancer in a traditional ballet troupe. [PAWN_pronoun] mastered the ancient motions and entertained thousands.</description>
  <bodyTypeMale>Thin</bodyTypeMale>
  <bodyTypeFemale>Thin</bodyTypeFemale>
  <spawnCategories><li>ImperialCommon</li></spawnCategories>
  <skillGains>
    <Social>3</Social>
    <Melee>3</Melee>
  </skillGains>
  <requiredWorkTags>
    <li>Social</li>
  </requiredWorkTags>
  <forcedTraits>
    <Nimble>0</Nimble>
  </forcedTraits>
</BackstoryDef>
```

### Adulthood - Rare/Solid with Possessions

```xml
<BackstoryDef>
  <defName>DoomsdayPariah18</defName>
  <slot>Adulthood</slot>
  <title>doomsday pariah</title>
  <titleShort>pariah</titleShort>
  <description>[PAWN_nameDef] managed to open a vault of otherworldly technology while scavenging a dig site. [PAWN_pronoun] unwittingly triggered a doomsday device that cleansed the planet of all life. More interested in the tech than human life, [PAWN_pronoun] boarded the vault's spacecraft and departed to find more relics to abuse.</description>
  <skillGains>
    <Construction>2</Construction>
    <Intellectual>3</Intellectual>
    <Shooting>2</Shooting>
    <Melee>2</Melee>
    <Crafting>2</Crafting>
  </skillGains>
  <workDisables>Caring, Artistic</workDisables>
  <spawnCategories>
    <li>Offworld</li>
    <li>Cult</li>
  </spawnCategories>
  <bodyTypeGlobal>Female</bodyTypeGlobal>
  <shuffleable>False</shuffleable>
  <possessions>
    <Apparel_CeremonialCultistMask MayRequire="Ludeon.RimWorld.Anomaly">1</Apparel_CeremonialCultistMask>
  </possessions>
</BackstoryDef>
```

### Adulthood - Forced Psychopath

```xml
<BackstoryDef>
  <defName>CorporateFixer36</defName>
  <slot>Adulthood</slot>
  <title>corporate fixer</title>
  <titleShort>fixer</titleShort>
  <description>[PAWN_nameDef] was a fixer for an energy corporation. [PAWN_pronoun] work required knowing when to push, when to pull, when to back down and when to strike. The job fit [PAWN_possessive] natural lack of empathy.\n\nWhile violence was unusual, [PAWN_nameDef] stayed prepared for anything.</description>
  <bodyTypeMale>Male</bodyTypeMale>
  <bodyTypeFemale>Female</bodyTypeFemale>
  <spawnCategories><li>ImperialCommon</li></spawnCategories>
  <skillGains>
    <Social>4</Social>
    <Shooting>3</Shooting>
    <Melee>3</Melee>
  </skillGains>
  <requiredWorkTags>
    <li>Social</li>
    <li>Violent</li>
  </requiredWorkTags>
  <forcedTraits>
    <Psychopath>0</Psychopath>
  </forcedTraits>
</BackstoryDef>
```

---

## Offworld Category Analysis

The vanilla `Offworld` spawnCategory contains **~382 backstories** across Core and Royalty. An audit of every backstory in this pool reveals a significant thematic split:

| Classification   | Count | %   | Description                                                        |
| ---------------- | ----: | --: | ------------------------------------------------------------------ |
| **Another World** | ~132 | 35% | Explicitly planetary — references homeworld, urbworld, glitterworld, medieval world, rimworld, etc. |
| **Spacer**        |  ~99 | 26% | Lives/works in space — ships, stations, orbital bases, void, deep space rigs |
| **Ambiguous**     | ~148 | 39% | Generic occupations with no spatial indicators (bartender, nurse, farmer, etc.) |

### Key Findings

1. **Offworld was designed pre-Odyssey**, when "not from this rimworld" was the only relevant distinction. The category conflates planetary origin (urbworld chef, medieval blacksmith) with spacefaring life (starship janitor, space marine).

2. **Only ~26% of Offworld backstories** describe someone who plausibly grew up or worked in space. Many Ambiguous entries lean planetary by implication (ranchers, loggers, foresters — activities requiring land and atmosphere).

3. **With the Odyssey DLC** introducing orbital settlements and the Traders Guild faction, there are now significant numbers of pawns who thematically live their entire lives in orbit. The current Offworld pool will frequently produce backstories referencing "my homeworld," "the planet," "urbworld streets" — potentially jarring for orbital station inhabitants.

### Possible Directions

| Approach | Pros | Cons |
| -------- | ---- | ---- |
| **New `Spacer`/`Orbital` spawnCategory** | Full thematic control; doesn't affect other factions; can be weighted heavily for TG | Requires enough backstories to avoid repetition; need to adjust faction backstoryFilters |
| **Reclassify existing spacer backstories** | Enriches the new category immediately with ~99 vanilla entries; benefits all spacer factions | Requires XML patches on vanilla BackstoryDefs; mod compatibility concerns |
| **Hybrid: new category + reclassify** | Best of both; new BTG backstories in new category, patch existing spacer ones to dual-list | Most flexible but most work; needs careful testing |
| **Expand Offworld only** | Simplest; no faction filter changes needed | Dilutes slowly (382 pool is large); doesn't solve planetary backstories appearing on orbital pawns |
| **Accept status quo** | Zero effort | Ambiguous backstories aren't wrong; truly jarring ones (medieval farmer) are a minority |

### If Adding a New Category

The faction's `backstoryFilters` could be patched to weight the new category heavily:

```xml
<backstoryFilters>
  <li>
    <categories><li>Spacer</li></categories>
    <commonality>0.7</commonality>
  </li>
  <li>
    <categories><li>Offworld</li></categories>
    <commonality>0.25</commonality>
  </li>
  <li>
    <categories><li>Outlander</li></categories>
    <commonality>0.05</commonality>
  </li>
</backstoryFilters>
```

This would also benefit other spacer-inclined factions (e.g., Pirates who operate from ships) if they were similarly reweighted. The ~99 existing spacer backstories could be patched to include the new category alongside their existing `Offworld` tag, preserving backward compatibility.

### Source Data

- **Shuffled Offworld**: 112 backstories (4 Spacer, 49 Another World, 59 Ambiguous)
- **Solid Offworld**: 267 backstories (~95 Spacer, ~83 Another World, ~89 Ambiguous)
- **Solid skews more spacer** than Shuffled (unique/named characters more often have specific space settings)

---

## Backstory Ideas for TradersGuild

Potential role categories to fill based on orbital station operations:

### Trade & Commerce

- Cargo appraiser, freight broker, commodity trader, auction clerk
- Trade negotiator, contract arbitrator, tariff assessor
- Black market fence, smuggler-turned-legitimate, sanctions runner

### Station Operations

- Station engineer, hull technician, pressure seal inspector
- Reactor technician, power grid operator, solar array maintainer
- Life support tech, atmospheric processor, waste recycler
- Docking coordinator, shuttle dispatcher, traffic controller

### Military & Security

- Station security chief, vault guard, customs enforcer
- Turret technician, weapons calibrator, sentry drone handler
- Counter-boarding specialist, perimeter watch, airlock marshal

### Medical & Science

- Station medic, vacuum trauma surgeon, radiation therapist
- Hydroponics botanist, nutrition chemist, pharmaceutical compounder

### Administration & Leadership

- Station administrator, logistics coordinator, supply chain analyst
- Guild representative, diplomatic attache, cultural liaison
- Records keeper, data archivist, comms officer

### Skilled Trades

- Shipwright, hull welder, EVA repair tech
- Machinist, fabricator, electronics assembler
- Plumber/pipe fitter, conduit installer, structural welder

### Service & Support

- Station cook, mess hall manager, provisions buyer
- Quartermaster, inventory clerk, storehouse manager
- Janitor/cleaner (low-gravity specialist), maintenance crew
