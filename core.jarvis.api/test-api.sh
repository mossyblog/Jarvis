#!/bin/bash
# Bash script to test the Jarvis API endpoints

BASE_URL="http://localhost:7071/api"

echo -e "\033[32mTesting Jarvis API Endpoints\033[0m"
echo -e "\033[32m============================\033[0m"

# Generate UUIDs
ID1=$(uuidgen)
ID2=$(uuidgen)
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Test 1: Authentication
echo -e "\n\033[33mTest 1: Authentication (POST /api/security/auth)\033[0m"
AUTH_BODY=$(cat <<EOF
{
  "id": "$ID1",
  "ownerEntityId": "$ID2",
  "updatedAt": "$TIMESTAMP",
  "email": "test@example.com",
  "password": "TestPassword123!",
  "clientId": "bash-test"
}
EOF
)

AUTH_RESPONSE=$(curl -s -X POST "$BASE_URL/security/auth" \
  -H "Content-Type: application/json" \
  -d "$AUTH_BODY")

if [ $? -eq 0 ]; then
  echo -e "\033[32m✓ Auth Success:\033[0m"
  echo "$AUTH_RESPONSE" | jq .
  
  # Extract sessionId and refreshToken
  SESSION_ID=$(echo "$AUTH_RESPONSE" | jq -r '.sessionId // empty')
  REFRESH_TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.refreshToken // empty')
else
  echo -e "\033[31m✗ Auth Failed\033[0m"
fi

# Test 2: Token Validation
if [ ! -z "$SESSION_ID" ]; then
  echo -e "\n\033[33mTest 2: Token Validation (GET /api/security/validate)\033[0m"
  VALIDATE_RESPONSE=$(curl -s -X GET "$BASE_URL/security/validate" \
    -H "X-Token-Id: $SESSION_ID")
  
  if [ $? -eq 0 ]; then
    echo -e "\033[32m✓ Validation Success:\033[0m"
    echo "$VALIDATE_RESPONSE" | jq .
  else
    echo -e "\033[31m✗ Validation Failed\033[0m"
  fi
fi

# Test 3: Token Refresh
if [ ! -z "$REFRESH_TOKEN" ]; then
  echo -e "\n\033[33mTest 3: Token Refresh (POST /api/security/refresh)\033[0m"
  REFRESH_BODY=$(cat <<EOF
{
  "id": "$(uuidgen)",
  "ownerEntityId": "$(uuidgen)",
  "updatedAt": "$TIMESTAMP",
  "refreshToken": "$REFRESH_TOKEN",
  "clientId": "bash-test"
}
EOF
)

  REFRESH_RESPONSE=$(curl -s -X POST "$BASE_URL/security/refresh" \
    -H "Content-Type: application/json" \
    -d "$REFRESH_BODY")
  
  if [ $? -eq 0 ]; then
    echo -e "\033[32m✓ Refresh Success:\033[0m"
    echo "$REFRESH_RESPONSE" | jq .
  else
    echo -e "\033[31m✗ Refresh Failed\033[0m"
  fi
fi

# Test 4: Deauthentication
if [ ! -z "$SESSION_ID" ]; then
  echo -e "\n\033[33mTest 4: Deauthentication (POST /api/security/deauth)\033[0m"
  DEAUTH_RESPONSE=$(curl -s -X POST "$BASE_URL/security/deauth" \
    -H "Content-Type: application/json" \
    -d "\"$SESSION_ID\"")
  
  if [ $? -eq 0 ]; then
    echo -e "\033[32m✓ Deauth Success:\033[0m"
    echo "Confirmation ID: $DEAUTH_RESPONSE"
  else
    echo -e "\033[31m✗ Deauth Failed\033[0m"
  fi
fi

# Test 5: Invalid Requests
echo -e "\n\033[33mTest 5: Invalid Request Tests\033[0m"

# Invalid auth (missing email)
echo -e "\033[36m- Testing invalid auth (missing email)...\033[0m"
INVALID_AUTH_BODY=$(cat <<EOF
{
  "id": "$(uuidgen)",
  "ownerEntityId": "$(uuidgen)",
  "updatedAt": "$TIMESTAMP",
  "password": "TestPassword123!"
}
EOF
)

INVALID_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$BASE_URL/security/auth" \
  -H "Content-Type: application/json" \
  -d "$INVALID_AUTH_BODY")

HTTP_CODE=$(echo "$INVALID_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
if [ "$HTTP_CODE" = "400" ]; then
  echo -e "\033[32m✓ Correctly rejected with HTTP 400\033[0m"
else
  echo -e "\033[31m✗ Should have failed with HTTP 400\033[0m"
fi

# Invalid GUID
echo -e "\033[36m- Testing invalid GUID format...\033[0m"
INVALID_GUID_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$BASE_URL/security/deauth" \
  -H "Content-Type: application/json" \
  -d '"not-a-guid"')

HTTP_CODE=$(echo "$INVALID_GUID_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
if [ "$HTTP_CODE" = "400" ]; then
  echo -e "\033[32m✓ Correctly rejected with HTTP 400\033[0m"
else
  echo -e "\033[31m✗ Should have failed with HTTP 400\033[0m"
fi

echo -e "\n\033[32mAll tests completed!\033[0m"