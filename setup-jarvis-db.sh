#!/bin/bash

# setup-jarvis-db.sh
# Sets up the Jarvis framework database with all required tables

set -e  # Exit on error

# Default values
SERVER="${JARVIS_DB_SERVER:-localhost}"
PORT="${JARVIS_DB_PORT:-5432}"
USERNAME="${JARVIS_DB_USERNAME:-supabase_admin}"
DATABASE="${JARVIS_DB_NAME:-jarvis_test}"
CREATE_TEST_USER=false

# Color codes for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to display usage
usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Sets up the Jarvis framework database with all required tables.

OPTIONS:
    -h, --host          PostgreSQL server hostname (default: localhost)
    -p, --port          PostgreSQL server port (default: 5432)
    -u, --username      PostgreSQL username (default: supabase_admin)
    -d, --database      Target database name (default: jarvis)
    -t, --test-user     Create a test user for development
    --help              Display this help message

ENVIRONMENT VARIABLES:
    PGPASSWORD          PostgreSQL password (recommended method)
    JARVIS_DB_SERVER    Default server (overridden by -h)
    JARVIS_DB_PORT      Default port (overridden by -p)
    JARVIS_DB_USERNAME  Default username (overridden by -u)
    JARVIS_DB_NAME      Default database name (overridden by -d)

EXAMPLES:
    $0 -h localhost -u postgres
    PGPASSWORD=mypassword $0 -h db.example.com -d jarvis_prod
    $0 --test-user

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--host)
            SERVER="$2"
            shift 2
            ;;
        -p|--port)
            PORT="$2"
            shift 2
            ;;
        -u|--username)
            USERNAME="$2"
            shift 2
            ;;
        -d|--database)
            DATABASE="$2"
            shift 2
            ;;
        -t|--test-user)
            CREATE_TEST_USER=true
            shift
            ;;
        --help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Check if psql is available
if ! command -v psql &> /dev/null; then
    echo -e "${RED}Error: PostgreSQL client (psql) is not installed or not in PATH${NC}"
    echo "Please install PostgreSQL client tools:"
    echo "  Ubuntu/Debian: sudo apt-get install postgresql-client"
    echo "  macOS: brew install postgresql"
    echo "  RHEL/CentOS: sudo yum install postgresql"
    exit 1
fi

# Check if password is set, default to 'postgres' for supabase_admin
if [ -z "$PGPASSWORD" ]; then
    if [ "$USERNAME" = "supabase_admin" ]; then
        PGPASSWORD="postgres"
        export PGPASSWORD
        echo -e "${CYAN}Using default password for supabase_admin${NC}"
    else
        echo -n "Enter PostgreSQL password for user '$USERNAME': "
        read -s PGPASSWORD
        echo
        export PGPASSWORD
    fi
fi

echo -e "${GREEN}Setting up Jarvis database...${NC}"

# Function to execute SQL
execute_sql() {
    local query="$1"
    local target_db="${2:-postgres}"
    local suppress_error="${3:-false}"
    
    if [ "$suppress_error" = "true" ]; then
        psql -h "$SERVER" -p "$PORT" -U "$USERNAME" -d "$target_db" -c "$query" 2>/dev/null
    else
        psql -h "$SERVER" -p "$PORT" -U "$USERNAME" -d "$target_db" -c "$query"
    fi
}

# Check if database exists
echo -e "${YELLOW}Checking if database '$DATABASE' exists...${NC}"
if execute_sql "SELECT 1 FROM pg_database WHERE datname = '$DATABASE'" "postgres" true | grep -q 1; then
    echo -e "${GREEN}Database '$DATABASE' already exists${NC}"
else
    echo -e "${YELLOW}Creating database '$DATABASE'...${NC}"
    if execute_sql "CREATE DATABASE $DATABASE" "postgres"; then
        echo -e "${GREEN}Database '$DATABASE' created successfully${NC}"
    else
        echo -e "${RED}Failed to create database '$DATABASE'${NC}"
        exit 1
    fi
fi

# SQL script for creating tables
read -r -d '' SETUP_SQL << 'EOF' || true
-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create account_component table (follows ECS naming)
CREATE TABLE IF NOT EXISTS account_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    password VARCHAR(255) NOT NULL DEFAULT '',
    two_factor_code VARCHAR(10),
    auth_method VARCHAR(50) NOT NULL DEFAULT 'password',
    client_id VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address VARCHAR(45),
    user_agent TEXT
);

-- Create indexes for account_component table
CREATE INDEX IF NOT EXISTS idx_account_owner_entity_id ON account_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_account_email ON account_component(email);

-- Enable Row Level Security on account_component table
ALTER TABLE account_component ENABLE ROW LEVEL SECURITY;

-- Create a simple RLS policy for account_component table
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = 'public' AND tablename = 'account_component' AND policyname = 'Enable all for development') THEN
        CREATE POLICY "Enable all for development" ON account_component
            FOR ALL USING (true) WITH CHECK (true);
    END IF;
END $$;

