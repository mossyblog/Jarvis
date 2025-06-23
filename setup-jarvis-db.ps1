#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sets up the Jarvis framework database with all required tables.

.DESCRIPTION
    This script creates the Jarvis database and all necessary tables for the
    Entity Component System (ECS) framework including authentication, audit,
    and core component tables.

.PARAMETER Server
    PostgreSQL server hostname (default: localhost)

.PARAMETER Port
    PostgreSQL server port (default: 5432)

.PARAMETER Username
    PostgreSQL username (default: postgres)

.PARAMETER Password
    PostgreSQL password (will prompt if not provided)

.PARAMETER Database
    Target database name (default: jarvis)

.PARAMETER CreateTestUser
    If specified, creates a test user for development

.EXAMPLE
    .\setup-jarvis-db.ps1 -Server localhost -Username postgres
    
.EXAMPLE
    .\setup-jarvis-db.ps1 -Server db.example.com -Port 5433 -Database jarvis_prod
#>

param(
    [string]$Server = "localhost",
    [int]$Port = 5432,
    [string]$Username = "postgres",
    [SecureString]$Password,
    [string]$Database = "jarvis_test",
    [switch]$CreateTestUser
)

# Check if psql is available
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Error "PostgreSQL client (psql) is not installed or not in PATH"
    Write-Host "Please install PostgreSQL client tools and ensure psql is in your PATH"
    exit 1
}

# Get password if not provided
if (-not $Password) {
    $Password = Read-Host "Enter PostgreSQL password for user '$Username'" -AsSecureString
}

# Convert SecureString to plain text (required for psql)
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
$PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Set PGPASSWORD environment variable for psql
$env:PGPASSWORD = $PlainPassword

Write-Host "Setting up Jarvis database..." -ForegroundColor Green

# Function to execute SQL
function Invoke-PostgreSQL {
    param(
        [string]$Query,
        [string]$TargetDatabase = "postgres",
        [switch]$SuppressError
    )
    
    $arguments = @(
        "-h", $Server,
        "-p", $Port,
        "-U", $Username,
        "-d", $TargetDatabase,
        "-c", $Query
    )
    
    if ($SuppressError) {
        $result = & psql @arguments 2>$null
    } else {
        $result = & psql @arguments
    }
    
    return $LASTEXITCODE -eq 0
}

# Check if database exists
Write-Host "Checking if database '$Database' exists..." -ForegroundColor Yellow
$dbExists = Invoke-PostgreSQL -Query "SELECT 1 FROM pg_database WHERE datname = '$Database'" -SuppressError

if (-not $dbExists) {
    Write-Host "Creating database '$Database'..." -ForegroundColor Yellow
    $created = Invoke-PostgreSQL -Query "CREATE DATABASE $Database"
    if ($created) {
        Write-Host "Database '$Database' created successfully" -ForegroundColor Green
    } else {
        Write-Error "Failed to create database '$Database'"
        exit 1
    }
} else {
    Write-Host "Database '$Database' already exists" -ForegroundColor Green
}

# SQL script for creating tables
$setupSQL = @"
-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create users table (required for authentication)
CREATE TABLE IF NOT EXISTS users (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Enable Row Level Security on users table
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Create index on email for faster lookups
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);

