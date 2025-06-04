#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.3"

using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

Console.WriteLine("Adding missing column to work_order_test_component table in default test database...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    var sql = "ALTER TABLE work_order_test_component ADD COLUMN IF NOT EXISTS is_pre_payment_required BOOLEAN NOT NULL DEFAULT FALSE";
    
    using var cmd = new NpgsqlCommand(sql, connection);
    await cmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("Column added successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}