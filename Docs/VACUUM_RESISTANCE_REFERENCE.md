# Vacuum Resistance Stat Reference

Quick reference for the VacuumResistance pawn stat introduced in the Odyssey DLC.

## Stat Definition

| Property | Value |
|----------|-------|
| defName | `VacuumResistance` |
| Range | 0% - 100% |
| Default | 0% |
| Worker | `StatWorker_VacuumResistance` |

## How It Works

### Vacuum Exposure Damage

Every 60 ticks (~1 second), for pawns in a vacuum biome with ≥50% vacuum level:

```
severityGain = 0.02 × vacuumLevel × max(0, 1 - VacuumResistance)
```

The `VacuumExposure` hediff has `lethalSeverity: 1` - pawns die at 100% severity.

| Resistance | Effect |
|------------|--------|
| 0% | Full damage (0.02/sec at 100% vacuum) |
| 50% | Half damage |
| 100% | **Immune** - no exposure damage |

### Vacuum Burns

Separate damage system targeting exposed body parts. Pawns with **<75% VacuumResistance** can receive vacuum burns to unprotected skin.

### Key Thresholds

| Threshold | Effect |
|-----------|--------|
| <50% | Takes vacuum exposure damage |
| <75% | Can receive vacuum burns |
| ≥90% | Game considers pawn "protected enough" for apparel generation |
| 100% | Complete immunity |

## Sources of VacuumResistance

### Genes (Odyssey + Biotech)

| Gene | Resistance | Additional Effects |
|------|------------|-------------------|
| Breathless (`VacuumResistance_Total`) | +100% | Immune to burns, tox gas, toxic environment. Archite gene. |
| Vacuum Resistant (`VacuumResistance_Partial`) | +45% | Immune to vacuum burns |

### Implants (Odyssey + Royalty)

| Implant | Resistance | Notes |
|---------|------------|-------|
| Vacskin Gland | +85% | -10% movement, immune to vacuum burns |

---

## Apparel with VacuumResistance

### Headgear

All values shown are the effective VacuumResistance stat offset when equipped.

| Item | defName | Resistance | Source |
|------|---------|------------|--------|
| Vacsuit helmet | `Apparel_VacsuitHelmet` | **69%** | Odyssey |
| Cataphract helmet | `Apparel_ArmorHelmetCataphract` | 68% | Royalty |
| Prestige cataphract helmet | `Apparel_ArmorHelmetCataphractPrestige` | 68% | Royalty |
| Marine helmet | `Apparel_PowerArmorHelmet` | 67% | Core |
| Prestige marine helmet | `Apparel_ArmorMarineHelmetPrestige` | 67% | Royalty |
| Mechcommander helmet | `Apparel_ArmorHelmetMechCommander` | 67% | Biotech |
| Recon helmet | `Apparel_ArmorHelmetRecon` | 65% | Core |
| Prestige recon helmet | `Apparel_ArmorHelmetReconPrestige` | 65% | Royalty |

**Note:** The following headgear inherit from vacuum-resistant parents but explicitly **override to 0%**:
- Gunlink (`Apparel_Gunlink`) - Royalty
- Integrator headset (`Apparel_IntegratorHeadset`) - Biotech

### Body Armor

| Item | defName | Resistance | Source |
|------|---------|------------|--------|
| Vacsuit | `Apparel_Vacsuit` | **32%** | Odyssey |
| Kid vacsuit | `Apparel_VacsuitChildren` | **32%** | Odyssey+Biotech |
| Marine armor | `Apparel_PowerArmor` | 30% | Core |
| Prestige marine armor | `Apparel_ArmorMarinePrestige` | 30% | Royalty |
| Mechlord suit | `Apparel_MechlordSuit` | 30% | Biotech |
| Cataphract armor | `Apparel_ArmorCataphract` | 30% | Royalty |
| Prestige cataphract armor | `Apparel_ArmorCataphractPrestige` | 30% | Royalty |
| Phoenix armor | `Apparel_ArmorCataphractPhoenix` | 30% | Royalty |

**Note:** The following body armor does **NOT** provide VacuumResistance:
- Recon armor (all variants)
- Locust armor
- Flak vest/jacket
- All fabric/leather clothing

---

## Apparel Tags and PawnKindDef

**There is no specific "PowerArmor" apparel tag in vanilla RimWorld.**

| Tag | Used For | Includes |
|-----|----------|----------|
| `SpacerMilitary` | Pawn generation (`apparelTags`) | Marine, Recon, Cataphract, Locust, Phoenix + mod armors |
| `HiTechArmor` | Trade stock (`tradeTags`) | Not usable for pawn apparel generation |
| `PrestigeCombatGear` | Pawn generation | Prestige variants only (Royalty) |

### Mod Compatibility

Mods like [Vanilla Armour Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1814988282) add armors (e.g., Siegebreaker) that:
- Inherit from `ApparelArmorPowerBase` (same 30% vacuum resistance as Marine)
- Use `SpacerMilitary` tag (same as vanilla power armor)

### PawnKindDef Strategies

| Approach | Pros | Cons |
|----------|------|------|
| `apparelRequired` with defNames | Guaranteed vacuum protection | No mod armor variety |
| `apparelTags: SpacerMilitary` | Includes mod armors automatically | Could spawn Recon armor (0% vacuum) |

**Recommendation:** Use `apparelRequired` for vacuum-critical pawns. The `SpacerMilitary` tag includes Recon armor which has zero body vacuum resistance.

---

## Recommended Loadouts for Space

To reach ≥90% VacuumResistance (the "protected" threshold):

| Combination | Total | Notes |
|-------------|-------|-------|
| Vacsuit + Vacsuit helmet | 101% | Full protection, slow (-1.25 move) |
| Marine armor + Marine helmet | 97% | Good combat protection |
| Cataphract armor + Cataphract helmet | 98% | Best armor, slow |
| Mechlord suit + Mechcommander helmet | 97% | For mechanitors |
| Vacskin gland (implant) | 85% | No gear needed, but needs helmet for 100% |
| Vacskin gland + any 65%+ helmet | 150%+ | Best flexibility |

### For Traders Guild Faction Pawns

Recommended combinations that balance vacuum protection with faction aesthetics:

**High Protection (≥90%):**
- Marine armor + Marine helmet (97%) - Military crew
- Cataphract armor + Cataphract helmet (98%) - Elite guards

**Moderate Protection (60-70%):**
- Recon helmet alone (65%) - Light protection, good visibility
- Marine helmet alone (67%) - Better protection

**Civilian (32-69%):**
- Vacsuit + no helmet (32%) - Workers
- Vacsuit helmet alone (69%) - Officers without combat gear
