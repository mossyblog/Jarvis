#!/bin/bash

# Jarvis Database Docker Initialization Script
# This script runs automatically when the PostgreSQL container starts for the first time
# 
# This script is idempotent - it can be run multiple times safely.
# It will create tables, default users, and navigation items.
#
# Default test user created:
# - Email: test@example.com
# - Password: TestPassword123!

set -e  # Exit on any error

DB_NAME=${POSTGRES_DB:-jarvis_test}
DB_USER=${POSTGRES_USER:-postgres}

echo "========================================="
echo "🚀 Jarvis Database Docker Initialization"
echo "Database: $DB_NAME"
echo "User: $DB_USER"
echo "========================================="

# Function to execute SQL commands (runs inside container, no docker exec needed)
execute_sql() {
    psql -U "$DB_USER" -c "$1"
}

# Function to execute SQL commands in a specific database
execute_sql_db() {
    psql -U "$DB_USER" -d "$1" -c "$2"
}

# Create database if it doesn't exist (DB already exists in Docker initialization)
echo "📊 Database $DB_NAME already exists..."

# Create supabase_admin role (required for pg_graphql)
echo "👤 Creating supabase_admin role..."
execute_sql "CREATE ROLE supabase_admin WITH LOGIN SUPERUSER PASSWORD 'postgres'" || true

# Enable pg_graphql extension
echo "🔗 Enabling pg_graphql extension..."
execute_sql_db "$DB_NAME" "CREATE EXTENSION IF NOT EXISTS pg_graphql CASCADE"

# Create account_component table (based on Account.cs model)
echo "👤 Creating account_component table..."
execute_sql_db "$DB_NAME" "
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
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address TEXT,
    user_agent TEXT
);

CREATE INDEX IF NOT EXISTS idx_account_component_owner_entity_id ON account_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_account_component_last_updated ON account_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_account_component_email ON account_component(email);
"

# Create security_profile_component table
echo "🔐 Creating security_profile_component table..."
execute_sql_db "$DB_NAME" "
CREATE TABLE IF NOT EXISTS security_profile_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    name TEXT NOT NULL DEFAULT '',
    avatar TEXT,
    role_ids TEXT[] NOT NULL DEFAULT '{}',
    permission_ids TEXT[] NOT NULL DEFAULT '{}',
    preferences TEXT NOT NULL DEFAULT '{}'
);

CREATE INDEX IF NOT EXISTS idx_security_profile_component_owner_entity_id ON security_profile_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_security_profile_component_last_updated ON security_profile_component(last_updated);
"

# Create navigation_item_component table
echo "🧭 Creating navigation_item_component table..."
execute_sql_db "$DB_NAME" "
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
    is_active BOOLEAN NOT NULL DEFAULT true,
    badge_config TEXT
);

CREATE INDEX IF NOT EXISTS idx_navigation_item_component_owner_entity_id ON navigation_item_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_last_updated ON navigation_item_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_menu_id ON navigation_item_component(menu_id);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_sort_order ON navigation_item_component(sort_order);
"

# Create role_component table
echo "👥 Creating role_component table..."
execute_sql_db "$DB_NAME" "
CREATE TABLE IF NOT EXISTS role_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    name TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    permission_ids TEXT[] NOT NULL DEFAULT '{}'
);

CREATE INDEX IF NOT EXISTS idx_role_component_owner_entity_id ON role_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_role_component_last_updated ON role_component(last_updated);
"

# Create permission_component table
echo "🔑 Creating permission_component table..."
execute_sql_db "$DB_NAME" "
CREATE TABLE IF NOT EXISTS permission_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resource TEXT NOT NULL DEFAULT '',
    actions TEXT[] NOT NULL DEFAULT '{}'
);

CREATE INDEX IF NOT EXISTS idx_permission_component_owner_entity_id ON permission_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_permission_component_last_updated ON permission_component(last_updated);
"

# Create auth_token_component table
echo "🎫 Creating auth_token_component table..."
execute_sql_db "$DB_NAME" "
CREATE TABLE IF NOT EXISTS auth_token_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER,
    access_token TEXT NOT NULL DEFAULT '',
    refresh_token TEXT NOT NULL DEFAULT '',
    refresh_token_hash TEXT NOT NULL DEFAULT '',
    expires_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    refresh_expires_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    token_type TEXT NOT NULL DEFAULT 'Bearer',
    session_id UUID NOT NULL DEFAULT gen_random_uuid(),
    client_id TEXT,
    ip_address TEXT,
    user_agent TEXT,
    is_revoked BOOLEAN NOT NULL DEFAULT false,
    revoked_at TIMESTAMPTZ,
    issued_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_auth_token_component_owner_entity_id ON auth_token_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_component_last_updated ON auth_token_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_auth_token_component_session_id ON auth_token_component(session_id);
