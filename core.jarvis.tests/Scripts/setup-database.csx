#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;
using System.IO;

var connectionString = "Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres";
var scriptPath = "core.jarvis.tests/Scripts/setup-test-database.sql";

Console.WriteLine("Setting up test database...");

try
{
    var script = File.ReadAllText(scriptPath);
    
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    using var command = new NpgsqlCommand(script, connection);
    await command.ExecuteNonQueryAsync();
    
    Console.WriteLine("Database setup completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error setting up database: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}