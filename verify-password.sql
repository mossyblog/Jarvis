-- Check if the account exists and show the password hash
SELECT 
    owner_entity_id,
    email, 
    password_hash,
    is_active,
    LENGTH(password_hash) as hash_length
FROM "account" 
WHERE email = 'test@example.com';