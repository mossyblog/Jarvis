# Swagger/OpenAPI Documentation

The Jarvis API now includes comprehensive Swagger/OpenAPI documentation for all endpoints.

## Accessing Swagger UI

When running the API locally, you can access the interactive Swagger UI at:

```
http://localhost:7071/api/swagger/ui
```

## Features

### 1. Interactive API Testing
- Test all endpoints directly from the browser
- Input validation with schema enforcement
- View request/response examples
- See all available parameters and headers

### 2. API Documentation
- Detailed descriptions for each endpoint
- Request/response schemas
- Error response documentation
- Component schema definitions

### 3. Code Generation
You can use the OpenAPI specification to generate client SDKs:

```bash
# Get the OpenAPI spec
curl http://localhost:7071/api/swagger.json -o jarvis-api.json

# Generate C# client
nswag openapi2csclient /input:jarvis-api.json /namespace:JarvisApi.Client /output:JarvisApiClient.cs

# Generate TypeScript client
npx @openapitools/openapi-generator-cli generate -i jarvis-api.json -g typescript-axios -o ./client
```

## API Endpoints

All endpoints are documented with:
- Operation descriptions
- Parameter requirements
- Request body schemas (IComponent objects)
- Response schemas
- Error responses
- Example values

### Security Tag
- `POST /api/security/auth` - Authenticate user
- `POST /api/security/deauth` - Deauthenticate session
- `POST /api/security/refresh` - Refresh tokens
- `GET /api/security/validate` - Validate token

## Component Schemas

All request/response bodies implement the `IComponent` interface:

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "updatedAt": "2024-01-01T00:00:00Z",
  // Additional properties specific to each component type
}
```

## Testing with Swagger UI

1. Navigate to `http://localhost:7071/api/swagger/ui`
2. Click on any endpoint to expand it
3. Click "Try it out"
4. Fill in the required parameters
5. Click "Execute" to send the request
6. View the response below

## Example: Testing Authentication

1. Expand `POST /api/security/auth`
2. Click "Try it out"
3. Use this example request body:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownerEntityId": "550e8400-e29b-41d4-a716-446655440001",
  "updatedAt": "2024-01-01T00:00:00Z",
  "email": "test@example.com",
  "password": "TestPassword123!",
  "clientId": "swagger-ui"
}
```
4. Click "Execute"
5. Copy the `sessionId` from the response for use in other endpoints

## Configuration

OpenAPI configuration is defined in `OpenApiConfigurationOptions.cs`:
- API title and description
- Version information
- Server URLs
- Contact and license info

## Security Considerations

- The Swagger UI is exposed at `/api/swagger/ui` - consider restricting access in production
- The OpenAPI spec at `/api/swagger.json` reveals all API structure
- Use API key or authentication to protect these endpoints in production