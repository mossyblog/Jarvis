# Jarvis API - Azure Functions

RESTful API layer for the Jarvis ECS framework, built on Azure Functions with clean exception handling, global middleware, and OpenAPI documentation.

## Key Features

- **Clean Exception Bubbling**: Global exception middleware handles all errors consistently
- **No Try-Catch in Functions**: Exceptions bubble up naturally to middleware
- **Type-Safe Components**: Direct use of IComponent types in API contracts
- **OpenAPI/Swagger**: Full API documentation with interactive UI
- **Security Middleware**: JWT validation, rate limiting, security headers

## Architecture

```
Request → ExceptionHandlingMiddleware → SecurityMiddleware → Function → System → Handler
                      ↑                                                            ↓
                      └──────────── Exceptions bubble up ←────────────────────────┘
```

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

## Exception Handling

The API uses a clean exception bubbling pattern with centralized handling:

### Domain Exceptions
```csharp
// In Handler - just throw
if (!order.IsPaid)
    throw new BusinessRuleException("ORDER_NOT_PAID", "Order must be paid");

if (entity == null)
    throw new EntityNotFoundException(entityId, "Order");

if (string.IsNullOrEmpty(email))
    throw new ValidationException("Email is required");
```

### API Functions - No Try-Catch
```csharp
[Function("processOrder")]
public async Task<HttpResponseData> ProcessOrder(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
    Guid orderId)
{
    // No try-catch - exceptions bubble to middleware
    var system = _serviceProvider.GetRequiredService<OrderSystem>();
    await system.ProcessOrder(orderId);
    
    return req.CreateResponse(HttpStatusCode.OK);
}
```

### Automatic HTTP Response Conversion
- `ValidationException` → 400 Bad Request
- `BusinessRuleException` → 400 Bad Request
- `EntityNotFoundException` → 404 Not Found
- `UnauthorizedException` → 401 Unauthorized
- `Exception` → 500 Internal Server Error (logged, not exposed)

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
    "Jwt:Audience": "jarvis-clients",
    "ASPNETCORE_ENVIRONMENT": "Development"
  },
  "ConnectionStrings": {
    "JarvisDb": "Host=localhost;Database=jarvis;Username=postgres;Password=postgres"
  }
}
```

## Middleware Pipeline

Middleware executes in order:

1. **ExceptionHandlingMiddleware** - Catches all exceptions, converts to HTTP responses
2. **SecurityHeadersMiddleware** - Adds security headers (CSP, X-Frame-Options, etc.)
3. **AuthorizationMiddleware** - JWT validation and claims extraction
4. **RateLimitingMiddleware** - (Optional) Request throttling
5. **Your Function** - Business logic execution

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

# Configure app settings
az functionapp config appsettings set \
  --name jarvis-api-functions \
  --resource-group myResourceGroup \
  --settings ASPNETCORE_ENVIRONMENT=Production

# Deploy
func azure functionapp publish jarvis-api-functions
```

## Best Practices

1. **Let exceptions bubble** - Don't catch in functions
2. **Use domain exceptions** - BusinessRuleException, ValidationException
3. **Component contracts** - Use IComponent types directly in API
4. **Middleware handles errors** - Single point of exception handling
5. **Log at boundaries** - Log in middleware, not in every handler