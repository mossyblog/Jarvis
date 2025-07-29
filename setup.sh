#!/bin/bash

# Jarvis Database Setup Script
# This script sets up the PostgreSQL database exactly as the ECS framework expects
# 
# Usage: ./setup.sh
# 
# This script is idempotent - it can be run multiple times safely.
# It will create tables, default users, and navigation items.
#
# Default test user created:
# - Email: test@example.com
# - Password: TestPassword123!

set -e  # Exit on any error

DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-jarvis_test}
DB_USER=${DB_USER:-postgres}
DB_PASSWORD=${DB_PASSWORD:-postgres}

echo "========================================="
echo "Setting up Jarvis database..."
echo "Database: $DB_NAME"
echo "Host: $DB_HOST:$DB_PORT"
echo "User: $DB_USER"
echo "========================================="

# Function to execute SQL commands
execute_sql() {
    docker exec jarvis-postgres psql -U $DB_USER -c "$1"
}

# Function to execute SQL commands in a specific database
execute_sql_db() {
    docker exec jarvis-postgres psql -U $DB_USER -d $1 -c "$2"
}

# Check if Docker container is running
if ! docker ps | grep -q jarvis-postgres; then
    echo "Error: jarvis-postgres container is not running"
    echo "Please run: docker-compose up -d"
    exit 1
fi

# Create database if it doesn't exist
echo "Creating database $DB_NAME..."
execute_sql "SELECT 'CREATE DATABASE $DB_NAME' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$DB_NAME')\gexec" || true

# Enable required extensions
echo "Enabling PostgreSQL extensions..."
execute_sql_db $DB_NAME "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";"
execute_sql_db $DB_NAME "CREATE EXTENSION IF NOT EXISTS pg_graphql;"
execute_sql_db $DB_NAME "CREATE EXTENSION IF NOT EXISTS pgcrypto;"
execute_sql_db $DB_NAME "CREATE EXTENSION IF NOT EXISTS pgjwt;"

# Create account_component table (based on Account.cs model)
echo "Creating account_component table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS account_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    email TEXT NOT NULL DEFAULT '',
    password_hash TEXT NOT NULL DEFAULT '',
    password TEXT NOT NULL DEFAULT '',
    two_factor_code TEXT,
    auth_method TEXT NOT NULL DEFAULT 'password',
    client_id TEXT,
    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address TEXT,
    user_agent TEXT
);

CREATE INDEX IF NOT EXISTS idx_account_component_owner_entity_id ON account_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_account_component_last_updated ON account_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_account_component_email ON account_component(email);
"

# Create security_profile_component table
echo "Creating security_profile_component table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS security_profile_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    name TEXT NOT NULL DEFAULT '',
    role_ids UUID[] DEFAULT '{}',
    permission_ids UUID[] DEFAULT '{}'
);

CREATE INDEX IF NOT EXISTS idx_security_profile_component_owner_entity_id ON security_profile_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_security_profile_component_last_updated ON security_profile_component(last_updated);
"

# Create navigation_item_component table
echo "Creating navigation_item_component table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS navigation_item_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    menu_id TEXT NOT NULL DEFAULT '',
    label TEXT NOT NULL DEFAULT '',
    icon TEXT NOT NULL DEFAULT '',
    href TEXT NOT NULL DEFAULT '',
    sort_order INTEGER NOT NULL DEFAULT 0,
    required_permission_id UUID,
    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    badge_config JSONB
);

CREATE INDEX IF NOT EXISTS idx_navigation_item_component_owner_entity_id ON navigation_item_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_last_updated ON navigation_item_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_menu_id ON navigation_item_component(menu_id);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_sort_order ON navigation_item_component(sort_order);
"

# Create role_component table
echo "Creating role_component table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS role_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    name TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    permission_ids UUID[] DEFAULT '{}',
    is_system BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_role_component_owner_entity_id ON role_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_role_component_last_updated ON role_component(last_updated);
"

