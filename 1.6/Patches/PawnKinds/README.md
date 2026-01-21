# PawnKinds Patches

Upgrades TradersGuild pawns to spacer-level gear, fixing the vanilla issue where a wealthy spacer faction uses industrial-tier equipment.

## Design Constraints

**Critical:** The vanilla PawnKindDef hierarchy shares abstract bases with Salvager (space pirate) pawns:

```
TradersGuildBase (Abstract)
├── TradersGuild_Citizen       ← PATCH THIS
├── TradersGuild_Child         ← PATCH THIS
├── TradersGuildMidTierBase (Abstract)
│   ├── TradersGuildGunnerBase (Abstract)
│   │   ├── TradersGuild_Gunner    ← PATCH THIS
│   │   └── Salvager_Pirate        ← DO NOT AFFECT
│   ├── TradersGuildSlasherBase (Abstract)
│   │   ├── TradersGuild_Slasher   ← PATCH THIS
│   │   └── Salvager_Scrapper      ← DO NOT AFFECT
│   └── TradersGuild_Heavy         ← PATCH THIS
└── TradersGuildEliteTierBase (Abstract)
    ├── TradersGuild_Magister      ← PATCH THIS
    └── TradersGuildEliteBase (Abstract)
        ├── TradersGuild_Elite     ← PATCH THIS
        └── Salvager_Elite         ← DO NOT AFFECT
```

**Always target concrete defs only** to avoid buffing space pirates.

## Patch Summary

| PawnKind | Combat Power | Armor                                   | Weapons                            | Implants   | Quality   |
| -------- | -----------: | --------------------------------------- | ---------------------------------- | ---------- | --------- |
| Citizen  |     45 -> 45 | Vacsuit + Helmet                        | —                                  | —          | —         |
| Child    |            — | Child Vacsuit + Helmet + Shield         | —                                  | —          | —         |
| Gunner   |     85 → 110 | Marine Armor                            | SpacerGun                          | 40%        | Good      |
| Slasher  |    140 → 160 | Shield + Marine Armor                   | MedievalMeleeAdvanced (+Ultratech) | 40%        | Good      |
| Heavy    |    140 → 170 | Marine Armor                            | GunHeavy + SpacerGun               | 40%        | Good      |
| Elite    |    130 → 180 | Cataphract (Royalty) or Marine          | SpacerGun + GunHeavy               | 65%, max 4 | Excellent |
| Magister |    130 → 200 | Prestige Cataphract (Royalty) or Marine | SpacerGun + GunHeavy               | 85%, max 4 | Excellent |

All combat pawns have 100% gear health (was 70-320%) and 50-80% weapon biocoding.

## DLC Handling

**Royalty DLC** (armor/weapons):

```xml
<Operation Class="PatchOperationConditional">
  <xpath>/Defs/ThingDef[defName="Apparel_ArmorCataphract"]</xpath>
  <match><!-- Use Cataphract --></match>
  <!-- nomatch: keep PowerArmor -->
</Operation>
```

**Biotech DLC** (xenotypes):

```xml
<Operation Class="PatchOperationAdd" MayRequire="Ludeon.RimWorld.Biotech">
  ...
</Operation>
```

## Key Fields

| Field                         | Purpose                                                           |
| ----------------------------- | ----------------------------------------------------------------- |
| `apparelTags`                 | Pool filter for random apparel (use `Inherit="False"` to replace) |
| `apparelRequired`             | Forced items (armor, vacuum gear)                                 |
| `apparelMoney`                | Budget for apparel selection                                      |
| `weaponTags`                  | Pool filter for weapons                                           |
| `weaponMoney`                 | Budget for weapons                                                |
| `techHediffsChance`           | Probability of implants (0.0-1.0)                                 |
| `techHediffsMoney`            | Budget for implants                                               |
| `techHediffsMaxAmount`        | Cap on number of implants                                         |
| `techHediffsRequired`         | Guaranteed implants (Magister only)                               |
| `itemQuality`                 | Normal, Good, Excellent, Masterwork, Legendary                    |
| `gearHealthRange`             | Condition range (1~1 = 100%)                                      |
| `biocodeWeaponChance`         | Weapon biocoding rate                                             |
| `combatEnhancingDrugsChance`  | Drug usage probability                                            |
| `xenotypeSet`                 | Override faction xenotype distribution                            |
| `specificApparelRequirements` | Force specific materials (synthread/hyperweave)                   |

## Files

| File                             | Purpose                             |
| -------------------------------- | ----------------------------------- |
| `PawnKinds_Citizen.xml`          | Adult civilian vacuum protection    |
| `PawnKinds_Child.xml`            | Child vacuum protection (Biotech)   |
| `PawnKinds_Gunner.xml`           | Ranged fighter upgrades             |
| `PawnKinds_Slasher.xml`          | Melee fighter upgrades              |
| `PawnKinds_Heavy.xml`            | Heavy weapons specialist upgrades   |
| `PawnKinds_Elite.xml`            | Elite guard upgrades                |
| `PawnKinds_Magister.xml`         | Faction leader upgrades             |
| `PawnKinds_ApparelMaterials.xml` | Force synthread/hyperweave clothing |
