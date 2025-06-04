#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;
using System.Text.Json;

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Testing GraphQL queries...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // Test queries that the tests are using
    var testQueries = new Dictionary<string, string> {
        ["Simple __typename"] = "{ __typename }",
        ["With operation name"] = "query TestOperation { __typename }",
        ["Type query"] = "{ __type(name: \"Query\") { name kind } }",
        ["Invalid field test"] = "{ invalidField }",
        ["With variables placeholder"] = "query($testVar: String) { __typename }"
    };
    
    foreach (var test in testQueries)
    {
        Console.WriteLine($"\n=== {test.Key} ===");
        Console.WriteLine($"Query: {test.Value}");
        
        try
        {
            using var cmd = new NpgsqlCommand("SELECT graphql.resolve($1::text)", connection);
            cmd.Parameters.AddWithValue(test.Value);
            
            var result = await cmd.ExecuteScalarAsync() as string;
            
            if (!string.IsNullOrEmpty(result))
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(result);
                var hasData = parsed.TryGetProperty("data", out var data);
                var isDataNull = hasData && data.ValueKind == JsonValueKind.Null;
                var hasErrors = parsed.TryGetProperty("errors", out var errors);
                
                Console.WriteLine($"Has data: {hasData}, Data is null: {isDataNull}, Has errors: {hasErrors}");
                
                if (hasData && !isDataNull)
                {
                    Console.WriteLine($"Data: {data}");
                }
                if (hasErrors)
                {
                    Console.WriteLine($"Errors: {errors}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Connection error: {ex.Message}");
}