# Setting Up Jarvis UI with Real API Integration

This guide explains how to set up the Jarvis UI to work with the real API instead of mock data.

## Prerequisites

1. PostgreSQL database running locally
2. .NET 8 SDK installed
3. Azure Functions Core Tools v4 installed
4. Node.js 18+ installed

## Step 1: Database Setup

First, ensure your PostgreSQL database is set up with the necessary tables and test user.

### Option A: Using the main setup script (creates all tables)
```powershell
# From the root jarvis directory
powershell.exe -Command ".\setup-jarvis-db.ps1 -Database jarvis_test -CreateTestUser"
```

### Option B: Just create the test user (if tables already exist)
```powershell
# From the UI directory
cd core.jarvis.ui.studio
powershell.exe -Command ".\scripts\setup-test-user.ps1"
```

This creates a test user with:
- Email: `test@example.com`
- Password: `TestPassword123!`

## Step 2: Configure the API

1. Navigate to the API directory:
```powershell
cd core.jarvis.api
```

2. Create or update `local.settings.json`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Jwt:SecretKey": "your-very-secure-256-bit-secret-key-for-jwt-signing",
    "Jwt:Issuer": "jarvis-api",
    "Jwt:Audience": "jarvis-clients",
    "Jwt:AccessTokenExpiry": "60",
    "Jwt:RefreshTokenExpiry": "10080"
  },
  "ConnectionStrings": {
    "JarvisDb": "Host=localhost;Database=jarvis_test;Username=postgres;Password=your-password"
  }
}
```

3. Start the API:
```powershell
func start
```

The API will be available at `http://localhost:7071/api/`

## Step 3: Configure the UI

1. Navigate to the UI directory:
```powershell
cd core.jarvis.ui.studio
```

2. The `.env` file has already been created with:
```env
VITE_USE_MOCK_API=false
VITE_API_URL=http://localhost:7071/api
```

3. Install dependencies:
```powershell
npm install
```

4. Start the development server:
```powershell
npm run dev
```

The UI will be available at `http://localhost:5173/`

## Step 4: Test the Integration

1. Open the UI in your browser: `http://localhost:5173/`
2. Click on "Sign In" 
3. Login with:
   - Email: `test@example.com`
   - Password: `TestPassword123!`

## API Endpoints

The real API provides these endpoints:

### Authentication
- `POST /api/security/auth` - Login
- `POST /api/security/deauth` - Logout
- `POST /api/security/refresh` - Refresh tokens
- `POST /api/security/validate` - Validate token

### Registration
- `POST /api/auth/register` - Register new user

### Navigation
- `GET /api/security/navigation` - Get navigation items

### Roles & Permissions
- `GET /api/security/roles` - Get roles
- `GET /api/security/account` - Get account info

## Troubleshooting

### API Connection Issues

1. **Check API is running:**
   - Look for `Http Functions:` in the console output
   - Try accessing `http://localhost:7071/api/swagger/ui`

2. **CORS errors:**
   - The API should have CORS enabled for localhost
   - Check browser console for specific CORS errors

3. **Database connection:**
   - Verify PostgreSQL is running
   - Check connection string in `local.settings.json`
   - Ensure database `jarvis_test` exists

### Authentication Issues

1. **Login fails with "Invalid credentials":**
   - Run the test user setup script again
   - Check the password hash in the database
   - Verify the API can connect to the database

2. **Token issues:**
   - Check JWT configuration in `local.settings.json`
   - Ensure the secret key is at least 256 bits

## Switching Between Mock and Real API

To switch back to mock mode:
1. Edit `.env` file
2. Set `VITE_USE_MOCK_API=true`
3. Restart the development server

Or use the API mode toggle in the UI (available in the user dropdown menu).

## API Response Format

The API returns responses in PascalCase format:
```json
{
  "AccessToken": "jwt-token-here",
  "RefreshToken": "refresh-token-here",
  "OwnerEntityId": "user-entity-id",
  "ExpiresAt": "2024-01-01T00:00:00Z"
}
```

The UI automatically handles the case conversion.

## Security Notes

- The test user password (`TestPassword123!`) is for development only
- In production, use strong passwords and proper security practices
- The JWT secret key should be stored securely
- Enable HTTPS in production environments