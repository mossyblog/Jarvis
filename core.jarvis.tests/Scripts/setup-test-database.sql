-- Setup script for Jarvis integration tests
-- Run this against your local PostgreSQL test instance
-- WARNING: This script DROPS and recreates the entire database - only use in test environments!

-- Drop the test database if it exists and recreate it
DROP DATABASE IF EXISTS jarvis_test;
CREATE DATABASE jarvis_test;

-- Connect to the new database
\c jarvis_test;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enable GraphQL extension (requires elevated permissions)
-- This will fail silently if not available
DO $$
BEGIN
    CREATE EXTENSION IF NOT EXISTS pg_graphql;
    RAISE NOTICE 'pg_graphql extension created successfully';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'pg_graphql extension not available - GraphQL tests will use mock responses';
END $$;

-- Drop existing tables (CASCADE will drop dependent objects)
DROP TABLE IF EXISTS "account" CASCADE;
DROP TABLE IF EXISTS account_component CASCADE;
DROP TABLE IF EXISTS auth_token CASCADE;
DROP TABLE IF EXISTS auth_token_component CASCADE;
DROP TABLE IF EXISTS security_token CASCADE;
DROP TABLE IF EXISTS audit_event CASCADE;
DROP TABLE IF EXISTS audit_event_component CASCADE;
DROP TABLE IF EXISTS security_audit_event CASCADE;
DROP TABLE IF EXISTS security_audit_event_component CASCADE;
DROP TABLE IF EXISTS security_profile CASCADE;
DROP TABLE IF EXISTS security_profile_component CASCADE;
DROP TABLE IF EXISTS role CASCADE;
DROP TABLE IF EXISTS role_component CASCADE;
DROP TABLE IF EXISTS permission CASCADE;
DROP TABLE IF EXISTS permission_component CASCADE;
DROP TABLE IF EXISTS test_component CASCADE;
DROP TABLE IF EXISTS test_component_component CASCADE;
DROP TABLE IF EXISTS position_component CASCADE;
DROP TABLE IF EXISTS position_component_component CASCADE;
DROP TABLE IF EXISTS velocity_component CASCADE;
DROP TABLE IF EXISTS velocity_component_component CASCADE;
DROP TABLE IF EXISTS blog_component CASCADE;
DROP TABLE IF EXISTS blog_component_component CASCADE;
DROP TABLE IF EXISTS blog_post_component CASCADE;
DROP TABLE IF EXISTS blog_post_component_component CASCADE;
DROP TABLE IF EXISTS component_snapshots CASCADE;
DROP TABLE IF EXISTS order_component CASCADE;
DROP TABLE IF EXISTS order_component_component CASCADE;
DROP TABLE IF EXISTS invoice_test_component CASCADE;
DROP TABLE IF EXISTS payment_test_component CASCADE;
DROP TABLE IF EXISTS work_order_test_component CASCADE;
DROP TABLE IF EXISTS work_order_test_component_component CASCADE;
DROP TABLE IF EXISTS invoice_test_component_component CASCADE;
DROP TABLE IF EXISTS payment_test_component_component CASCADE;
DROP TABLE IF EXISTS entity_relationship CASCADE;
DROP TABLE IF EXISTS entity_relationship_component CASCADE;
DROP TABLE IF EXISTS navigation_item CASCADE;
DROP TABLE IF EXISTS test_component_component CASCADE;
DROP TABLE IF EXISTS work_order_component_component CASCADE;

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
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address VARCHAR(45),
    user_agent TEXT
);

-- Create indexes for account_component table
CREATE INDEX IF NOT EXISTS idx_account_owner_entity_id ON account_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_account_email ON account_component(email);

-- Create test account with hashed password
-- Password: 'test123' (bcrypt hash with cost factor 12)
-- We'll use a fixed UUID for the test user so we can create the security profile
-- Note: The security profile will be created after the security_profile table is created

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
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for auth_token_component
CREATE INDEX IF NOT EXISTS idx_auth_token_owner_entity_id ON auth_token_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_session_id ON auth_token_component(session_id);
CREATE INDEX IF NOT EXISTS idx_auth_token_refresh_expires_at ON auth_token_component(refresh_expires_at) WHERE is_revoked = FALSE;

