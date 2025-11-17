#!/bin/bash

# Quick script to generate placement diagrams for test comments

MOD_ROOT="/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"
CSC="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/Roslyn/csc.exe"

cd "$MOD_ROOT"

# Compile the diagram generator as a standalone executable
"$CSC" /target:exe \
    /out:"Tests/bin/DiagramGen.exe" \
    /reference:"Assemblies/BetterTradersGuild.dll" \
    Tests/Helpers/DiagramGenerator.cs \
    Tests/generate-diagrams.cs \
    2>&1 | grep -v "^Microsoft"

if [ $? -eq 0 ]; then
    echo "Running diagram generator..."
    echo ""
    Tests/bin/DiagramGen.exe
else
    echo "Compilation failed"
    exit 1
fi
