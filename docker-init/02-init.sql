-- Jarvis Database SQL Initialization
-- This SQL script creates all tables, indexes, and seeds default data
-- 
-- Default test user created:
-- - Email: test@example.com  
-- - Password: TestPassword123!

-- Create account_component table (based on Account.cs model)
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
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_account_component_owner_entity_id ON account_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_account_component_last_updated ON account_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_account_component_email ON account_component(email);

-- Create security_profile_component table
CREATE TABLE IF NOT EXISTS security_profile_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    name TEXT NOT NULL DEFAULT '',
    role_ids TEXT[] NOT NULL DEFAULT '{}',
    permission_ids TEXT[] NOT NULL DEFAULT '{}'
);

CREATE INDEX IF NOT EXISTS idx_security_profile_component_owner_entity_id ON security_profile_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_security_profile_component_last_updated ON security_profile_component(last_updated);

-- Create navigation_item_component table
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
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_navigation_item_component_owner_entity_id ON navigation_item_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_last_updated ON navigation_item_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_menu_id ON navigation_item_component(menu_id);
CREATE INDEX IF NOT EXISTS idx_navigation_item_component_sort_order ON navigation_item_component(sort_order);

-- Create role_component table
CREATE TABLE IF NOT EXISTS role_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    name TEXT NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    is_system BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_role_component_owner_entity_id ON role_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_role_component_last_updated ON role_component(last_updated);

-- Create permission_component table
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

-- Create auth_token_component table
CREATE TABLE IF NOT EXISTS auth_token_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID UNIQUE NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    session_id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
    token_hash TEXT NOT NULL DEFAULT '',
    token_type TEXT NOT NULL DEFAULT 'access',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '1 hour'),
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMPTZ,
    last_used_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_auth_token_component_owner_entity_id ON auth_token_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_component_last_updated ON auth_token_component(last_updated);
CREATE INDEX IF NOT EXISTS idx_auth_token_component_session_id ON auth_token_component(session_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_component_user_id ON auth_token_component(user_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_component_expires_at ON auth_token_component(expires_at);

-- Seed default test user
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
    '$2a$10$G7V86I50b3Z0iexpJesSWuo3AlFo0tWY.GqvNbRodfdahpqv6qh0i',
    'password',
    true,
    NOW(),
    NOW()
) ON CONFLICT (id) DO UPDATE SET
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    is_active = EXCLUDED.is_active,
    last_updated = NOW();

-- Seed navigation items
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
),
(
    '550e8400-e29b-41d4-a716-446655440004',
    '550e8400-e29b-41d4-a716-446655440014',
    'main',
    'Pages',
    'FileText',
    '/pages',
    4,
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

-- Setup GraphQL extension if available
DO $$
BEGIN
    -- Check if pg_graphql extension exists and create it
    IF EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'pg_graphql') THEN
        CREATE EXTENSION IF NOT EXISTS pg_graphql CASCADE;
        
        -- Grant necessary permissions for GraphQL
        GRANT USAGE ON SCHEMA graphql TO postgres;
        GRANT ALL ON ALL TABLES IN SCHEMA graphql TO postgres;
        GRANT ALL ON ALL SEQUENCES IN SCHEMA graphql TO postgres;
        GRANT ALL ON ALL FUNCTIONS IN SCHEMA graphql TO postgres;
        
        RAISE NOTICE 'GraphQL setup completed';
    ELSE
        RAISE NOTICE 'pg_graphql extension not found - skipping GraphQL setup';
    END IF;
END $$;