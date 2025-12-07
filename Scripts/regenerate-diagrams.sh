#!/bin/bash

# Rebuilds both projects and regenerates test diagrams.
#
# Usage:
#   ./regenerate-diagrams.sh        # Uses latest supported version (from LoadFolders.xml)
#   ./regenerate-diagrams.sh 1.6    # Explicitly target version 1.6

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_ROOT="$(dirname "$SCRIPT_DIR")"
MSBUILD="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"
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

echo "=== Building Main Project ==="
cd "Source/${VERSION}"
"$MSBUILD" BetterTradersGuild.csproj /p:Configuration=Debug /v:minimal
if [ $? -ne 0 ]; then
    echo "Main project build failed"
    exit 1
fi
echo ""

echo "=== Building Test Project ==="
cd "../../Tests/${VERSION}"
"$MSBUILD" BetterTradersGuild.Tests.csproj /p:Configuration=Debug /v:minimal
if [ $? -ne 0 ]; then
    echo "Test project build failed"
    exit 1
fi
echo ""

echo "=== Compiling Diagram Tool ==="
cd "$MOD_ROOT"

"$CSC" /target:exe \
    /out:"Tests/${VERSION}/Tools/RegenerateDiagrams.exe" \
    /reference:"${VERSION}/Assemblies/BetterTradersGuild.dll" \
    /reference:"Tests/${VERSION}/bin/Debug/net472/BetterTradersGuild.Tests.dll" \
    "Tests/${VERSION}/Tools/RegenerateDiagrams.cs" \
    2>&1 | grep -v "^Microsoft"

if [ $? -ne 0 ]; then
    echo "Diagram tool compilation failed"
    exit 1
fi
echo ""

echo "=== Regenerating Diagrams ==="

# Copy dependencies to the same directory
cp "${VERSION}/Assemblies/BetterTradersGuild.dll" "Tests/${VERSION}/Tools/"
cp "Tests/${VERSION}/bin/Debug/net472/BetterTradersGuild.Tests.dll" "Tests/${VERSION}/Tools/"

# Run from the Tools directory with Windows-style path
cd "Tests/${VERSION}/Tools"
./RegenerateDiagrams.exe "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\Mods\\BetterTradersGuild\\Tests\\${VERSION}\\RoomContents\\PlacementCalculatorTests.cs"

echo ""
echo "Diagrams regenerated! Check PlacementCalculatorTests.cs for updates."