"

# Create system_setup_component table
echo "⚙️ Creating system_setup_component table..."
execute_sql_db "$DB_NAME" "
CREATE TABLE IF NOT EXISTS system_setup_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    default_roles_created BOOLEAN NOT NULL DEFAULT false,
    default_navigation_created BOOLEAN NOT NULL DEFAULT false,
    last_setup_date TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_system_setup_component_owner_entity_id ON system_setup_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_system_setup_component_last_updated ON system_setup_component(last_updated);
"

# Create token_validation_component table
echo "🔍 Creating token_validation_component table..."
execute_sql_db "$DB_NAME" "
CREATE TABLE IF NOT EXISTS token_validation_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_valid BOOLEAN NOT NULL DEFAULT false,
    user_id UUID,
    expires_at TIMESTAMPTZ,
    claims JSONB,
    error_message TEXT
);

CREATE INDEX IF NOT EXISTS idx_token_validation_component_owner_entity_id ON token_validation_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_token_validation_component_last_updated ON token_validation_component(last_updated);
"

# Create entity_relationship table (for parent-child relationships)
echo "🔗 Creating entity_relationship table..."
execute_sql_db "$DB_NAME" "
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
echo "📸 Creating component_snapshots table..."
execute_sql_db "$DB_NAME" "
CREATE TABLE IF NOT EXISTS component_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id UUID NOT NULL,
    component_type TEXT NOT NULL DEFAULT '',
    component_id UUID NOT NULL,
    snapshots TEXT NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_component_snapshots_entity_id ON component_snapshots(entity_id);
CREATE INDEX IF NOT EXISTS idx_component_snapshots_component_id ON component_snapshots(component_id);
CREATE INDEX IF NOT EXISTS idx_component_snapshots_created_at ON component_snapshots(created_at);
"

# Create audit_event_component table
echo "📋 Creating audit_event_component table..."
execute_sql_db "$DB_NAME" "
CREATE TABLE IF NOT EXISTS audit_event_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    event_type TEXT NOT NULL DEFAULT '',
    entity_id UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    component_type TEXT NOT NULL DEFAULT '',
    user_id TEXT NOT NULL DEFAULT '',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    operation TEXT NOT NULL DEFAULT '',
    old_value TEXT,
    new_value TEXT,
    metadata TEXT
);

CREATE INDEX IF NOT EXISTS idx_audit_event_component_owner_entity_id ON audit_event_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_event_component_last_updated ON audit_event_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_audit_event_component_entity_id ON audit_event_component(entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_event_component_timestamp ON audit_event_component(timestamp);
"

# Seed default test user
echo "👤 Creating default test user..."
execute_sql_db "$DB_NAME" "
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

# Seed navigation items
echo "🧭 Creating navigation items..."
execute_sql_db "$DB_NAME" "
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
    icon = EXCLUDED.icon,
    href = EXCLUDED.href,
    sort_order = EXCLUDED.sort_order,
    is_active = EXCLUDED.is_active,
    last_updated = NOW();
"

# Grant GraphQL permissions
echo "🔗 Granting GraphQL permissions..."
execute_sql_db "$DB_NAME" "
DO \$\$
BEGIN
    -- Grant necessary permissions for GraphQL if schema exists
    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'graphql') THEN
        GRANT USAGE ON SCHEMA graphql TO postgres;
        GRANT ALL ON ALL TABLES IN SCHEMA graphql TO postgres;
        GRANT ALL ON ALL SEQUENCES IN SCHEMA graphql TO postgres;
        GRANT ALL ON ALL FUNCTIONS IN SCHEMA graphql TO postgres;
        
        RAISE NOTICE 'GraphQL permissions granted';
    ELSE
        RAISE NOTICE 'GraphQL schema not found - skipping permissions';
    END IF;
END \$\$;
"

echo "========================================="
echo "✅ Database initialization completed!"
echo "========================================="
echo ""
echo "📋 Summary:"
echo "  • Database created: $DB_NAME"
echo "  • Tables created with proper indexes"
echo "  • Default test user: test@example.com / TestPassword123!"
echo "  • Navigation items configured"
echo "  • GraphQL extension configured (if available)"
echo ""
echo "🚀 Database is ready for the Jarvis API!"