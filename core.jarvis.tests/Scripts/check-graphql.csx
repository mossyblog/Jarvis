#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Checking pg_graphql extension...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // Check if extension exists
    var sql = @"
SELECT EXISTS (
    SELECT 1 
    FROM pg_extension 
    WHERE extname = 'pg_graphql'
)";
    
    using var cmd = new NpgsqlCommand(sql, connection);
    var exists = (bool)(await cmd.ExecuteScalarAsync() ?? false);
    
    Console.WriteLine($"pg_graphql extension exists: {exists}");
    
    if (!exists)
    {
        Console.WriteLine("\nTrying to create pg_graphql extension...");
        try
        {
            using var createCmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS pg_graphql CASCADE", connection);
            await createCmd.ExecuteNonQueryAsync();
            Console.WriteLine("Extension created successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create extension: {ex.Message}");
        }
    }
    
    // Check if graphql schema exists
    var schemaCmd = new NpgsqlCommand(@"
SELECT EXISTS (
    SELECT 1 
    FROM information_schema.schemata 
    WHERE schema_name = 'graphql'
)", connection);
    
    var schemaExists = (bool)(await schemaCmd.ExecuteScalarAsync() ?? false);
    Console.WriteLine($"graphql schema exists: {schemaExists}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}