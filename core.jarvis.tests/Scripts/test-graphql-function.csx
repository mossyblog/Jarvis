#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;
using System.Text.Json;

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Testing GraphQL function...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // Test simple introspection query
    var query = JsonSerializer.Serialize(new { query = "{ __typename }" });
    
    using var cmd = new NpgsqlCommand("SELECT graphql.resolve($1::text)", connection);
    cmd.Parameters.AddWithValue(query);
    
    var result = await cmd.ExecuteScalarAsync() as string;
    
    Console.WriteLine($"GraphQL result: {result}");
    
    if (!string.IsNullOrEmpty(result))
    {
        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        Console.WriteLine($"Parsed result: {JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true })}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
}