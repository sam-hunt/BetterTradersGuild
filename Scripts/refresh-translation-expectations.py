#!/usr/bin/env python3
# Better Traders Guild's config shim over the shared sidecar-refresh engine
# (l10n/refresh/refresh_expectations.py — the rimworld-l10n submodule),
# which drives the L10nProbe dev mod (source at l10n/probe/; build/deploy it
# only from the canonical ~/dev/rimworld-l10n checkout). The engine holds all
# logic; this file holds only this repo's config and the rationale behind it.
# Usage is unchanged (game must be closed):
#   python3 Scripts/refresh-translation-expectations.py [--no-launch]
# If l10n/ is empty, run: git submodule update --init

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "refresh"))
import refresh_expectations as engine  # noqa: E402  (import after sys.path edit)

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

engine.PACKAGE_ID = "shunter.bettertradersguild"

# RATIONALE: Odyssey is a hard dependency (About.xml's modDependencies);
# Biotech qualifies via the 1.6/Mods/Biotech compat root, which ships
# BTG_ConfigureStartingPawnsXenotypes (ScenPartDef) and its DefInjected
# entries. UniqueMeleeWeapons rides along because the
# 1.6/Mods/UniqueMeleeWeapons compat root's BTG_SilverInlayMelee defs only
# load (and therefore only dump) with it active. This repo is the only
# sidecar-bearing mod in its own boot. See the engine's header for the
# general membership rule, the lowercase-id warning, and the pinning
# rationale; order is load order, the probe last.
engine.CANONICAL_ACTIVE_MODS = [
    "brrainz.harmony",
    "ludeon.rimworld",
    "ludeon.rimworld.biotech",
    "ludeon.rimworld.odyssey",
    "shunter.uniquemeleeweapons",
    "shunter.bettertradersguild",
    "shunter.l10nprobe",
]

raise SystemExit(engine.main())
