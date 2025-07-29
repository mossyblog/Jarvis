#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.5"

using Npgsql;
using System;
using System.Threading.Tasks;

var connectionString = "Host=127.0.0.1;Port=5432;Database=jarvis_test;Username=supabase_admin;Password=postgres";

try
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    
    Console.WriteLine("Connected to database successfully!");
    
    // The SQL to create the test user
    var sql = @"
DO $$
DECLARE
    test_entity_id UUID := '11111111-1111-1111-1111-111111111111';
    test_email VARCHAR(255) := 'test@example.com';
    test_password VARCHAR(255) := 'TestPassword123!';
    password_hash VARCHAR(255);
BEGIN
    -- Use the provided bcrypt hash for 'TestPassword123!'
    password_hash := '$2a$12$QnOKnn.PumrtVscPkO3C.ONHR/5NANzEbqMoLQOyUFhQMhynyVoe.';
    
    -- Check if user already exists
    IF EXISTS (SELECT 1 FROM account_component WHERE email = test_email) THEN
        RAISE NOTICE 'User % already exists', test_email;
        RETURN;
    END IF;
    
    -- Create account component
    INSERT INTO account_component (
        id,
        owner_entity_id,
        email,
        password_hash,
        password,
        auth_method,
        is_active,
        created_at,
        last_updated
    ) VALUES (
        gen_random_uuid(),
        test_entity_id,
        test_email,
        password_hash,
        '', -- Password field is not used when password_hash is set
        'password',
        true,
        NOW(),
        NOW()
    );
    
    -- Create security profile component for the user
    INSERT INTO security_profile_component (
        id,
        owner_entity_id,
        name,
        avatar,
        role_ids,
        permission_ids,
        preferences,
        created_at,
        last_updated
    ) VALUES (
        gen_random_uuid(),
        test_entity_id,
        'Test User',
        NULL,
        '{}', -- Empty role IDs for now
        '{}', -- Empty permission IDs for now
        '{""theme"": ""dark"", ""sidebarBehavior"": ""open""}',
        NOW(),
        NOW()
    );
    
    -- Create admin role if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM role_component WHERE name = 'Administrator') THEN
        INSERT INTO role_component (
            id,
            owner_entity_id,
            name,
            description,
            permission_ids,
            created_at,
            last_updated
        ) VALUES (
            gen_random_uuid(),
            gen_random_uuid(), -- Role has its own entity
            'Administrator',
            'Full system access',
            '{}', -- Permissions would be added separately
            NOW(),
            NOW()
        );
    END IF;
    
    -- Add basic navigation items if they don't exist
    INSERT INTO navigation_item (owner_entity_id, title, path, icon, sort_order, is_active)
    VALUES 
        (gen_random_uuid(), 'Home', '/', 'Home', 1, true),
        (gen_random_uuid(), 'Table Editor', '/editor', 'Table', 2, true),
        (gen_random_uuid(), 'Schema Visualizer', '/SchemaVisualizer', 'Database', 3, true),
        (gen_random_uuid(), 'SQL Editor', '/sql', 'FileCode2', 4, true),
        (gen_random_uuid(), 'Database', '/database', 'Database', 5, true),
        (gen_random_uuid(), 'Authentication', '/auth', 'Shield', 6, true),
        (gen_random_uuid(), 'Storage', '/storage', 'HardDrive', 7, true),
        (gen_random_uuid(), 'Edge Functions', '/functions', 'FileText', 8, true),
        (gen_random_uuid(), 'Realtime', '/realtime', 'Radio', 9, true),
        (gen_random_uuid(), 'API Docs', '/api', 'FileCode2', 10, true),
        (gen_random_uuid(), 'Settings', '/settings', 'Settings', 11, true)
    ON CONFLICT (owner_entity_id) DO NOTHING;
    
    RAISE NOTICE 'Test user created successfully: % with password: %', test_email, test_password;
    RAISE NOTICE 'Entity ID: %', test_entity_id;
END $$;
";

    using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
    
    Console.WriteLine("Test user creation script executed successfully!");
    
    // Verify the user was created
    var verifyCmd = new NpgsqlCommand(@"
        SELECT 
            ac.owner_entity_id,
            ac.email,
            ac.is_active,
            sp.name,
            sp.preferences
        FROM account_component ac
        LEFT JOIN security_profile_component sp ON ac.owner_entity_id = sp.owner_entity_id
        WHERE ac.email = 'test@example.com'", conn);
    
    using var reader = await verifyCmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        Console.WriteLine("\nUser created successfully:");
        Console.WriteLine($"Entity ID: {reader["owner_entity_id"]}");
        Console.WriteLine($"Email: {reader["email"]}");
        Console.WriteLine($"Is Active: {reader["is_active"]}");
        Console.WriteLine($"Name: {reader["name"]}");
        Console.WriteLine($"Preferences: {reader["preferences"]}");
        Console.WriteLine("\nLogin credentials:");
        Console.WriteLine("Email: test@example.com");
        Console.WriteLine("Password: TestPassword123!");
    }
    else
    {
        Console.WriteLine("Warning: User was not found after creation attempt.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
    }
}
