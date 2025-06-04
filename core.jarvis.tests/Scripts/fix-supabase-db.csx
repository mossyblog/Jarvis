#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

// Try both databases
var databases = new Dictionary<string, string> {
    ["Default Test DB"] = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres",
    ["Supabase DB"] = "Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres"
};

foreach (var db in databases)
{
    Console.WriteLine($"\n=== Fixing {db.Key} ===");
    Console.WriteLine($"Connection: {db.Value}");
    
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
        
        // Work order columns
        "ALTER TABLE work_order_test_component ADD COLUMN IF NOT EXISTS is_pre_payment_required BOOLEAN NOT NULL DEFAULT FALSE",
        "ALTER TABLE work_order_test_component ADD COLUMN IF NOT EXISTS work_order_id VARCHAR(255) NOT NULL DEFAULT ''",
        "ALTER TABLE work_order_test_component ADD COLUMN IF NOT EXISTS work_order_number VARCHAR(255) NOT NULL DEFAULT ''",
        
        // Invoice columns
        "ALTER TABLE invoice_test_component ADD COLUMN IF NOT EXISTS work_order_id UUID",
        "ALTER TABLE invoice_test_component ADD COLUMN IF NOT EXISTS invoice_number VARCHAR(255) NOT NULL DEFAULT ''",
        "ALTER TABLE invoice_test_component ADD COLUMN IF NOT EXISTS amount INTEGER NOT NULL DEFAULT 0"
    };
    
    try
    {
        using var connection = new NpgsqlConnection(db.Value);
        await connection.OpenAsync();
        
        foreach (var sql in commands)
        {
            try
            {
                Console.WriteLine($"  Executing: {sql.Substring(0, Math.Min(60, sql.Length))}...");
                using var cmd = new NpgsqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"  ✓ Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Connection error: {ex.Message}");
    }
}

Console.WriteLine("\nAll database fixes completed!");