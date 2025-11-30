# Phase 3.3: Enhanced TradersGuild Pawn Generation

## Research Summary

### TradersGuild Faction Configuration

**Source:** `/Data/Odyssey/Defs/FactionDefs/Factions_Misc.xml`

| Property     | Value             |
| ------------ | ----------------- |
| Tech Level   | Spacer            |
| Layer        | Orbit only        |
| Leader Title | Magister of Trade |

**Xenotypes (Biotech):** Hussar 5%, Genie 10%, Starjack 25%, Baseliner 60%

**Ideology:** Shipborn meme, Astropolitan culture, Techist style

---

### PawnKindDef Inheritance Hierarchy

**Source:** `/Data/Odyssey/Defs/PawnKinds/PawnKinds_TradersGuild.xml`

```
TradersGuildBase (Abstract)
├── TradersGuild_Citizen
├── TradersGuild_Child (Biotech)
├── TradersGuildMidTierBase (Abstract)
│   ├── TradersGuildGunnerBase (Abstract)
│   │   ├── TradersGuild_Gunner
│   │   └── Salvager_Pirate  ← SHARED!
│   ├── TradersGuildSlasherBase (Abstract)
│   │   ├── TradersGuild_Slasher
│   │   └── Salvager_Scrapper  ← SHARED!
│   └── TradersGuild_Heavy
└── TradersGuildEliteTierBase (Abstract)
    ├── TradersGuild_Magister (leader)
    └── TradersGuildEliteBase (Abstract)
        ├── TradersGuild_Elite
        └── Salvager_Elite  ← SHARED!
```

**CRITICAL:** Abstract bases are SHARED with Salvager pawns! Patching abstracts would
unintentionally buff space pirates. Must patch concrete defs (TradersGuild_Gunner, etc.) only.

---

### Vanilla/Odyssey PawnKind Values

| PawnKind |  CP | Weapon $ | Apparel $ | Implant % | Quality | Vacuum Gear      |
| -------- | --: | -------: | --------: | --------: | ------- | ---------------- |
| Citizen  |  45 |  150-250 |   200-400 |        6% | Normal  | Helmet only      |
| Gunner   |  85 |  330-650 | 1000-1500 |       15% | Normal  | Full vacsuit     |
| Slasher  | 140 |  200-500 |  300-1400 |       15% | Normal  | Vacsuit + Shield |
| Heavy    | 140 |     1200 |   200-350 |       15% | Normal  | Full vacsuit     |
| Elite    | 130 | 500-1400 | 2500-3500 |       35% | Normal  | Power Armor      |
| Magister | 130 | 500-1400 | 2500-3500 |       35% | Normal  | Power Armor      |

### Vanilla/Odyssey Tag Usage (Problem: Industrial-Level Gear)

**Weapon Tags:**

- Gunner: `Gun` (includes industrial weapons)
- Slasher: `MedievalMeleeDecent`, `MedievalMeleeAdvanced`
- Heavy: `GunHeavy`, `GunSingleUse`, `Flamethrower`
- Elite: `IndustrialGunAdvanced`

**Apparel Tags:**

- Mid-tier: `IndustrialBasic`, `IndustrialAdvanced`, `IndustrialMilitaryBasic`, `IndustrialMilitaryAdvanced`
- Elite adds: `SpacerMilitary`

---

### Tag-Based Gear Selection System

RimWorld uses **tag-based pools** - mods adding items with matching tags are automatically available:

| Field               | Purpose                                    |
| ------------------- | ------------------------------------------ |
| `apparelRequired`   | Items ALWAYS equipped (vacsuit for vacuum) |
| `apparelTags`       | Pool filter for random apparel selection   |
| `apparelMoney`      | Budget for apparel                         |
| `weaponTags`        | Pool filter for weapons                    |
| `weaponMoney`       | Budget for weapons                         |
| `techHediffsTags`   | Pool filter for implants                   |
| `techHediffsChance` | Probability of implants                    |
| `techHediffsMoney`  | Budget for implants                        |

**This mirrors our orbital trader rotation** - we use pools that mods can extend.

### Available Spacer-Level Tags

**Weapons:**
| Tag | Examples |
|-----|----------|
| `SpacerGun` | Charge rifle, charge lance |
| `GunHeavy` | Minigun, LMG |
| `UltratechMelee` | Monosword, zeushammer (Royalty) |