-- Create auth_token_component table (follows ECS naming)
CREATE TABLE IF NOT EXISTS auth_token_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    access_token TEXT NOT NULL DEFAULT '',
    refresh_token TEXT NOT NULL DEFAULT '',
    refresh_token_hash TEXT NOT NULL DEFAULT '',
    expires_at TIMESTAMPTZ NOT NULL,
    refresh_expires_at TIMESTAMPTZ NOT NULL,
    token_type TEXT NOT NULL DEFAULT 'Bearer',
    session_id UUID NOT NULL,
    scope TEXT NOT NULL DEFAULT '',
    client_id TEXT,
    issued_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address TEXT,
    user_agent TEXT,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMPTZ,
    version INTEGER,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for auth_token_component
CREATE INDEX IF NOT EXISTS idx_auth_token_owner_entity_id ON auth_token_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_session_id ON auth_token_component(session_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_refresh_expires_at ON auth_token_component(refresh_expires_at) WHERE is_revoked = FALSE;

-- Create audit_event table for comprehensive audit logging
CREATE TABLE IF NOT EXISTS audit_event (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    user_id VARCHAR(255),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    old_value TEXT,
    new_value TEXT,
    metadata JSONB,
    transaction_id VARCHAR(100),
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for audit_event
CREATE INDEX IF NOT EXISTS idx_audit_event_owner_entity_id ON audit_event(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_event_entity_type ON audit_event(entity_type);
CREATE INDEX IF NOT EXISTS idx_audit_event_event_type ON audit_event(event_type);
CREATE INDEX IF NOT EXISTS idx_audit_event_timestamp ON audit_event(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_event_user_id ON audit_event(user_id);

-- Create component_snapshots table for versioning support
CREATE TABLE IF NOT EXISTS component_snapshots (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id UUID NOT NULL,
    component_type VARCHAR(255) NOT NULL,
    component_id UUID NOT NULL UNIQUE,
    snapshots JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for component_snapshots
CREATE INDEX IF NOT EXISTS idx_component_snapshots_entity_id ON component_snapshots(entity_id);
CREATE INDEX IF NOT EXISTS idx_component_snapshots_component_type ON component_snapshots(component_type);

-- Create entity_relationship table for parent-child relationships
CREATE TABLE IF NOT EXISTS entity_relationship (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    parent_id UUID,
    children_ids UUID[] DEFAULT '{}',
    parent_type TEXT,
    child_types JSONB DEFAULT '{}',
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for entity_relationship
CREATE INDEX IF NOT EXISTS idx_entity_relationship_owner_entity_id ON entity_relationship(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_entity_relationship_parent_id ON entity_relationship(parent_id);

-- Create update timestamp trigger function
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply update timestamp triggers to all tables
DO $$
DECLARE
    t text;
BEGIN
    FOR t IN 
        SELECT table_name 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND column_name = 'updated_at'
    LOOP
        EXECUTE format('
            CREATE TRIGGER update_%I_updated_at 
            BEFORE UPDATE ON %I 
            FOR EACH ROW 
            EXECUTE FUNCTION update_updated_at()', t, t);
    END LOOP;
END $$;
EOF

# Execute the setup SQL
echo -e "${YELLOW}Creating tables in database '$DATABASE'...${NC}"
if echo "$SETUP_SQL" | psql -h "$SERVER" -p "$PORT" -U "$USERNAME" -d "$DATABASE"; then
    echo -e "${GREEN}All tables created successfully${NC}"
else
    echo -e "${RED}Failed to create tables${NC}"
    exit 1
fi

# Grant permissions
echo -e "${YELLOW}Granting permissions...${NC}"
GRANT_SQL="GRANT ALL ON ALL TABLES IN SCHEMA public TO $USERNAME;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO $USERNAME;"

if execute_sql "$GRANT_SQL" "$DATABASE"; then
    echo -e "${GREEN}Permissions granted successfully${NC}"
else
    echo -e "${YELLOW}Warning: Failed to grant some permissions (this may be normal)${NC}"
fi

# Create test user if requested
if [ "$CREATE_TEST_USER" = true ]; then
    echo -e "${YELLOW}Creating test user...${NC}"
    
    TEST_USER_SQL="INSERT INTO account_component (owner_entity_id, email, password_hash) 
VALUES (gen_random_uuid(), 'test@example.com', '\$2a\$12\$QnOKnn.PumrtVscPkO3C.ONHR/5NANzEbqMoLQOyUFhQMhynyVoe.')
ON CONFLICT (email) DO NOTHING;"
    
    if execute_sql "$TEST_USER_SQL" "$DATABASE"; then
        echo -e "${GREEN}Test user created (test@example.com / test123)${NC}"
    else
        echo -e "${YELLOW}Warning: Failed to create test user (may already exist)${NC}"
    fi
fi

# Verify tables
echo -e "\n${YELLOW}Verifying created tables...${NC}"
VERIFY_SQL="SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_type = 'BASE TABLE'
ORDER BY table_name;"

echo -e "\n${GREEN}Created tables:${NC}"
psql -h "$SERVER" -p "$PORT" -U "$USERNAME" -d "$DATABASE" -t -c "$VERIFY_SQL" | while read -r table; do
    if [ -n "$table" ]; then
        echo -e "  ${CYAN}- $(echo $table | xargs)${NC}"
    fi
done

# Display connection string
echo -e "\n${GREEN}Jarvis database setup completed successfully!${NC}"
echo -e "Connection string: ${YELLOW}Host=$SERVER;Port=$PORT;Database=$DATABASE;Username=$USERNAME;Password=<your-password>${NC}"

# Clean up
unset PGPASSWORD