#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

var connectionString = "Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Testing direct SQL query...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // Test the exact query that PgTable would generate
    var sql = @"SELECT 
                    t.id, 
                    t.owner_entity_id, 
                    t.parent_id, 
                    t.children_ids, 
                    t.parent_type, 
                    t.child_types, 
                    t.version, 
                    t.updated_at
                FROM entity_relationship t
                WHERE t.owner_entity_id = @p0
                LIMIT 2";
    
    using var cmd = new NpgsqlCommand(sql, connection);
    cmd.Parameters.AddWithValue("@p0", Guid.NewGuid());
    
    using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine("Query executed successfully!");
    Console.WriteLine($"Has rows: {reader.HasRows}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}