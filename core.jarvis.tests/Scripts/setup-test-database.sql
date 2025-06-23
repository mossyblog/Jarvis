-- Setup script for Jarvis integration tests
-- Run this against your local PostgreSQL test instance
-- WARNING: This script DROPS and recreates tables - only use in test environments!

-- Enable required extensions for Supabase compatibility
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_graphql";

-- Create graphql schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS graphql;

-- Note: GraphQL wrapper function is created separately in setup-graphql-wrapper.sql
-- as it needs to be run as supabase_admin user

-- Drop existing tables (CASCADE will drop dependent objects)
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS security_token CASCADE;
DROP TABLE IF EXISTS audit_event CASCADE;
DROP TABLE IF EXISTS test_component CASCADE;
DROP TABLE IF EXISTS position_component CASCADE;
DROP TABLE IF EXISTS velocity_component CASCADE;
DROP TABLE IF EXISTS blog CASCADE;
DROP TABLE IF EXISTS blog_post CASCADE;
DROP TABLE IF EXISTS component_snapshots CASCADE;
DROP TABLE IF EXISTS order_component CASCADE;
DROP TABLE IF EXISTS invoice_test_component CASCADE;
DROP TABLE IF EXISTS payment_test_component CASCADE;
DROP TABLE IF EXISTS work_order_test_component CASCADE;
DROP TABLE IF EXISTS entity_relationship CASCADE;

-- Create users table for PgClient authentication
CREATE TABLE users (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create test user with hashed password
-- Password: 'test123' (bcrypt hash with cost factor 12)
INSERT INTO users (email, password_hash) VALUES 
    ('test@example.com', '$2a$12$QnOKnn.PumrtVscPkO3C.ONHR/5NANzEbqMoLQOyUFhQMhynyVoe.');

-- Create security_token table for JWT refresh tokens
CREATE TABLE security_token (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    user_id UUID NOT NULL REFERENCES users(id),
    session_id UUID NOT NULL,
    refresh_token_hash TEXT NOT NULL,
    issued_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    refresh_expires_at TIMESTAMPTZ NOT NULL,
    client_id TEXT,
    ip_address TEXT,
    user_agent TEXT,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMPTZ,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for security_token
CREATE INDEX idx_security_token_owner_entity_id ON security_token(owner_entity_id);
CREATE INDEX idx_security_token_user_id ON security_token(user_id);
CREATE INDEX idx_security_token_session_id ON security_token(session_id);
CREATE INDEX idx_security_token_refresh_expires_at ON security_token(refresh_expires_at) WHERE is_revoked = FALSE;

-- Create audit_event table
CREATE TABLE audit_event (
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
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1
);

-- Create indexes for performance
CREATE INDEX idx_audit_event_entity_id ON audit_event(owner_entity_id);
CREATE INDEX idx_audit_event_event_type ON audit_event(event_type);
CREATE INDEX idx_audit_event_timestamp ON audit_event(timestamp);
CREATE INDEX idx_audit_event_transaction_id ON audit_event(transaction_id);

-- Create test_component table for integration tests
CREATE TABLE test_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    value INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'ACTIVE',
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT test_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_test_component_owner_entity_id ON test_component(owner_entity_id);

-- Create position_component table for integration tests
CREATE TABLE position_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    x REAL NOT NULL DEFAULT 0,
    y REAL NOT NULL DEFAULT 0,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT position_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_position_component_owner_entity_id ON position_component(owner_entity_id);

-- Create velocity_component table for integration tests
CREATE TABLE velocity_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    delta_x REAL NOT NULL DEFAULT 0,
    delta_y REAL NOT NULL DEFAULT 0,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT velocity_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_velocity_component_owner_entity_id ON velocity_component(owner_entity_id);

-- Create blog table for blog example
CREATE TABLE blog_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    theme VARCHAR(100),
    tone VARCHAR(100),
    target_audience VARCHAR(255),
    settings JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT blog_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_blog_component_owner_entity_id ON blog_component(owner_entity_id);

-- Create blog_post table for blog example
CREATE TABLE blog_post_component (
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
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    archived_at TIMESTAMPTZ,
    version INTEGER DEFAULT 1
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_blog_post_component_owner_entity_id ON blog_post_component(owner_entity_id);

-- Create order_component table for order example
CREATE TABLE order_component (
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
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT order_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_order_component_owner_entity_id ON order_component(owner_entity_id);

-- Create invoice_test_component table
CREATE TABLE invoice_test_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    work_order_id UUID,
    invoice_number VARCHAR(255) NOT NULL,
    amount INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    due_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT invoice_test_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_invoice_test_component_owner_entity_id ON invoice_test_component(owner_entity_id);

-- Create payment_test_component table
CREATE TABLE payment_test_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    payment_method VARCHAR(50) NOT NULL,
    amount INTEGER NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    processed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT payment_test_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_payment_test_component_owner_entity_id ON payment_test_component(owner_entity_id);

-- Create work_order_test_component table
CREATE TABLE work_order_test_component (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    work_order_id VARCHAR(255),
    work_order_number VARCHAR(255) NOT NULL,
    description TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    assigned_to VARCHAR(255),
    is_pre_payment_required BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1,
    CONSTRAINT work_order_test_component_owner_entity_id_unique UNIQUE (owner_entity_id)
);

-- Create index for owner_entity_id queries
CREATE INDEX idx_work_order_test_component_owner_entity_id ON work_order_test_component(owner_entity_id);

-- Create component_snapshots table for versioning support
CREATE TABLE component_snapshots (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id UUID NOT NULL,
    component_type VARCHAR(255) NOT NULL,
    component_id UUID NOT NULL UNIQUE,
    snapshots JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for component_snapshots
CREATE INDEX idx_snapshots_entity ON component_snapshots(entity_id);
CREATE INDEX idx_snapshots_component ON component_snapshots(component_id);
CREATE INDEX idx_snapshots_type ON component_snapshots(component_type);

-- Create entity_relationship table for tracking parent-child relationships
CREATE TABLE entity_relationship (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL UNIQUE,
    parent_id UUID,
    children_ids UUID[] DEFAULT '{}',
    parent_type TEXT,
    child_types JSONB DEFAULT '{}',
    version INT,
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Index for efficient lookups
    CONSTRAINT idx_entity_relationship_owner UNIQUE (owner_entity_id)
);

-- Create indexes for performance
CREATE INDEX idx_entity_relationship_parent_id ON entity_relationship(parent_id);
CREATE INDEX idx_entity_relationship_updated_at ON entity_relationship(updated_at);

-- Enable RLS on users table (required by PgClient)
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Create a simple RLS policy for users table
CREATE POLICY "Users can view their own data" ON users
    FOR SELECT USING (true);

-- Display confirmation
DO $$
BEGIN
    RAISE NOTICE 'Test database setup complete!';
    RAISE NOTICE 'Tables created:';
    RAISE NOTICE '  - users (with test user: test@example.com / test123)';
    RAISE NOTICE '  - security_token';
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
    RAISE NOTICE '  - component_snapshots';
    RAISE NOTICE '  - entity_relationship';
END $$;