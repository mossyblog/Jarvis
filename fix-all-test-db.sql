-- Fix all database schema issues for tests

-- Ensure entity_relationship table exists with all columns
CREATE TABLE IF NOT EXISTS entity_relationship (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL UNIQUE,
    parent_id UUID,
    children_ids UUID[] DEFAULT '{}',
    parent_type TEXT,
    child_types JSONB DEFAULT '{}',
    version INT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create indexes if they don't exist
CREATE INDEX IF NOT EXISTS idx_entity_relationship_parent_id ON entity_relationship(parent_id);
CREATE INDEX IF NOT EXISTS idx_entity_relationship_updated_at ON entity_relationship(updated_at);

-- Fix work_order_test_component table
ALTER TABLE work_order_test_component 
ADD COLUMN IF NOT EXISTS is_pre_payment_required BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE work_order_test_component 
ADD COLUMN IF NOT EXISTS work_order_id VARCHAR(255) NOT NULL DEFAULT '';

-- Fix invoice_test_component table  
ALTER TABLE invoice_test_component
ADD COLUMN IF NOT EXISTS work_order_id UUID;

-- Update invoice_test_component to allow nulls temporarily for testing
ALTER TABLE invoice_test_component 
ALTER COLUMN invoice_number DROP NOT NULL;

ALTER TABLE invoice_test_component 
ALTER COLUMN amount DROP NOT NULL;

-- Set defaults
ALTER TABLE invoice_test_component 
ALTER COLUMN invoice_number SET DEFAULT '';

ALTER TABLE invoice_test_component 
ALTER COLUMN amount SET DEFAULT 0;