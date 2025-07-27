# Troubleshooting Guide

This guide helps you resolve common issues when working with Jarvis ECS.

## Common Issues

### Installation Issues

#### .NET SDK Not Found
**Problem**: `dotnet` command not recognized.

**Solution**:
1. Ensure .NET SDK 8.0+ is installed: https://dotnet.microsoft.com/download
2. Restart your terminal/command prompt
3. Verify with: `dotnet --version`

#### Git Not Found
**Problem**: `git` command not recognized.

**Solution**:
1. Install Git: https://git-scm.com/downloads
2. Restart your terminal
3. Verify with: `git --version`

### Database Connection Issues

#### PostgreSQL Connection Failed
**Problem**: Cannot connect to PostgreSQL database.

**Solution**:
1. Ensure Docker Desktop is running
2. Check if PostgreSQL container is running: `docker ps`
3. Verify connection string in your environment variables
4. Default connection: `postgresql://postgres:postgres@localhost:5432/jarvis`

### Build Errors

#### Package Restore Failed
**Problem**: NuGet packages fail to restore.

**Solution**:
```bash
dotnet clean
dotnet restore
dotnet build
```

#### Missing Dependencies
**Problem**: Compiler errors about missing types.

**Solution**:
1. Ensure all required packages are in `.csproj` files
2. Run: `dotnet restore`
3. Check for typos in using statements

### Runtime Errors

#### JWT Secret Key Not Found
**Problem**: `Jwt__SecretKey not configured` error.

**Solution**:
Set environment variable:
```bash
# Windows
set Jwt__SecretKey=your-secret-key-at-least-32-characters-long

# Linux/Mac
export Jwt__SecretKey=your-secret-key-at-least-32-characters-long
```

#### Table Does Not Exist
**Problem**: Database table not found errors.

**Solution**:
1. Run migrations: `dotnet ef database update`
2. Or let Jarvis auto-create tables (if enabled)
3. Check your connection has CREATE permissions

### Testing Issues

#### Tests Failing with Database Errors
**Problem**: Integration tests fail with connection errors.

**Solution**:
1. Ensure test database is accessible
2. Check `TEST_DATABASE_URL` environment variable
3. Run: `docker-compose up -d postgres-test`

## Getting Help

If you're still stuck:
1. Check the [API documentation](../api-reference/core-interfaces.md)
2. Review the [Getting Started guide](./quickstart.md)
3. Ask your team lead or mentor
4. Create an issue in the project repository

## Debug Tips

### Enable Detailed Logging
Add to your `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "core.jarvis": "Trace"
    }
  }
}
```

### Check Environment Variables
List all Jarvis-related variables:
```bash
# Windows
set | findstr Jarvis
set | findstr Jwt

# Linux/Mac
env | grep -i jarvis
env | grep -i jwt
```

### Database Schema Issues
Check existing tables:
```sql
-- Connect to your database
\dt

-- Show table structure
\d your_table_name
```

Remember: Most issues are configuration-related. Double-check your environment variables and connection strings first!