**Apparel:**
| Tag | Examples |
|-----|----------|
| `SpacerMilitary` | Vacsuit, marine armor, recon armor |

**Implants:**
| Tag | Examples |
|-----|----------|
| `Advanced` | Bionic eye, arm, leg, heart |
| `ImplantEmpireCommon` | Coagulator, armorskin gland |
| `ImplantEmpireRoyal` | Higher-tier (Royalty) |

---

### Controllable Fields via XML Patches

**Gear Selection:**

- `apparelMoney`, `weaponMoney` - Budget ranges
- `apparelTags`, `weaponTags` - Pool filters (REPLACE to remove industrial tags)
- `apparelRequired` - Forced items
- `apparelAllowHeadgearChance` - Helmet probability

**Implants/Bionics:**

- `techHediffsChance` - Probability (0.0-1.0)
- `techHediffsMoney` - Budget range
- `techHediffsTags` - Pool filter
- `techHediffsMaxAmount` - Cap on implants
- `techHediffsRequired` - Guaranteed implants (like Empire Cataphract)

**Quality & Condition:**

- `itemQuality` - Normal, Good, Excellent, Masterwork, Legendary
- `gearHealthRange` - Condition range

**Combat:**

- `combatPower` - Raid point value
- `combatEnhancingDrugsChance` - Drug usage
- `combatEnhancingDrugsCount` - Drug count
- `biocodeWeaponChance` - Biocoding rate

**Skills & Recruitment:**

- `skills` - Skill ranges
- `disallowedTraits` - Blocked traits
- `initialWillRange`, `initialResistanceRange` - Recruitment difficulty

**Xenotypes (Biotech):**

- `xenotypeSet` - Override faction xenotype chances

---

### Reference: Empire Elite Pawns

| Empire Kind   |  CP |  Apparel $ | Implant % | Required Implants            |
| ------------- | --: | ---------: | --------: | ---------------------------- |
| Cataphract    | 150 | 7000-10000 |       30% | ArmorskinGland               |
| Stellic Guard | 150 |   Prestige |      100% | BionicEye x2, StoneskinGland |

**Pattern:** Elite pawns have **required implants** for guaranteed valuable loot.

---

### Mod Compatibility

**Why XML patches are best:**

1. TradersGuild abstracts are faction-specific (no cross-contamination)
2. Mods patching TradersGuild defs (oxygen packs, etc.) will apply to our patched versions
3. Future DLC changes to base defs flow through inherited fields
4. Lower maintenance than custom PawnKindDefs

---

## Implementation Plan

### Goal

Upgrade all TradersGuild combat pawns to **spacer-level only** - no industrial gear.

### Design Decisions

- **No new pawnkinds** - buff existing ones, scale up stats case-by-case
- **No required implants** - keep chance-based (can add later)
- **60% weapon biocoding** - high challenge, loot reward from settlement not pawns
- **Armor upgrades:**
  - Gunner/Slasher: Recon armor + Vacsuit Helmet (layer conflict prevents full vacsuit)
  - Heavy: Power Armor (aka Marine armor) + Power Armor Helmet
  - Elite: Cataphract (Royalty) or Power Armor (fallback)
  - Magister: Prestige Cataphract (Royalty) or Power Armor (fallback)
- **Melee weapons:**
  - Remove MedievalMeleeDecent (too low tech)
  - Keep MedievalMeleeAdvanced (Longsword, Spear) - high budget + quality = plasteel/uranium
  - Add UltratechMelee conditionally (Royalty DLC)
- **Mechanoid option (Biotech):** Consider adding Scyther (`Mech_Scyther`) spawn chance, reduce Slasher weight

### Layer Conflict Note

Recon Armor and Vacsuit both use `Middle + Shell` layers - cannot stack.

- Recon Armor: 0.3 vacuum resistance
- Vacsuit: 0.32 vacuum resistance
- Vacsuit Helmet: 0.69 vacuum resistance (main protection)

**Solution:** Use Recon Armor + Vacsuit Helmet for mid-tier (combat armor + vacuum helmet).

### XPath Targets

**IMPORTANT:** Target CONCRETE defs only, not abstract bases (shared with Salvagers).

