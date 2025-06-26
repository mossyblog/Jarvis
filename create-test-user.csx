#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"
#r "nuget: Npgsql, 8.0.3"
#r "nuget: Dapper, 2.1.35"

using BCrypt.Net;
using Npgsql;
using Dapper;
using System;

// Connection string - adjust if needed
var connectionString = "Host=localhost;Port=5432;Database=jarvis;Username=postgres;Password=postgres";

// User details
var email = "test@example.com";
var password = "test123";

// Hash the password using BCrypt with the same settings as the app
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 12); // cost factor 12

Console.WriteLine($"Creating user with email: {email}");
Console.WriteLine($"Password hash: {hashedPassword}");

using var connection = new NpgsqlConnection(connectionString);
connection.Open();

// Check if user already exists
var existingUser = await connection.QueryFirstOrDefaultAsync<dynamic>(
    "SELECT email FROM \"user\" WHERE email = @email",
    new { email }
);

if (existingUser != null)
{
    // Update existing user
    var updateSql = @"
        UPDATE ""user"" 
        SET password_hash = @passwordHash, 
            is_active = true, 
            updated_at = NOW()
        WHERE email = @email";
    
    await connection.ExecuteAsync(updateSql, new { email, passwordHash = hashedPassword });
    Console.WriteLine("Updated existing user");
}
else
{
    // Insert new user
    var insertSql = @"
        INSERT INTO ""user"" (
            id, owner_entity_id, email, password_hash, password,
            auth_method, is_active, created_at, updated_at
        ) VALUES (
            gen_random_uuid(), gen_random_uuid(), @email, @passwordHash, '',
            'password', true, NOW(), NOW()
        )";
    
    await connection.ExecuteAsync(insertSql, new { email, passwordHash = hashedPassword });
    Console.WriteLine("Created new user");
}

Console.WriteLine($"User {email} is ready to login with password: {password}");