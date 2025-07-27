#!/bin/bash

# Script to run Row Level Security tests after the row_security fix
# This script ensures the RLS functionality works correctly after changing
# row_security to rowsecurity in PgClientFactory.cs

echo "=== Row Level Security Test Runner ==="
echo "Testing the fix for PostgreSQL column name issue"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: dotnet CLI is not installed${NC}"
    echo "Please install .NET SDK to run tests"
    exit 1
fi

echo -e "${YELLOW}Running Row Level Security Tests...${NC}"
echo ""

# Run the specific RLS tests
echo "1. Running RowLevelSecurityTests..."
dotnet test core.jarvis.data.tests/core.jarvis.data.tests.csproj \
    --filter "FullyQualifiedName~RowLevelSecurity" \
    --logger "console;verbosity=normal" \
    --no-build

RLS_TEST_RESULT=$?

echo ""
echo "2. Running PgClientFactory related tests..."
dotnet test core.jarvis.data.tests/core.jarvis.data.tests.csproj \
    --filter "FullyQualifiedName~PgClient" \
    --logger "console;verbosity=normal" \
    --no-build

PGCLIENT_TEST_RESULT=$?

echo ""
echo "3. Running integration tests that use RLS..."
dotnet test core.jarvis.tests/core.jarvis.tests.csproj \
    --filter "Category=Integration" \
    --logger "console;verbosity=normal" \
    --no-build

INTEGRATION_TEST_RESULT=$?

echo ""
echo "=== Test Summary ==="

if [ $RLS_TEST_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ Row Level Security Tests: PASSED${NC}"
else
    echo -e "${RED}✗ Row Level Security Tests: FAILED${NC}"
fi

if [ $PGCLIENT_TEST_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ PgClient Tests: PASSED${NC}"
else
    echo -e "${RED}✗ PgClient Tests: FAILED${NC}"
fi

if [ $INTEGRATION_TEST_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ Integration Tests: PASSED${NC}"
else
    echo -e "${RED}✗ Integration Tests: FAILED${NC}"
fi

echo ""
echo "=== Verification of Fix ==="
echo "Checking for any remaining instances of 'row_security'..."

if grep -r "row_security" . --include="*.cs" --exclude-dir=".git" --exclude-dir="bin" --exclude-dir="obj" 2>/dev/null; then
    echo -e "${RED}Warning: Found instances of 'row_security' in the codebase${NC}"
else
    echo -e "${GREEN}✓ No instances of 'row_security' found${NC}"
fi

echo ""
echo "Checking correct usage of 'rowsecurity'..."
if grep -r "rowsecurity" . --include="*.cs" --exclude-dir=".git" --exclude-dir="bin" --exclude-dir="obj" 2>/dev/null; then
    echo -e "${GREEN}✓ Found correct usage of 'rowsecurity'${NC}"
else
    echo -e "${YELLOW}Note: No instances of 'rowsecurity' found - verify if this is expected${NC}"
fi

# Calculate overall result
OVERALL_RESULT=0
if [ $RLS_TEST_RESULT -ne 0 ] || [ $PGCLIENT_TEST_RESULT -ne 0 ] || [ $INTEGRATION_TEST_RESULT -ne 0 ]; then
    OVERALL_RESULT=1
fi

echo ""
if [ $OVERALL_RESULT -eq 0 ]; then
    echo -e "${GREEN}=== All tests passed! The RLS fix is working correctly. ===${NC}"
else
    echo -e "${RED}=== Some tests failed. Please review the output above. ===${NC}"
fi

exit $OVERALL_RESULT