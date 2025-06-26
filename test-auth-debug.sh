#!/bin/bash

echo "Testing authentication with debug output..."

# Test with correct format
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '{"Email":"test@example.com","Password":"test123"}' \
  -v 2>&1 | tee auth-debug.log

echo -e "\n\nResponse saved to auth-debug.log"