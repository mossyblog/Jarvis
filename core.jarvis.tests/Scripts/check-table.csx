#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

var connectionString = "Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Checking if entity_relationship table exists...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    var checkTableSql = @"
        SELECT EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = 'entity_relationship'
        );";
    
    using var checkCmd = new NpgsqlCommand(checkTableSql, connection);
    var exists = (bool)await checkCmd.ExecuteScalarAsync();
    
    Console.WriteLine($"Table exists: {exists}");
    
    if (exists)
    {
        var countSql = "SELECT COUNT(*) FROM entity_relationship";
        using var countCmd = new NpgsqlCommand(countSql, connection);
        var count = (long)await countCmd.ExecuteScalarAsync();
        Console.WriteLine($"Row count: {count}");
        
        // Check column names
        var columnsSql = @"
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'entity_relationship' 
            ORDER BY ordinal_position";
        
        using var colCmd = new NpgsqlCommand(columnsSql, connection);
        using var reader = await colCmd.ExecuteReaderAsync();
        
        Console.WriteLine("\nColumns:");
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"  - {reader.GetString(0)}: {reader.GetString(1)}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}