# Create permission_component table
echo "Creating permission_component table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS permission_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    name TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    is_system BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_permission_component_owner_entity_id ON permission_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_permission_component_last_updated ON permission_component(last_updated);
"

# Create auth_token_component table
echo "Creating auth_token_component table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS auth_token_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    access_token TEXT NOT NULL DEFAULT '',
    refresh_token TEXT NOT NULL DEFAULT '',
    access_token_expiry TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    refresh_token_expiry TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address TEXT,
    user_agent TEXT,
    client_id TEXT
);

CREATE INDEX IF NOT EXISTS idx_auth_token_component_owner_entity_id ON auth_token_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_component_last_updated ON auth_token_component(last_updated);
"

# Create entity_relationship table (for parent-child relationships)
echo "Creating entity_relationship table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS entity_relationship (
    parent_entity_id UUID NOT NULL,
    child_entity_id UUID NOT NULL,
    parent_type TEXT NOT NULL DEFAULT '',
    child_type TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (parent_entity_id, child_entity_id)
);

CREATE INDEX IF NOT EXISTS idx_entity_relationship_parent ON entity_relationship(parent_entity_id);
CREATE INDEX IF NOT EXISTS idx_entity_relationship_child ON entity_relationship(child_entity_id);
"

# Create component_snapshots table (for snapshot functionality)
echo "Creating component_snapshots table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS component_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id UUID NOT NULL,
    component_type TEXT NOT NULL DEFAULT '',
    component_data JSONB NOT NULL DEFAULT '{}',
    version INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by TEXT NOT NULL DEFAULT '',
    snapshot_reason TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS idx_component_snapshots_entity_id ON component_snapshots(entity_id);
CREATE INDEX IF NOT EXISTS idx_component_snapshots_created_at ON component_snapshots(created_at);
"

# Create audit_event table
echo "Creating audit_event table..."
execute_sql_db $DB_NAME "
CREATE TABLE IF NOT EXISTS audit_event (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    event_type TEXT NOT NULL DEFAULT '',
    entity_id UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    component_type TEXT NOT NULL DEFAULT '',
    user_id TEXT NOT NULL DEFAULT '',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    operation TEXT NOT NULL DEFAULT '',
    old_value JSONB,
    new_value JSONB,
    metadata JSONB
);

