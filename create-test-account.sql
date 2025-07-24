-- Delete existing test account if any
DELETE FROM "account" WHERE email = 'test@example.com';

-- Create test account with proper BCrypt hash for 'TestPassword123!'
-- This hash was generated with BCrypt cost factor 10
INSERT INTO "account" (
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
    '11111111-1111-1111-1111-111111111111', 
    'test@example.com', 
    '$2a$10$8K1p/a0dL1LXMIgoEDFrwOfMQbLgtnOoKsWc77IZUt4Ejl3/dhF2E', -- TestPassword123!
    '', 
    'password', 
    true, 
    NOW(), 
    NOW()
);

-- Verify the account was created
SELECT 
    owner_entity_id,
    email, 
    password_hash,
    is_active
FROM "account" 
WHERE email = 'test@example.com';