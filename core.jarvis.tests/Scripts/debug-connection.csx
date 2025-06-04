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
    
    // List all tables
    var sql = @"
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_type = 'BASE TABLE'
ORDER BY table_name";
    
    using var cmd = new NpgsqlCommand(sql, connection);
    using var reader = await cmd.ExecuteReaderAsync();
    
    Console.WriteLine("\nTables in database:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader.GetString(0)}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}