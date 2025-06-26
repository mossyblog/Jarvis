#!/bin/bash

echo "Testing authentication endpoint..."
echo

# Test with properly formatted JSON
echo "1. Testing with valid JSON format:"
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '{"Email": "test@example.com", "Password": "TestPassword123!"}' \
  -v

echo
echo "2. Testing with lowercase properties:"
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "TestPassword123!"}' \
  -v

echo
echo "3. Testing with empty body (should return 400):"
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '' \
  -v

echo
echo "4. Testing with missing password (should return 400):"
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com"}' \
  -v