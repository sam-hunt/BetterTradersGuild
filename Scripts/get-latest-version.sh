#!/bin/bash

# Returns the latest minor game version supported by this mod.
# Parses LoadFolders.xml to find all <vX.Y> tags and returns the highest version.
#
# Usage:
#   ./get-latest-version.sh          # Outputs: 1.6 (or whatever the latest is)
#   VERSION=$(./get-latest-version.sh)  # Capture in variable

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_ROOT="$(dirname "$SCRIPT_DIR")"
LOAD_FOLDERS="$MOD_ROOT/LoadFolders.xml"

if [ ! -f "$LOAD_FOLDERS" ]; then
    echo "Error: LoadFolders.xml not found at $LOAD_FOLDERS" >&2
    exit 1
fi

# Extract version tags like <v1.6>, sort numerically, return highest
# Uses grep to find <vX.Y> patterns, sed to extract just the version number
LATEST_VERSION=$(grep -oP '<v\K[0-9]+\.[0-9]+' "$LOAD_FOLDERS" | sort -t. -k1,1n -k2,2n | tail -1)

if [ -z "$LATEST_VERSION" ]; then
    echo "Error: No version tags found in LoadFolders.xml" >&2
    exit 1
fi

echo "$LATEST_VERSION"
