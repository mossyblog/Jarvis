#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;
using System.Text.Json;

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Testing GraphQL introspection query...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // Test the exact query used in tests
    var query = "{ __typename }";
    
    using var cmd = new NpgsqlCommand("SELECT graphql.resolve($1::text)", connection);
    cmd.Parameters.AddWithValue(query);
    
    var result = await cmd.ExecuteScalarAsync() as string;
    
    Console.WriteLine($"Raw result: {result}");
    
    if (!string.IsNullOrEmpty(result))
    {
        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        Console.WriteLine($"\nParsed result: {JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true })}");
        
        // Check if data property exists
        if (parsed.TryGetProperty("data", out var data))
        {
            Console.WriteLine($"\nData exists: {data}");
        }
        else
        {
            Console.WriteLine("\nNo 'data' property found in response");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
}