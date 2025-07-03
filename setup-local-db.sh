#!/bin/bash

echo "Setting up local Jarvis database with seed data..."

# Run the test database setup script against the local database
PGPASSWORD=postgres psql -h localhost -p 5432 -U supabase_admin -d jarvis_test -f core.jarvis.tests/Scripts/setup-test-database.sql

# Create test user account
echo "Creating test user account..."
PGPASSWORD=postgres psql -h localhost -p 5432 -U supabase_admin -d jarvis_test -f create-test-account-fixed.sql

echo "Local database setup complete!"
echo "Default user created:"
echo "  Email: test@example.com"
echo "  Password: test123"