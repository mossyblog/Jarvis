#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_URL") ?? 
    "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine($"Connecting to: {connectionString}");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // Drop and recreate entity_relationship table
    var sql = @"
DROP TABLE IF EXISTS entity_relationship CASCADE;

-- Create entity_relationship table for tracking parent-child relationships
CREATE TABLE entity_relationship (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL UNIQUE,
    parent_id UUID,
    children_ids UUID[] DEFAULT '{}',
    parent_type TEXT,
    child_types JSONB DEFAULT '{}',
    version INT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Index for efficient lookups
    CONSTRAINT idx_entity_relationship_owner UNIQUE (owner_entity_id)
);

-- Create indexes for performance
CREATE INDEX idx_entity_relationship_parent_id ON entity_relationship(parent_id);
CREATE INDEX idx_entity_relationship_updated_at ON entity_relationship(updated_at);
";
    
    using var cmd = new NpgsqlCommand(sql, connection);
    await cmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("entity_relationship table created successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}