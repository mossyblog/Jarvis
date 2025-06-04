#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Checking columns in invoice_test_component table...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    var sql = @"
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'invoice_test_component' 
AND table_schema = 'public'
ORDER BY ordinal_position";
    
    using var cmd = new NpgsqlCommand(sql, connection);
    using var reader = await cmd.ExecuteReaderAsync();
    
    Console.WriteLine("\nColumns in invoice_test_component:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader.GetString(0)} ({reader.GetString(1)}) - Nullable: {reader.GetString(2)}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}