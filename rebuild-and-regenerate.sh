#!/bin/bash

# Rebuild both projects and regenerate diagrams

MOD_ROOT="/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"
MSBUILD="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"

cd "$MOD_ROOT"

echo "=== Building Main Project ==="
cd Source
"$MSBUILD" BetterTradersGuild.csproj /p:Configuration=Debug /v:minimal
if [ $? -ne 0 ]; then
    echo "✗ Main project build failed"
    exit 1
fi
echo ""

echo "=== Building Test Project ==="
cd ../Tests
"$MSBUILD" BetterTradersGuild.Tests.csproj /p:Configuration=Debug /v:minimal
if [ $? -ne 0 ]; then
    echo "✗ Test project build failed"
    exit 1
fi
echo ""

echo "=== Regenerating Diagrams ==="
cd ..
bash regenerate-diagrams.sh
