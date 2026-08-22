#!/usr/bin/env python3
# Pre-release integration smoke test: boots the real game once with BTG plus
# every mod BTG integrates with, on a pinned minimal list where the baseline
# is a clean log, then classifies every Player.log error/warning by origin
# and fails on anything attributed to BTG or an integration seam. Thin shim
# over the shared engine in l10n/smoke/startup_smoke.py (see its header for
# mechanics and the v1.1.0 CWTL incident this exists to catch).
#
# Run this before every release, with the game closed:
#   python3 Scripts/integration-smoke-test.py              # boot + scan
#   python3 Scripts/integration-smoke-test.py --no-launch  # rescan last log
#   python3 Scripts/integration-smoke-test.py --strict     # any error fails

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "smoke"))
import startup_smoke as engine  # noqa: E402

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

engine.PACKAGE_ID = "shunter.bettertradersguild"

# RATIONALE: Odyssey is BTG's hard dep; Biotech is MayRequire-gated (xenotype
# ScenPart) and VREA's own hard dep. The five optional mods are exactly the
# Integrations/ roster - each activates conditional patches or reflection
# that never runs otherwise: HAR (outfit stand finalizer), VEF (PipeSystem
# landing pad pipes), VREA (android stand gate), CWTL (attack CanAttack
# gate), UMW (silver-inlay melee trait + nursery knife). VEF is also VREA's
# hard dep and must load before it. Probe last (auto-quit).
engine.SMOKE_ACTIVE_MODS = [
    "brrainz.harmony",
    "ludeon.rimworld",
    "ludeon.rimworld.biotech",
    "ludeon.rimworld.odyssey",
    "erdelf.humanoidalienraces",
    "oskarpotocki.vanillafactionsexpanded.core",
    "vanillaracesexpanded.android",
    "kearril.choosewheretoland",
    "shunter.uniquemeleeweapons",
    "shunter.bettertradersguild",
    "shunter.l10nprobe",
]

engine.OWN_PATTERNS = ["BetterTradersGuild", "[Better Traders Guild]", "BTG_"]

# The other mod's namespaces/prefixes: an error mentioning any of these gates
# the test even when the exception fires inside their code - the v1.1.0
# incident surfaced as a red error inside CWTL's own static ctor.
engine.INTEGRATION_PATTERNS = {
    "CWTL": ["ChooseWhereToLand", "CWTL"],
    "HAR": ["AlienRace", "HumanoidAlienRaces"],
    "VREA": ["VREAndroids"],
    "VEF/PipeSystem": ["PipeSystem", "VEF."],
    "UMW": ["UniqueMeleeWeapons", "UMW_"],
}

raise SystemExit(engine.main())
