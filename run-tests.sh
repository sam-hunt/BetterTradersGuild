#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Paths
MOD_ROOT="/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"
SOURCE_DIR="$MOD_ROOT/Source"
TESTS_DIR="$MOD_ROOT/Tests"
MSBUILD="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"

echo -e "${BLUE}=== Better Traders Guild - Test Runner ===${NC}"
echo ""

# Step 1: Build the mod DLL
echo -e "${YELLOW}[1/3] Building mod DLL...${NC}"
cd "$SOURCE_DIR"
"$MSBUILD" BetterTradersGuild.csproj /p:Configuration=Debug /v:quiet /nologo

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Mod build failed!${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Mod build succeeded${NC}"
echo ""

# Step 2: Build the test project
echo -e "${YELLOW}[2/3] Building test project...${NC}"
cd "$TESTS_DIR"
"$MSBUILD" BetterTradersGuild.Tests.csproj /p:Configuration=Debug /v:quiet /nologo

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Test build failed!${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Test build succeeded${NC}"
echo ""

# Step 3: Run tests
echo -e "${YELLOW}[3/3] Running tests...${NC}"
echo ""

# Try dotnet CLI first, fall back to VSTest.Console.exe
if command -v dotnet &> /dev/null; then
    dotnet test --no-build --verbosity normal --filter "FullyQualifiedName~PlacementCalculator"
    TEST_EXIT_CODE=$?
else
    # Use VSTest.Console.exe from Visual Studio
    VSTEST="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"
    if [ -f "$VSTEST" ]; then
        TEST_DLL="C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\Mods\\BetterTradersGuild\\Tests\\bin\\Debug\\net472\\BetterTradersGuild.Tests.dll"
        "$VSTEST" "$TEST_DLL" /Tests:PlacementCalculatorTests /logger:"console;verbosity=normal"
        TEST_EXIT_CODE=$?
    else
        echo -e "${RED}✗ Test runner not found. Please install .NET SDK or Visual Studio.${NC}"
        exit 1
    fi
fi

echo ""
if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✓ All tests passed!${NC}"
    echo -e "${GREEN}========================================${NC}"
else
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}✗ Some tests failed${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi
