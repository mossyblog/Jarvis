#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"
#r "nuget: Microsoft.Extensions.DependencyInjection, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 8.0.0"

#load "core.jarvis/Data/IPgClient.cs"
#load "core.jarvis/Data/PgClientWrapper.cs"
#load "core.jarvis/Data/GraphQL/IGraphQLQuery.cs"
#load "core.jarvis/Data/GraphQL/GraphQLQueryBuilder.cs"
#load "core.jarvis/Data/GraphQL/GraphQLHandler.cs"
#load "core.jarvis/Data/GraphQL/IGraphQLHandler.cs"

using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using core.jarvis.Data;
using core.jarvis.Data.GraphQL;
using System.Text.Json;

Console.WriteLine("Testing GraphQL with minimal setup...");

// This won't work due to complex dependencies, but let me try a different approach
// Let me just check what happens when we authenticate first

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // First, let's check if the test user exists and can authenticate
    var checkUserSql = "SELECT COUNT(*) FROM users WHERE email = 'test@example.com'";
    using var checkCmd = new NpgsqlCommand(checkUserSql, connection);
    var userCount = (long)(await checkCmd.ExecuteScalarAsync() ?? 0);
    Console.WriteLine($"Test user exists: {userCount > 0}");
    
    // Now test GraphQL with different queries to see what works
    var testQueries = new Dictionary<string, string> {
        ["Simple introspection"] = "{ __typename }",
        ["With operation name"] = "query TestOperation { __typename }",
        ["Schema introspection"] = "{ __schema { queryType { name } } }",
        ["Invalid field"] = "{ invalidField }",
        ["Type introspection"] = "{ __type(name: \"Query\") { name kind } }"
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
                Console.WriteLine($"Response: {JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true })}");
            }
            else
            {
                Console.WriteLine("Empty response");
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