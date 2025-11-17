#!/bin/bash

# Script to regenerate all test diagrams with corrected in-game behavior

MOD_ROOT="/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"
CSC="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/Roslyn/csc.exe"

cd "$MOD_ROOT"

echo "Compiling diagram regeneration tool..."

# Compile the regeneration tool
"$CSC" /target:exe \
    /out:"Tests/Tools/RegenerateDiagrams.exe" \
    /reference:"Assemblies/BetterTradersGuild.dll" \
    /reference:"Tests/bin/Debug/net472/BetterTradersGuild.Tests.dll" \
    Tests/Tools/RegenerateDiagrams.cs \
    2>&1 | grep -v "^Microsoft"

if [ $? -eq 0 ]; then
    echo ""
    echo "Running diagram regeneration..."
    echo ""

    # Copy dependencies to the same directory
    cp "Assemblies/BetterTradersGuild.dll" "Tests/Tools/"
    cp "Tests/bin/Debug/net472/BetterTradersGuild.Tests.dll" "Tests/Tools/"

    # Run from the Tools directory with Windows-style path
    cd "Tests/Tools"
    ./RegenerateDiagrams.exe "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\Mods\\BetterTradersGuild\\Tests\\RoomContents\\PlacementCalculatorTests.cs"

    echo ""
    echo "✓ Diagrams regenerated! Check PlacementCalculatorTests.cs for updates."
else
    echo "✗ Compilation failed"
    exit 1
fi
