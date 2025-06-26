#!/bin/bash

echo "Setting up local Jarvis database with seed data..."

# Run the test database setup script against the local database
psql -h localhost -p 5432 -U postgres -d jarvis -f core.jarvis.tests/Scripts/setup-test-database.sql

echo "Local database setup complete!"
echo "Default user created:"
echo "  Email: test@example.com"
echo "  Password: test123"