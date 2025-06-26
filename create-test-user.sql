-- Create a test user for local development
-- Password: TestPassword123!
-- Email: test@example.com

-- First, let's check if the user table exists and its structure
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'user' 
ORDER BY ordinal_position;

-- Insert a test user with a hashed password
-- The password hash is for "TestPassword123!" using BCrypt
INSERT INTO "user" (
    id,
    owner_entity_id,
    email,
    password_hash,
    auth_method,
    is_active,
    created_at,
    updated_at
) VALUES (
    gen_random_uuid(),
    gen_random_uuid(),
    'test@example.com',
    '$2a$10$8K1p/a0dL1LXMIgoEDFrwOfMQbLgtnOoKsWc77IZUt4Ejl3/dhF2E', -- BCrypt hash of TestPassword123!
    'password',
    true,
    NOW(),
    NOW()
) ON CONFLICT (email) DO NOTHING;

-- Verify the user was created
SELECT id, owner_entity_id, email, is_active FROM "user" WHERE email = 'test@example.com';