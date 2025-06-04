-- Create the entity_relationship table for tracking parent-child relationships
CREATE TABLE IF NOT EXISTS entity_relationship (
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
CREATE INDEX IF NOT EXISTS idx_entity_relationship_parent_id ON entity_relationship(parent_id);
CREATE INDEX IF NOT EXISTS idx_entity_relationship_updated_at ON entity_relationship(updated_at);

-- Add comment to describe the table
COMMENT ON TABLE entity_relationship IS 'Tracks parent-child relationships between entities in the ECS system';
COMMENT ON COLUMN entity_relationship.owner_entity_id IS 'The entity that owns this relationship record';
COMMENT ON COLUMN entity_relationship.parent_id IS 'The parent entity ID if this entity has a parent';
COMMENT ON COLUMN entity_relationship.children_ids IS 'Array of child entity IDs';
COMMENT ON COLUMN entity_relationship.parent_type IS 'Type of the parent entity (e.g., WorkOrder)';
COMMENT ON COLUMN entity_relationship.child_types IS 'JSON mapping of child entity IDs to their types';
COMMENT ON COLUMN entity_relationship.version IS 'Version number for optimistic concurrency control';