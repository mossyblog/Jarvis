#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Fixing all database schema issues...");

var commands = new[] {
    // Entity relationship table
    @"CREATE TABLE IF NOT EXISTS entity_relationship (
        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
        owner_entity_id UUID NOT NULL UNIQUE,
        parent_id UUID,
        children_ids UUID[] DEFAULT '{}',
        parent_type TEXT,
        child_types JSONB DEFAULT '{}',
        version INT,
        created_at TIMESTAMPTZ DEFAULT NOW(),
        updated_at TIMESTAMPTZ DEFAULT NOW()
    )",
    
    // Indexes
    "CREATE INDEX IF NOT EXISTS idx_entity_relationship_parent_id ON entity_relationship(parent_id)",
    "CREATE INDEX IF NOT EXISTS idx_entity_relationship_updated_at ON entity_relationship(updated_at)",
    
    // Work order columns
    "ALTER TABLE work_order_test_component ADD COLUMN IF NOT EXISTS is_pre_payment_required BOOLEAN NOT NULL DEFAULT FALSE",
    "ALTER TABLE work_order_test_component ADD COLUMN IF NOT EXISTS work_order_id VARCHAR(255) NOT NULL DEFAULT ''",
    
    // Invoice columns
    "ALTER TABLE invoice_test_component ADD COLUMN IF NOT EXISTS work_order_id UUID",
    
    // Make invoice columns nullable for testing
    "ALTER TABLE invoice_test_component ALTER COLUMN invoice_number DROP NOT NULL",
    "ALTER TABLE invoice_test_component ALTER COLUMN amount DROP NOT NULL",
    "ALTER TABLE invoice_test_component ALTER COLUMN invoice_number SET DEFAULT ''",
    "ALTER TABLE invoice_test_component ALTER COLUMN amount SET DEFAULT 0"
};

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    foreach (var sql in commands)
    {
        try
        {
            Console.WriteLine($"Executing: {sql.Substring(0, Math.Min(50, sql.Length))}...");
            using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("✓ Success");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }
    
    Console.WriteLine("\nDatabase fixes completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"Connection error: {ex.Message}");
}