CREATE INDEX IF NOT EXISTS idx_audit_event_owner_entity_id ON audit_event(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_event_last_updated ON audit_event(last_updated);
CREATE INDEX IF NOT EXISTS idx_audit_event_entity_id ON audit_event(entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_event_timestamp ON audit_event(timestamp);
"

# Create test user if running in test mode
if [ "$DB_NAME" = "jarvis_test" ]; then
    echo "Creating test user..."
    execute_sql_db $DB_NAME "
    INSERT INTO account_component (
        id, 
        owner_entity_id, 
        email, 
        password_hash, 
        auth_method, 
        is_active,
        created_at,
        last_updated
    ) VALUES (
        'f47ac10b-58cc-4372-a567-0e02b2c3d479',
        'f47ac10b-58cc-4372-a567-0e02b2c3d480',
        'test@example.com',
        '\$2a\$10\$G7V86I50b3Z0iexpJesSWuo3AlFo0tWY.GqvNbRodfdahpqv6qh0i',
        'password',
        true,
        NOW(),
        NOW()
    ) ON CONFLICT (id) DO UPDATE SET
        email = EXCLUDED.email,
        password_hash = EXCLUDED.password_hash,
        is_active = EXCLUDED.is_active,
        last_updated = NOW();
    "
    
    echo "Creating additional test accounts..."
    execute_sql_db $DB_NAME "
    INSERT INTO account_component (
        id, 
        owner_entity_id, 
        email, 
        password_hash, 
        auth_method, 
        is_active,
        created_at,
        last_updated
    ) VALUES 
    (
        'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
        'a1b2c3d4-e5f6-7890-abcd-ef1234567891',
        'admin@jarvis.com',
        '\$2a\$10\$G7V86I50b3Z0iexpJesSWuo3AlFo0tWY.GqvNbRodfdahpqv6qh0i',
        'password',
        true,
        NOW(),
        NOW()
    ),
    (
        'b2c3d4e5-f6a7-8901-bcde-f12345678901',
        'b2c3d4e5-f6a7-8901-bcde-f12345678902',
        'user@jarvis.com',
        '\$2a\$10\$G7V86I50b3Z0iexpJesSWuo3AlFo0tWY.GqvNbRodfdahpqv6qh0i',
        'password',
        true,
        NOW(),
        NOW()
    ),
    (
        'c3d4e5f6-a7b8-9012-cdef-123456789012',
        'c3d4e5f6-a7b8-9012-cdef-123456789013',
        'demo@jarvis.com',
        '\$2a\$10\$G7V86I50b3Z0iexpJesSWuo3AlFo0tWY.GqvNbRodfdahpqv6qh0i',
        'password',
        false,
        NOW() - INTERVAL '30 days',
        NOW()
    )
    ON CONFLICT (id) DO UPDATE SET
        email = EXCLUDED.email,
        is_active = EXCLUDED.is_active,
        last_updated = NOW();
    "
    
    echo "Creating default navigation items..."
    execute_sql_db $DB_NAME "
    INSERT INTO navigation_item_component (
        id, 
        owner_entity_id, 
        menu_id,
        label, 
        icon,
        href,
        sort_order,
        is_active,
        last_updated
    ) VALUES 
    (
        '550e8400-e29b-41d4-a716-446655440001',
        '550e8400-e29b-41d4-a716-446655440011',
        'main',
        'Dashboard',
        'LayoutDashboard',
        '/',
        1,
        true,
        NOW()
    ),
    (
        '550e8400-e29b-41d4-a716-446655440002',
        '550e8400-e29b-41d4-a716-446655440012', 
        'main',
        'Accounts',
        'Users',
        '/accounts',
        2,
        true,
        NOW()
    ),
    (
        '550e8400-e29b-41d4-a716-446655440003',
        '550e8400-e29b-41d4-a716-446655440013',
        'main', 
        'Schema',
        'Database',
        '/schema',
        3,
        true,
        NOW()
    )
    ON CONFLICT (id) DO UPDATE SET
        label = EXCLUDED.label,
        href = EXCLUDED.href,
        is_active = EXCLUDED.is_active,
        last_updated = NOW();
    "
fi

# Setup GraphQL if extension is available
echo "Setting up GraphQL..."
execute_sql_db $DB_NAME "
DO \$\$
BEGIN
    -- Check if pg_graphql is installed
    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_graphql') THEN
        -- Create GraphQL schema if it doesn't exist
        CREATE SCHEMA IF NOT EXISTS graphql;
        
        -- Grant permissions
        GRANT USAGE ON SCHEMA graphql TO postgres;
        GRANT ALL ON ALL TABLES IN SCHEMA graphql TO postgres;
        GRANT ALL ON ALL SEQUENCES IN SCHEMA graphql TO postgres;
        GRANT ALL ON ALL FUNCTIONS IN SCHEMA graphql TO postgres;
        
        RAISE NOTICE 'GraphQL setup completed';
    ELSE
        RAISE NOTICE 'pg_graphql extension not found - skipping GraphQL setup';
    END IF;
END \$\$;
"

echo "========================================="
echo "✅ Database setup completed successfully!"
echo "========================================="
echo ""
echo "📋 Summary:"
echo "  • Database created: $DB_NAME"
echo "  • Tables created with proper indexes"
echo "  • Default test user: test@example.com / TestPassword123!"
echo "  • Navigation items configured"
echo "  • GraphQL extension configured (if available)"
echo ""
echo "🔌 Connection details:"
echo "  Host: $DB_HOST"
echo "  Port: $DB_PORT"
echo "  Database: $DB_NAME"
echo "  User: $DB_USER"
echo ""
echo "💡 To connect manually:"
echo "   psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME"
echo ""
echo "🚀 Ready to start the API with: func start --port 7071"