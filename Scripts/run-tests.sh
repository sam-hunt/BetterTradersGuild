#!/bin/bash

# Builds and runs tests for the specified game version.
#
# Usage:
#   ./run-tests.sh        # Uses latest supported version (from LoadFolders.xml)
#   ./run-tests.sh 1.6    # Explicitly target version 1.6

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_ROOT="$(dirname "$SCRIPT_DIR")"
VSTEST="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"

# Get version from CLI argument or fall back to latest
VERSION="${1:-$("$SCRIPT_DIR/get-latest-version.sh")}"
if [ $? -ne 0 ] && [ -z "$1" ]; then
    echo -e "${RED}✗ Failed to determine version${NC}"
    exit 1
fi

# Version-specific paths (Windows-style for VSTest)
TEST_DLL="C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\Mods\\BetterTradersGuild\\Tests\\${VERSION}\\bin\\Debug\\net472\\BetterTradersGuild.Tests.dll"

echo -e "${BLUE}=== Better Traders Guild - Test Runner ===${NC}"
echo -e "${BLUE}    Target Version: ${VERSION}${NC}"
echo ""

# Step 1: Build solution (builds both main project and tests)
echo -e "${YELLOW}[1/2] Building solution...${NC}"
cd "$MOD_ROOT"
dotnet build --verbosity quiet

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Build failed!${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Build succeeded${NC}"
echo ""

# Step 2: Run tests using VSTest (faster on WSL than dotnet test)
echo -e "${YELLOW}[2/2] Running tests...${NC}"
echo ""

if [ -f "$VSTEST" ]; then
    "$VSTEST" "$TEST_DLL" /Tests:PlacementCalculatorTests /logger:"console;verbosity=normal"
    TEST_EXIT_CODE=$?
else
    echo -e "${RED}✗ VSTest.Console.exe not found at expected location.${NC}"
    echo -e "${RED}   Please ensure Visual Studio 2022 is installed.${NC}"
    exit 1
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