-- Create security_profile_component table (follows ECS naming)
CREATE TABLE IF NOT EXISTS security_profile_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL DEFAULT '',
    avatar VARCHAR(500),
    role_ids TEXT[] DEFAULT '{}',
    permission_ids TEXT[] DEFAULT '{}',
    preferences TEXT DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for security_profile_component
CREATE INDEX IF NOT EXISTS idx_security_profile_owner_entity_id ON security_profile_component(owner_entity_id);

-- Create role_component table (follows ECS naming)
CREATE TABLE IF NOT EXISTS role_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    permission_ids TEXT[] DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1
);

-- Create indexes for role_component
CREATE INDEX IF NOT EXISTS idx_role_owner_entity_id ON role_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_role_name ON role_component(name);

-- Create permission_component table (follows ECS naming)
CREATE TABLE IF NOT EXISTS permission_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    resource VARCHAR(100) NOT NULL,
    actions TEXT[] DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1
);

-- Create indexes for permission_component
CREATE INDEX IF NOT EXISTS idx_permission_owner_entity_id ON permission_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_permission_resource ON permission_component(resource);

-- Create navigation_item table
CREATE TABLE IF NOT EXISTS navigation_item (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    title VARCHAR(255) NOT NULL,
    path VARCHAR(500) NOT NULL,
    icon VARCHAR(100),
    parent_id UUID,
    sort_order INTEGER NOT NULL DEFAULT 0,
    permission_required VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1
);

