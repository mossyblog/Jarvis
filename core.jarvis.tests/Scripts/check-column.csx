#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

async Task CheckColumn(string connectionString, string name)
{
    Console.WriteLine($"\nChecking {name}: {connectionString}");
    try
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var sql = @"
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'work_order_test_component' 
AND table_schema = 'public'
ORDER BY column_name";
        
        using var cmd = new NpgsqlCommand(sql, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        
        Console.WriteLine("Columns in work_order_test_component:");
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"  - {reader.GetString(0)}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

// Check both databases
await CheckColumn("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres", "Default Test DB");
await CheckColumn("Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres", "Supabase DB");