| Concrete Def            | Role           | Patches Applied                                          |
| ----------------------- | -------------- | -------------------------------------------------------- |
| `TradersGuild_Gunner`   | Ranged fighter | SpacerGun, Recon armor, 40% implants                     |
| `TradersGuild_Slasher`  | Melee fighter  | MedievalMeleeAdvanced + UltratechMelee\*, Shield + Recon |
| `TradersGuild_Heavy`    | Heavy weapons  | GunHeavy + SpacerGun, Power armor                        |
| `TradersGuild_Elite`    | Elite guard    | SpacerGun + GunHeavy, Cataphract\*, 65% implants         |
| `TradersGuild_Magister` | Faction leader | Best gear, Prestige Cataphract\*, 85% implants           |

_\* Royalty DLC required, falls back to base game alternatives_

---

## Proposed Values by Tier

### Mid-Tier Shared (TradersGuildMidTierBase)

| Field                        | Current  | Proposed  | Notes                                 |
| ---------------------------- | -------- | --------- | ------------------------------------- |
| `techHediffsChance`          | 0.15     | 0.40      | 40% implant chance                    |
| `techHediffsMoney`           | 700~1200 | 1000~2000 | Higher budget                         |
| `biocodeWeaponChance`        | 0.2      | **0.80**  | High biocoding - loot from settlement |
| `combatEnhancingDrugsChance` | 0.15     | 0.35      | More combat drugs                     |
| `itemQuality`                | (Normal) | Good      | Better gear quality                   |
| `gearHealthRange`            | 0.7~3.2  | 0.9~1.5   | Better condition                      |

### Gunner (TradersGuildGunnerBase)

| Field             | Current        | Proposed                 | Notes                   |
| ----------------- | -------------- | ------------------------ | ----------------------- |
| `combatPower`     | 85             | 110                      | Increased threat        |
| `weaponMoney`     | 330~650        | 600~1200                 | Better weapons          |
| `apparelMoney`    | 1000~1500      | 2000~3000                | Better apparel budget   |
| `weaponTags`      | `Gun`          | `SpacerGun`              | Spacer weapons only     |
| `apparelTags`     | Industrial\*   | `SpacerMilitary`         | Spacer apparel only     |
| `apparelRequired` | Vacsuit+Helmet | ReconArmor+VacsuitHelmet | Recon body + vac helmet |

### Slasher (TradersGuildSlasherBase)

| Field             | Current                   | Proposed                                                  | Notes                            |
| ----------------- | ------------------------- | --------------------------------------------------------- | -------------------------------- |
| `combatPower`     | 140                       | 160                                                       | Increased threat                 |
| `weaponMoney`     | 200~500                   | 500~1200                                                  | High budget for plasteel/uranium |
| `apparelMoney`    | 300~1400                  | 2000~3500                                                 | Better apparel budget            |
| `weaponTags`      | MedievalMelee\*           | `MedievalMeleeAdvanced` only + `UltratechMelee` (Royalty) | Remove Decent                    |
| `apparelTags`     | Industrial\*              | `SpacerMilitary`                                          | Spacer apparel only              |
| `apparelRequired` | ShieldBelt+Vacsuit+Helmet | ShieldBelt+ReconArmor+VacsuitHelmet                       | Recon body                       |

### Heavy (TradersGuild_Heavy)

| Field             | Current        | Proposed                    | Notes                |
| ----------------- | -------------- | --------------------------- | -------------------- |
| `combatPower`     | 140            | 170                         | Increased threat     |
| `weaponMoney`     | 1200           | 1500~2500                   | Better heavy weapons |
| `apparelMoney`    | 200~350        | 3000~5000                   | Much better budget   |
| `weaponTags`      | GunHeavy etc   | Keep + add `SpacerGun`      | Add spacer option    |
| `apparelTags`     | Industrial\*   | `SpacerMilitary`            | Spacer only          |
| `apparelRequired` | Vacsuit+Helmet | PowerArmor+PowerArmorHelmet | Full power armor     |

### Elite Tier Shared (TradersGuildEliteTierBase)

| Field                        | Current   | Proposed  | Notes                                 |
| ---------------------------- | --------- | --------- | ------------------------------------- |
| `techHediffsChance`          | 0.35      | 0.65      | 65% implant chance                    |
| `techHediffsMoney`           | 1000~1200 | 2000~4000 | Much higher budget                    |
| `techHediffsMaxAmount`       | (default) | 4         | Up to 4 implants                      |
| `combatEnhancingDrugsChance` | 0.8       | 0.90      | Almost always has drugs               |
| `combatEnhancingDrugsCount`  | 1~2       | 1~3       | More drugs                            |
| `biocodeWeaponChance`        | 0.3       | **0.80**  | High biocoding - loot from settlement |
| `itemQuality`                | (Normal)  | Excellent | High quality gear                     |
| `gearHealthRange`            | 1.0       | 1.0       | Perfect condition                     |