-- Create indexes for navigation_item
CREATE INDEX IF NOT EXISTS idx_navigation_item_owner_entity_id ON navigation_item(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_navigation_item_parent_id ON navigation_item(parent_id);
CREATE INDEX IF NOT EXISTS idx_navigation_item_sort_order ON navigation_item(sort_order);

-- Create audit_event table
CREATE TABLE IF NOT EXISTS audit_event (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    user_id VARCHAR(255) NOT NULL DEFAULT 'SYSTEM',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    old_value TEXT,
    new_value TEXT,
    metadata JSONB,
    transaction_id VARCHAR(100),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_audit_event_entity_id ON audit_event(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_event_event_type ON audit_event(event_type);
CREATE INDEX IF NOT EXISTS idx_audit_event_timestamp ON audit_event(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_event_transaction_id ON audit_event(transaction_id);

-- Create audit_event_component table (ECS naming: AuditEvent -> audit_event_component)
CREATE TABLE IF NOT EXISTS audit_event_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    user_id VARCHAR(255) NOT NULL DEFAULT 'SYSTEM',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    old_value TEXT,
    new_value TEXT,
    metadata JSONB,
    transaction_id VARCHAR(100),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_audit_event_component_entity_id ON audit_event_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_event_component_event_type ON audit_event_component(event_type);
CREATE INDEX IF NOT EXISTS idx_audit_event_component_timestamp ON audit_event_component(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_event_component_transaction_id ON audit_event_component(transaction_id);

-- Create security_audit_event_component table for API security auditing (follows ECS naming)
CREATE TABLE IF NOT EXISTS security_audit_event_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    event_type VARCHAR(255) NOT NULL,
    event_time TIMESTAMPTZ NOT NULL,
    ip_address VARCHAR(45),
    user_agent TEXT,
    severity VARCHAR(20) NOT NULL DEFAULT 'INFO',
    reason TEXT,
    target_email VARCHAR(255),
    session_id UUID,
    target_user_id UUID,
    role_id VARCHAR(255),
    permission_id VARCHAR(255),
    failed_attempts INTEGER,
    locked_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for security_audit_event_component
CREATE INDEX IF NOT EXISTS idx_security_audit_event_owner ON security_audit_event_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_security_audit_event_type ON security_audit_event_component(event_type);
CREATE INDEX IF NOT EXISTS idx_security_audit_event_time ON security_audit_event_component(event_time);

-- Create test_component table for integration tests
CREATE TABLE IF NOT EXISTS test_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    value INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'ACTIVE',
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT test_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_test_component_owner_entity_id ON test_component(owner_entity_id);

-- Create test_component_component table (follows ECS naming convention)
-- This is required for core.jarvis.tests which use TestComponent
CREATE TABLE IF NOT EXISTS test_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    value INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'ACTIVE',
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT test_component_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_test_component_component_owner_entity_id ON test_component_component(owner_entity_id);

-- Create position_component table for integration tests
CREATE TABLE IF NOT EXISTS position_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    x REAL NOT NULL DEFAULT 0,
    y REAL NOT NULL DEFAULT 0,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT position_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_position_component_owner_entity_id ON position_component(owner_entity_id);

-- Create position_component_component table (ECS naming: PositionComponent -> position_component_component)
CREATE TABLE IF NOT EXISTS position_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    x REAL NOT NULL DEFAULT 0,
    y REAL NOT NULL DEFAULT 0,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT position_component_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_position_component_component_owner_entity_id ON position_component_component(owner_entity_id);

-- Create velocity_component table for integration tests
CREATE TABLE IF NOT EXISTS velocity_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    delta_x REAL NOT NULL DEFAULT 0,
    delta_y REAL NOT NULL DEFAULT 0,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT velocity_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_velocity_component_owner_entity_id ON velocity_component(owner_entity_id);

-- Create velocity_component_component table (ECS naming: VelocityComponent -> velocity_component_component)
CREATE TABLE IF NOT EXISTS velocity_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    delta_x REAL NOT NULL DEFAULT 0,
    delta_y REAL NOT NULL DEFAULT 0,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT velocity_component_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_velocity_component_component_owner_entity_id ON velocity_component_component(owner_entity_id);

-- Create blog table for blog example
CREATE TABLE IF NOT EXISTS blog_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    theme VARCHAR(100),
    tone VARCHAR(100),
    target_audience VARCHAR(255),
    settings JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT blog_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_blog_component_owner_entity_id ON blog_component(owner_entity_id);

-- Create blog_component_component table (ECS naming: BlogComponent -> blog_component_component)
CREATE TABLE IF NOT EXISTS blog_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    theme VARCHAR(100),
    tone VARCHAR(100),
    target_audience VARCHAR(255),
    settings JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT blog_component_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_blog_component_component_owner_entity_id ON blog_component_component(owner_entity_id);

-- Create blog_post table for blog example
CREATE TABLE IF NOT EXISTS blog_post_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    excerpt TEXT,
    tags TEXT[],
    status VARCHAR(50) NOT NULL DEFAULT 'draft',
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    word_count INTEGER NOT NULL DEFAULT 0,
    published_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    archived_at TIMESTAMPTZ,
    version INTEGER DEFAULT 1
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_blog_post_component_owner_entity_id ON blog_post_component(owner_entity_id);

-- Create blog_post_component_component table (ECS naming: BlogPostComponent -> blog_post_component_component)
CREATE TABLE IF NOT EXISTS blog_post_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    excerpt TEXT,
    tags TEXT[],
    status VARCHAR(50) NOT NULL DEFAULT 'draft',
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    word_count INTEGER NOT NULL DEFAULT 0,
    published_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    archived_at TIMESTAMPTZ,
    version INTEGER DEFAULT 1
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_blog_post_component_component_owner_entity_id ON blog_post_component_component(owner_entity_id);

-- Create order_component table for order example
CREATE TABLE IF NOT EXISTS order_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    order_number VARCHAR(255) NOT NULL,
    customer_id VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    total_amount_cents INTEGER NOT NULL DEFAULT 0,
    order_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    notes TEXT,
    shipping_address TEXT NOT NULL,
    is_paid BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT order_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_order_component_owner_entity_id ON order_component(owner_entity_id);

-- Create order_component_component table (ECS naming: OrderComponent -> order_component_component)
CREATE TABLE IF NOT EXISTS order_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    order_number VARCHAR(255) NOT NULL,
    customer_id VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    total_amount_cents INTEGER NOT NULL DEFAULT 0,
    order_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    notes TEXT,
    shipping_address TEXT NOT NULL,
    is_paid BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT order_component_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_order_component_component_owner_entity_id ON order_component_component(owner_entity_id);

-- Create invoice_test_component table
CREATE TABLE IF NOT EXISTS invoice_test_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    work_order_id UUID,
    invoice_number VARCHAR(255) NOT NULL,
    amount INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    due_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT invoice_test_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_invoice_test_component_owner_entity_id ON invoice_test_component(owner_entity_id);

-- Create invoice_test_component_component table (ECS naming: InvoiceTestComponent -> invoice_test_component_component)
CREATE TABLE IF NOT EXISTS invoice_test_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    work_order_id UUID,
    invoice_number VARCHAR(255) NOT NULL,
    amount INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    due_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT invoice_test_component_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_invoice_test_component_component_owner_entity_id ON invoice_test_component_component(owner_entity_id);

-- Create payment_test_component table
CREATE TABLE IF NOT EXISTS payment_test_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    payment_method VARCHAR(50) NOT NULL,
    amount INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    processed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT payment_test_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_payment_test_component_owner_entity_id ON payment_test_component(owner_entity_id);

-- Create payment_test_component_component table (ECS naming: PaymentTestComponent -> payment_test_component_component)
CREATE TABLE IF NOT EXISTS payment_test_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    payment_method VARCHAR(50) NOT NULL,
    amount INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    processed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT payment_test_component_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_payment_test_component_component_owner_entity_id ON payment_test_component_component(owner_entity_id);

-- Create work_order_test_component table
CREATE TABLE IF NOT EXISTS work_order_test_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    work_order_id VARCHAR(255),
    work_order_number VARCHAR(255) NOT NULL,
    description TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    assigned_to VARCHAR(255),
    is_pre_payment_required BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT work_order_test_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_work_order_test_component_owner_entity_id ON work_order_test_component(owner_entity_id);

-- Create work_order_test_component_component table (ECS naming: WorkOrderTestComponent -> work_order_test_component_component)
CREATE TABLE IF NOT EXISTS work_order_test_component_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    work_order_id VARCHAR(255),
    work_order_number VARCHAR(255) NOT NULL,
    description TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    assigned_to VARCHAR(255),
    is_pre_payment_required BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT work_order_test_component_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX IF NOT EXISTS idx_work_order_test_component_component_owner_entity_id ON work_order_test_component_component(owner_entity_id);

-- Create work_order_component table (for WorkOrderHandler)
CREATE TABLE IF NOT EXISTS work_order_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL,
    work_order_number VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(50) NOT NULL,
    priority VARCHAR(50) NOT NULL,
    assigned_to_account_id UUID,
    scheduled_date TIMESTAMP,
    completed_date TIMESTAMP,
    estimated_hours DECIMAL(10,2) NOT NULL,
    actual_hours DECIMAL(10,2) NOT NULL DEFAULT 0,
    notes TEXT,
    approved_by_account_id UUID,
    approved_date TIMESTAMP,
    cancellation_reason TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create index on owner_entity_id for performance
CREATE INDEX IF NOT EXISTS idx_work_order_component_owner_entity_id ON work_order_component(owner_entity_id);

-- Create work_order_component_component table (ECS naming: WorkOrderComponent -> work_order_component_component)
CREATE TABLE IF NOT EXISTS work_order_component_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL,
    work_order_number VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(50) NOT NULL,
    priority VARCHAR(50) NOT NULL,
    assigned_to_account_id UUID,
    scheduled_date TIMESTAMP,
    completed_date TIMESTAMP,
    estimated_hours DECIMAL(10,2) NOT NULL,
    actual_hours DECIMAL(10,2) NOT NULL DEFAULT 0,
    notes TEXT,
    approved_by_account_id UUID,
    approved_date TIMESTAMP,
    cancellation_reason TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create index on owner_entity_id for performance
CREATE INDEX IF NOT EXISTS idx_work_order_component_component_owner_entity_id ON work_order_component_component(owner_entity_id);

-- Create component_snapshots table for versioning support
CREATE TABLE IF NOT EXISTS component_snapshots (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id UUID NOT NULL,
    component_type VARCHAR(255) NOT NULL,
    component_id UUID NOT NULL UNIQUE,
    snapshots JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for component_snapshots
CREATE INDEX IF NOT EXISTS idx_snapshots_entity ON component_snapshots(entity_id);
CREATE INDEX IF NOT EXISTS idx_snapshots_component ON component_snapshots(component_id);
CREATE INDEX IF NOT EXISTS idx_snapshots_type ON component_snapshots(component_type);

-- Create entity_relationship table for tracking parent-child relationships
CREATE TABLE IF NOT EXISTS entity_relationship (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL UNIQUE,
    parent_id UUID,
    children_ids UUID[] DEFAULT '{}',
    parent_type TEXT,
    child_types JSONB DEFAULT '{}',
    version INT,
    last_updated TIMESTAMPTZ DEFAULT NOW(),
    
    -- Index for efficient lookups
    CONSTRAINT idx_entity_relationship_owner UNIQUE (owner_entity_id)
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_entity_relationship_parent_id ON entity_relationship(parent_id);
CREATE INDEX IF NOT EXISTS idx_entity_relationship_last_updated ON entity_relationship(last_updated);

-- Create entity_relationship_component table (ECS naming: EntityRelationship -> entity_relationship_component)
CREATE TABLE IF NOT EXISTS entity_relationship_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL UNIQUE,
    parent_id UUID,
    children_ids UUID[] DEFAULT '{}',
    parent_type TEXT,
    child_types JSONB DEFAULT '{}',
    version INT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated TIMESTAMPTZ DEFAULT NOW()
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_entity_relationship_component_owner_entity_id ON entity_relationship_component(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_entity_relationship_component_parent_id ON entity_relationship_component(parent_id);
CREATE INDEX IF NOT EXISTS idx_entity_relationship_component_last_updated ON entity_relationship_component(last_updated);

-- Enable RLS on account_component table (required by PgClient)
ALTER TABLE account_component ENABLE ROW LEVEL SECURITY;

-- Create a simple RLS policy for account_component table (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = 'public' AND tablename = 'account_component' AND policyname = 'Enable all for development') THEN
        CREATE POLICY "Enable all for development" ON account_component
            FOR ALL USING (true) WITH CHECK (true);
    END IF;
END $$;

-- Insert test data now that all tables exist
-- DISABLED: Test data creation causes conflicts when multiple tests run
-- Each test should create its own test data with unique IDs
-- DO $$
-- DECLARE
--     test_user_id UUID := '11111111-1111-1111-1111-111111111111';
-- BEGIN
--     -- Create test account
--     INSERT INTO account_component (owner_entity_id, email, password_hash, password, auth_method, is_active) VALUES 
--         (test_user_id, 'test@example.com', '$2a$12$QnOKnn.PumrtVscPkO3C.ONHR/5NANzEbqMoLQOyUFhQMhynyVoe.', '', 'password', true);
--     
--     -- Create security profile for the test account
--     INSERT INTO security_profile_component (owner_entity_id, name, avatar, role_ids, permission_ids, preferences) VALUES
--         (test_user_id, 'Test User', NULL, '{}', '{}', '{}');
-- END $$;

-- Create GraphQL wrapper function for tests
-- This allows tests to call GraphQL even if the full schema isn't available
CREATE OR REPLACE FUNCTION public.graphql_resolve(query text)
RETURNS json
LANGUAGE plpgsql
AS $function$
BEGIN
    -- Check if graphql.resolve exists (i.e., if pg_graphql extension is available)
    IF EXISTS (
        SELECT 1 FROM pg_proc p
        JOIN pg_namespace n ON p.pronamespace = n.oid
        WHERE n.nspname = 'graphql' AND p.proname = 'resolve'
    ) THEN
        -- Use real GraphQL if available
        RETURN graphql.resolve(query);
    ELSE
        -- Return a mock response for testing when GraphQL is not available
        RETURN json_build_object(
            'data', json_build_object('__typename', 'Query'),
            'errors', NULL
        );
    END IF;
END;
$function$;

-- Grant execute permission to all users
GRANT EXECUTE ON FUNCTION public.graphql_resolve(text) TO PUBLIC;

-- Display confirmation
DO $$
BEGIN
    RAISE NOTICE 'Test database setup complete!';
    RAISE NOTICE 'Tables created:';
    RAISE NOTICE '  - account_component';
    RAISE NOTICE '  - auth_token';
    RAISE NOTICE '  - security_profile';
    RAISE NOTICE '  - role';
    RAISE NOTICE '  - permission';
    RAISE NOTICE '  - audit_event';
    RAISE NOTICE '  - test_component';
    RAISE NOTICE '  - position_component';
    RAISE NOTICE '  - velocity_component';
    RAISE NOTICE '  - blog_component';
    RAISE NOTICE '  - blog_post_component';
    RAISE NOTICE '  - order_component';
    RAISE NOTICE '  - invoice_test_component';
    RAISE NOTICE '  - payment_test_component';
    RAISE NOTICE '  - work_order_test_component';
    RAISE NOTICE '  - work_order_component';
    RAISE NOTICE '  - component_snapshots';
    RAISE NOTICE '  - entity_relationship';
    RAISE NOTICE 'GraphQL wrapper function created for tests';
END $$;