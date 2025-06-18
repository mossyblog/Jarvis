# Jarvis API - Azure Functions

## Running Locally

### Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite (for local storage)

### Starting the Functions

1. **Navigate to the API project:**
```bash
cd core.jarvis.api
```

2. **Start the Azure Functions host:**
```bash
func start
```

Or using dotnet:
```bash
dotnet build
func start --verbose
```

The functions will start on `http://localhost:7071/api/`

### Available Endpoints

- **POST** `http://localhost:7071/api/security/auth` - Authenticate user
- **POST** `http://localhost:7071/api/security/deauth` - Logout/revoke session  
- **POST** `http://localhost:7071/api/security/refresh` - Refresh tokens
- **GET** `http://localhost:7071/api/security/validate` - Validate token

### Swagger/OpenAPI Documentation

- **Swagger UI**: `http://localhost:7071/api/swagger/ui`
- **OpenAPI Spec (JSON)**: `http://localhost:7071/api/swagger.json`
- **OpenAPI Spec (YAML)**: `http://localhost:7071/api/swagger.yaml`

The API includes full OpenAPI 3.0 documentation with:
- Interactive Swagger UI for testing endpoints
- Request/response schemas
- Example values
- Authentication requirements
- Error response documentation

## Testing the Endpoints

### 1. Authentication (Login)
```bash
curl -X POST http://localhost:7071/api/security/auth \
  -H "Content-Type: application/json" \
  -d '{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
    "updatedAt": "2024-01-01T00:00:00Z",
    "email": "user@example.com",
    "password": "SecurePassword123!",
    "clientId": "web-app"
  }'
```

### 2. Deauthentication (Logout)
```bash
# Using direct GUID
curl -X POST http://localhost:7071/api/security/deauth \
  -H "Content-Type: application/json" \
  -d '"550e8400-e29b-41d4-a716-446655440000"'

# Or using object format
curl -X POST http://localhost:7071/api/security/deauth \
  -H "Content-Type: application/json" \
  -d '{"sessionId": "550e8400-e29b-41d4-a716-446655440000"}'
```

### 3. Token Refresh
```bash
curl -X POST http://localhost:7071/api/security/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "id": "550e8400-e29b-41d4-a716-446655440002",
    "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
    "updatedAt": "2024-01-01T00:00:00Z",
    "refreshToken": "your-refresh-token-here",
    "clientId": "web-app"
  }'
```

### 4. Token Validation
```bash
curl -X GET http://localhost:7071/api/security/validate \
  -H "X-Token-Id: 550e8400-e29b-41d4-a716-446655440000"
```

## Using REST Client (VS Code)

Create a file `api.http` in your project:

```http
### Variables
@baseUrl = http://localhost:7071/api
@tokenId = 550e8400-e29b-41d4-a716-446655440000

### Auth - Login
POST {{baseUrl}}/security/auth
Content-Type: application/json

{
  "id": "{{$guid}}",
  "ownerEntityId": "{{$guid}}",
  "updatedAt": "{{$datetime iso8601}}",
  "email": "test@example.com",
  "password": "TestPassword123!"
}

### Deauth - Logout
POST {{baseUrl}}/security/deauth
Content-Type: application/json

"{{tokenId}}"

### Refresh Token
POST {{baseUrl}}/security/refresh
Content-Type: application/json

{
  "id": "{{$guid}}",
  "ownerEntityId": "{{$guid}}",
  "updatedAt": "{{$datetime iso8601}}",
  "refreshToken": "your-refresh-token",
  "clientId": "vscode-client"
}

### Validate Token
GET {{baseUrl}}/security/validate
X-Token-Id: {{tokenId}}
```

## Using Postman

Import this collection into Postman:

```json
{
  "info": {
    "name": "Jarvis API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Auth",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"id\": \"{{$guid}}\",\n  \"ownerEntityId\": \"{{$guid}}\",\n  \"updatedAt\": \"2024-01-01T00:00:00Z\",\n  \"email\": \"user@example.com\",\n  \"password\": \"Password123!\"\n}"
        },
        "url": {
          "raw": "http://localhost:7071/api/security/auth",
          "protocol": "http",
          "host": ["localhost"],
          "port": "7071",
          "path": ["api", "security", "auth"]
        }
      }
    }
  ]
}
```

## Debugging

1. **In VS Code:**
   - Press F5 to start debugging
   - Set breakpoints in your function code
   - The debugger will attach automatically

2. **In Visual Studio:**
   - Set the startup project to `core.jarvis.api`
   - Press F5 to debug
   - Functions will start with debugger attached

3. **View logs:**
```bash
func start --verbose
```

## Common Issues

1. **Port already in use:**
   - Change port in `local.settings.json`:
   ```json
   "Host": {
     "LocalHttpPort": 7072
   }
   ```

2. **Storage emulator not running:**
   - Start Azurite:
   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

3. **Missing connection string:**
   - Update `local.settings.json` with your database connection

## Environment Variables

Set these in `local.settings.json` for local development:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Jwt:SecretKey": "your-256-bit-secret-key-here",
    "Jwt:Issuer": "jarvis-api",
    "Jwt:Audience": "jarvis-clients"
  },
  "ConnectionStrings": {
    "JarvisDb": "Host=localhost;Database=jarvis;Username=postgres;Password=postgres"
  }
}
```

## Production Deployment

Deploy to Azure Functions:

```bash
# Create Function App
az functionapp create --resource-group myResourceGroup \
  --consumption-plan-location westus \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --name jarvis-api-functions \
  --storage-account mystorageaccount

# Deploy
func azure functionapp publish jarvis-api-functions
```