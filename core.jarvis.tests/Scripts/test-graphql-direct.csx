#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;
using System.Text.Json;

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Testing GraphQL directly...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // Test queries
    var queries = new[] {
        "{ __typename }",
        "query { __typename }",
        "{ __schema { queryType { name } } }"
    };
    
    foreach (var query in queries)
    {
        Console.WriteLine($"\nTesting query: {query}");
        
        using var cmd = new NpgsqlCommand("SELECT graphql.resolve($1::text)", connection);
        cmd.Parameters.AddWithValue(query);
        
        var result = await cmd.ExecuteScalarAsync() as string;
        Console.WriteLine($"Result: {result}");
        
        if (!string.IsNullOrEmpty(result))
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(result);
            var hasData = parsed.TryGetProperty("data", out var data);
            var hasErrors = parsed.TryGetProperty("errors", out var errors);
            
            Console.WriteLine($"Has data: {hasData}, Has errors: {hasErrors}");
            if (hasData && data.ValueKind != JsonValueKind.Null)
            {
                Console.WriteLine($"Data: {data}");
            }
            if (hasErrors)
            {
                Console.WriteLine($"Errors: {errors}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}