-- Create security_token table for JWT refresh tokens
CREATE TABLE IF NOT EXISTS security_token (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    user_id UUID NOT NULL REFERENCES users(id),
    session_id UUID NOT NULL,
    refresh_token_hash TEXT NOT NULL,
    issued_at TIMESTAMPTZ NOT NULL,
    refresh_expires_at TIMESTAMPTZ NOT NULL,
    client_id TEXT,
    ip_address TEXT,
    user_agent TEXT,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMPTZ,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for security_token
CREATE INDEX IF NOT EXISTS idx_security_token_owner_entity_id ON security_token(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_security_token_user_id ON security_token(user_id);
CREATE INDEX IF NOT EXISTS idx_security_token_session_id ON security_token(session_id);
CREATE INDEX IF NOT EXISTS idx_security_token_refresh_expires_at ON security_token(refresh_expires_at) WHERE is_revoked = FALSE;

-- Create audit_event table for comprehensive audit logging
CREATE TABLE IF NOT EXISTS audit_event (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    user_id VARCHAR(255),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    old_value TEXT,
    new_value TEXT,
    metadata JSONB,
    transaction_id VARCHAR(100),
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for audit_event
CREATE INDEX IF NOT EXISTS idx_audit_event_owner_entity_id ON audit_event(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_event_entity_type ON audit_event(entity_type);
CREATE INDEX IF NOT EXISTS idx_audit_event_event_type ON audit_event(event_type);
CREATE INDEX IF NOT EXISTS idx_audit_event_timestamp ON audit_event(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_event_user_id ON audit_event(user_id);

-- Create component_snapshots table for versioning support
CREATE TABLE IF NOT EXISTS component_snapshots (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id UUID NOT NULL,
    component_type VARCHAR(255) NOT NULL,
    component_id UUID NOT NULL UNIQUE,
    snapshots JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for component_snapshots
CREATE INDEX IF NOT EXISTS idx_component_snapshots_entity_id ON component_snapshots(entity_id);
CREATE INDEX IF NOT EXISTS idx_component_snapshots_component_type ON component_snapshots(component_type);

-- Create entity_relationship table for parent-child relationships
CREATE TABLE IF NOT EXISTS entity_relationship (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    owner_entity_id UUID NOT NULL UNIQUE,
    parent_id UUID,
    children_ids UUID[] DEFAULT '{}',
    parent_type TEXT,
    child_types JSONB DEFAULT '{}',
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for entity_relationship
CREATE INDEX IF NOT EXISTS idx_entity_relationship_owner_entity_id ON entity_relationship(owner_entity_id);
CREATE INDEX IF NOT EXISTS idx_entity_relationship_parent_id ON entity_relationship(parent_id);

-- Create update timestamp trigger function
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS ${'$'}${'$'}
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
${'$'}${'$'} LANGUAGE plpgsql;

-- Apply update timestamp triggers to all tables
DO ${'$'}${'$'}
DECLARE
    t text;
BEGIN
    FOR t IN 
        SELECT table_name 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND column_name = 'updated_at'
    LOOP
        EXECUTE format('
            CREATE TRIGGER update_%I_updated_at 
            BEFORE UPDATE ON %I 
            FOR EACH ROW 
            EXECUTE FUNCTION update_updated_at()', t, t);
    END LOOP;
END ${'$'}${'$'};

-- Grant permissions (adjust as needed for your environment)
GRANT ALL ON ALL TABLES IN SCHEMA public TO $Username;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO $Username;
"@

# Execute the setup SQL
Write-Host "Creating tables in database '$Database'..." -ForegroundColor Yellow
$tablesCreated = Invoke-PostgreSQL -Query $setupSQL -TargetDatabase $Database

if ($tablesCreated) {
    Write-Host "All tables created successfully" -ForegroundColor Green
} else {
    Write-Error "Failed to create tables"
    exit 1
}

# Create test user if requested
if ($CreateTestUser) {
    Write-Host "Creating test user..." -ForegroundColor Yellow
    
    # Hash the password using bcrypt (simplified for example - in production use proper hashing)
    $testUserSQL = @"
INSERT INTO users (email, password_hash) 
VALUES ('test@example.com', crypt('test123', gen_salt('bf')))
ON CONFLICT (email) DO NOTHING;
"@
    
    $userCreated = Invoke-PostgreSQL -Query $testUserSQL -TargetDatabase $Database
    
    if ($userCreated) {
        Write-Host "Test user created (test@example.com / test123)" -ForegroundColor Green
    } else {
        Write-Warning "Failed to create test user (may already exist)"
    }
}

# Verify tables
Write-Host "`nVerifying created tables..." -ForegroundColor Yellow
$verifySQL = @"
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_type = 'BASE TABLE'
ORDER BY table_name;
"@

$tables = & psql -h $Server -p $Port -U $Username -d $Database -t -c $verifySQL

Write-Host "`nCreated tables:" -ForegroundColor Green
$tables | ForEach-Object { 
    if ($_.Trim()) {
        Write-Host "  - $($_.Trim())" -ForegroundColor Cyan
    }
}

# Clean up
$env:PGPASSWORD = ""

Write-Host "`nJarvis database setup completed successfully!" -ForegroundColor Green
Write-Host "Connection string: " -NoNewline
Write-Host "Host=$Server;Port=$Port;Database=$Database;Username=$Username;Password=<your-password>" -ForegroundColor Yellow