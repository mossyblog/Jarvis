#r "nuget: BCrypt.Net-Next, 4.0.3"
#r "nuget: Npgsql, 8.0.3"
#r "nuget: Dapper, 2.1.35"

using BCrypt.Net;
using Npgsql;
using Dapper;
using System;
using System.Threading.Tasks;

// Run immediately
await CreateTestUser();

async Task CreateTestUser()
{
    // Connection string - adjust if needed
    var connectionString = "Host=localhost;Port=5432;Database=jarvis_test;Username=postgres;Password=postgres";

    // User details
    var email = "test@example.com";
    var password = "TestPassword123!";

    // Hash the password using BCrypt with the same settings as the app (cost factor 12)
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 12);

    Console.WriteLine($"Creating account with email: {email}");
    Console.WriteLine($"Password hash: {hashedPassword}");

    using var connection = new NpgsqlConnection(connectionString);
    connection.Open();

    // Create account_component table if it doesn't exist
    var createTableSql = @"
        CREATE TABLE IF NOT EXISTS account_component (
            id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
            owner_entity_id UUID NOT NULL,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
            email VARCHAR(255) NOT NULL UNIQUE,
            password_hash VARCHAR(255) NOT NULL,
            password VARCHAR(255) DEFAULT '',
            two_factor_code VARCHAR(255),
            auth_method VARCHAR(50) NOT NULL DEFAULT 'password',
            client_id VARCHAR(255),
            is_active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
            ip_address VARCHAR(45),
            user_agent TEXT
        )";
    
    await connection.ExecuteAsync(createTableSql);
    Console.WriteLine("Ensured account_component table exists");

    // Delete existing account first
    var deleteSql = @"DELETE FROM account_component WHERE email = @email";
    await connection.ExecuteAsync(deleteSql, new { email });
    Console.WriteLine("Deleted existing account (if any)");

    // Generate a fixed UUID for the test account
    var ownerEntityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var accountId = Guid.NewGuid();
    
    // Insert new account
    var insertSql = @"
        INSERT INTO account_component (
            id, owner_entity_id, email, password_hash, password, auth_method, is_active, created_at, updated_at
        ) VALUES (
            @accountId, @ownerEntityId, @email, @passwordHash, '', 'password', true, NOW(), NOW()
        )";
    
    await connection.ExecuteAsync(insertSql, new { accountId, ownerEntityId, email, passwordHash = hashedPassword });
    Console.WriteLine("Created new account");

    // Create security_profile_component for the user
    var createProfileTableSql = @"
        CREATE TABLE IF NOT EXISTS security_profile_component (
            id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
            owner_entity_id UUID NOT NULL,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
            name VARCHAR(255) NOT NULL,
            avatar VARCHAR(255),
            role_ids TEXT[] DEFAULT '{}',
            permission_ids TEXT[] DEFAULT '{}',
            preferences JSONB DEFAULT '{}'
        )";
    
    await connection.ExecuteAsync(createProfileTableSql);
    
    // Delete existing profile first
    await connection.ExecuteAsync("DELETE FROM security_profile_component WHERE owner_entity_id = @ownerEntityId", 
        new { ownerEntityId });
    
    // Insert security profile
    var insertProfileSql = @"
        INSERT INTO security_profile_component (
            id, owner_entity_id, name, avatar, role_ids, permission_ids, preferences
        ) VALUES (
            gen_random_uuid(), @ownerEntityId, @name, NULL, '{}', '{}', '{}'
        )";
    
    await connection.ExecuteAsync(insertProfileSql, new { ownerEntityId, name = "Test User" });
    Console.WriteLine("Created security profile");

    // Verify the account was created
    var verifyAccount = await connection.QueryFirstOrDefaultAsync<dynamic>(
        @"SELECT owner_entity_id, email, password_hash, is_active FROM account_component WHERE email = @email",
        new { email }
    );
    
    if (verifyAccount != null)
    {
        Console.WriteLine($"Verified account created:");
        Console.WriteLine($"  Owner Entity ID: {verifyAccount.owner_entity_id}");
        Console.WriteLine($"  Email: {verifyAccount.email}");
        Console.WriteLine($"  Is Active: {verifyAccount.is_active}");
        Console.WriteLine($"  Password Hash Length: {verifyAccount.password_hash.Length}");
    }

    Console.WriteLine($"\nAccount {email} is ready to login with password: {password}");
}