#!/bin/bash

# Generates placement diagrams for test comments.
#
# Usage:
#   ./generate-diagrams.sh        # Uses latest supported version (from LoadFolders.xml)
#   ./generate-diagrams.sh 1.6    # Explicitly target version 1.6

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_ROOT="$(dirname "$SCRIPT_DIR")"
CSC="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/Roslyn/csc.exe"

# Get version from CLI argument or fall back to latest
VERSION="${1:-$("$SCRIPT_DIR/get-latest-version.sh")}"
if [ $? -ne 0 ] && [ -z "$1" ]; then
    echo "Error: Failed to determine version"
    exit 1
fi

echo "Target Version: ${VERSION}"
echo ""

cd "$MOD_ROOT"

# Compile the diagram generator as a standalone executable
"$CSC" /target:exe \
    /out:"Tests/${VERSION}/bin/DiagramGen.exe" \
    /reference:"${VERSION}/Assemblies/BetterTradersGuild.dll" \
    "Tests/${VERSION}/Helpers/DiagramGenerator.cs" \
    "Tests/${VERSION}/generate-diagrams.cs" \
    2>&1 | grep -v "^Microsoft"

if [ $? -eq 0 ]; then
    echo "Running diagram generator..."
    echo ""
    "Tests/${VERSION}/bin/DiagramGen.exe"
else
    echo "Compilation failed"
    exit 1
fi