### Elite (TradersGuildEliteBase)

| Field             | Current               | Proposed                                      | Notes                |
| ----------------- | --------------------- | --------------------------------------------- | -------------------- |
| `combatPower`     | 130                   | 180                                           | Significant increase |
| `weaponMoney`     | 500~1400              | 1500~3000                                     | Better weapons       |
| `apparelMoney`    | 2500~3500             | 6000~9000                                     | Much better budget   |
| `weaponTags`      | IndustrialGunAdvanced | `SpacerGun`, `GunHeavy`                       | Spacer + heavy       |
| `apparelRequired` | PowerArmor+Helmet     | Cataphract (Royalty) or PowerArmor (fallback) |

### Magister (TradersGuild_Magister)

| Field               | Current           | Proposed                                              | Notes               |
| ------------------- | ----------------- | ----------------------------------------------------- | ------------------- |
| `combatPower`       | 130               | 200                                                   | Faction leader buff |
| `weaponMoney`       | 500~1400          | 2000~4000                                             | Best weapons        |
| `apparelMoney`      | 2500~3500         | 8000~12000                                            | Highest budget      |
| `techHediffsChance` | 0.35              | 0.85                                                  | Almost guaranteed   |
| `apparelRequired`   | PowerArmor+Helmet | PrestigeCataphract (Royalty) or PowerArmor (fallback) |

---

## DLC Handling

### Royalty DLC

Use `MayRequire="Ludeon.RimWorld.Royalty"` for:

- Cataphract armor (Elite) - in `/Data/Royalty/Defs/`
- Prestige Cataphract (Magister) - in `/Data/Royalty/Defs/`
- `UltratechMelee` weapon tag for Slashers (monosword, zeushammer)

**Fallback:** If Royalty not present, Elite/Magister keep Power Armor.

### Biotech DLC

Use `MayRequire="Ludeon.RimWorld.Biotech"` for:

- **Scyther mechanoids** (`Mech_Scyther`) - add to pawnGroupMakers as TradersGuild faction mechs
- Xenotype overrides via `xenotypeSet` - increase Hussar % for Heavy/Elite

### Scyther Integration (Biotech)

Add Scyther spawn chance to Settlement pawnGroupMaker:

- Reduce Slasher weight slightly (7 → 5)
- Add `Mech_Scyther` with weight 3-4
- Scythers are excellent melee (150 CP) and thematically fit wealthy faction buying mechanoids

**Note:** Need to verify if mechanoids can be added to non-mechanitor faction pawnGroupMakers, or if this requires C# patching.

---

## Patch Strategy

### PatchOperationSequence with Conditionals

```xml
<!-- Elite armor: Cataphract if Royalty, else keep PowerArmor -->
<Operation Class="PatchOperationConditional">
  <xpath>/Defs/ThingDef[defName="Apparel_ArmorCataphract"]</xpath>
  <match>
    <!-- Royalty present: use Cataphract -->
    <Operation Class="PatchOperationReplace">
      <xpath>...</xpath>
      <value>...</value>
    </Operation>
  </match>
  <nomatch>
    <!-- No Royalty: keep PowerArmor (no change needed) -->
  </nomatch>
</Operation>
```

### Tag Replacement (Remove Industrial)

To ensure spacer-only gear, REPLACE entire tag lists rather than ADD:

```xml
<Operation Class="PatchOperationReplace">
  <xpath>/Defs/PawnKindDef[@Name="TradersGuildGunnerBase"]/weaponTags</xpath>
  <value>
    <weaponTags>
      <li>SpacerGun</li>
    </weaponTags>
  </value>
</Operation>
```

---

## Files to Modify

| Location                                                | Purpose                       |
| ------------------------------------------------------- | ----------------------------- |
| `BetterTradersGuild/Patches/PawnKinds_TradersGuild.xml` | XML patches for pawn upgrades |

### Source Files (Read-Only Reference)

- `/Data/Odyssey/Defs/PawnKinds/PawnKinds_TradersGuild.xml`
- `/Data/Odyssey/Defs/FactionDefs/Factions_Misc.xml`
- `/Data/Royalty/Defs/PawnKinds/PawnKinds_Empire.xml` (elite patterns)
