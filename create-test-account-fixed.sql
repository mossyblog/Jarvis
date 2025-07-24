-- Delete existing test account if any
DELETE FROM account_component WHERE email = 'test@example.com';

-- Create test account with proper BCrypt hash for 'test123'
-- This hash was generated with BCrypt cost factor 12
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
    '11111111-1111-1111-1111-111111111111', 
    'test@example.com', 
    '$2a$12$QnOKnn.PumrtVscPkO3C.ONHR/5NANzEbqMoLQOyUFhQMhynyVoe.', -- test123
    '', 
    'password', 
    true, 
    NOW(), 
    NOW()
);

-- Also create the security profile for the test account
DELETE FROM security_profile_component WHERE owner_entity_id = '11111111-1111-1111-1111-111111111111';

INSERT INTO security_profile_component (
    owner_entity_id, 
    name, 
    avatar, 
    role_ids, 
    permission_ids, 
    preferences
) VALUES (
    '11111111-1111-1111-1111-111111111111', 
    'Test User', 
    NULL, 
    '{}', 
    '{}', 
    '{}'
);

-- Verify the account was created
SELECT 
    owner_entity_id,
    email, 
    password_hash,
    is_active
FROM account_component 
WHERE email = 'test@example.com';