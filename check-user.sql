-- Check if the test user exists
SELECT 
    id,
    owner_entity_id,
    email,
    password_hash,
    is_active,
    auth_method
FROM "user" 
WHERE email = 'test@example.com';

-- Check all users
SELECT 
    email,
    is_active,
    created_at
FROM "user";