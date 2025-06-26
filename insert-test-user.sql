-- Insert test user directly into the database
-- Email: test@example.com
-- Password: test123

INSERT INTO "user" (
    id,
    owner_entity_id,
    email,
    password_hash,
    password,
    auth_method,
    is_active,
    created_at,
    updated_at
) VALUES (
    gen_random_uuid(),
    gen_random_uuid(),
    'test@example.com',
    '$2a$12$QnOKnn.PumrtVscPkO3C.ONHR/5NANzEbqMoLQOyUFhQMhynyVoe.', -- BCrypt hash of 'test123'
    '',
    'password',
    true,
    NOW(),
    NOW()
) ON CONFLICT (email) DO UPDATE SET
    password_hash = EXCLUDED.password_hash,
    is_active = true,
    updated_at = NOW();