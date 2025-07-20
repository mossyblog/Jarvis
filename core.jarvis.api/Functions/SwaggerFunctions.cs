using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace core.jarvis.api.Functions;

/// <summary>
/// Azure Functions for serving OpenAPI documentation with all endpoints.
/// </summary>
public class SwaggerFunctions
{
    private readonly ILogger<SwaggerFunctions> _logger;

    public SwaggerFunctions(ILogger<SwaggerFunctions> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Renders Swagger UI.
    /// </summary>
    [Function(nameof(RenderSwaggerUI))]
    [OpenApiIgnore]
    public async Task<HttpResponseData> RenderSwaggerUI(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/ui")] HttpRequestData req)
    {
        _logger.LogInformation("Swagger UI requested");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");

        var html = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <title>Jarvis API - Swagger UI</title>
                <link rel="stylesheet" type="text/css" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui.css" />
                <style>
                    html { box-sizing: border-box; overflow: -moz-scrollbars-vertical; overflow-y: scroll; }
                    *, *:before, *:after { box-sizing: inherit; }
                    body { margin: 0; background: #fafafa; }
                </style>
            </head>
            <body>
                <div id="swagger-ui"></div>
                <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
                <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui-standalone-preset.js"></script>
                <script>
                window.onload = function() {
                    window.ui = SwaggerUIBundle({
                        url: "/api/swagger.json",
                        dom_id: '#swagger-ui',
                        deepLinking: true,
                        presets: [
                            SwaggerUIBundle.presets.apis,
                            SwaggerUIStandalonePreset
                        ],
                        plugins: [
                            SwaggerUIBundle.plugins.DownloadUrl
                        ],
                        layout: "StandaloneLayout",
                        persistAuthorization: true,
                        initOAuth: {
                            clientId: "swagger-ui",
                            realm: "swagger-ui",
                            appName: "Jarvis API",
                            scopeSeparator: " "
                        }
                    });
                };
                </script>
            </body>
            </html>
            """;

        await response.WriteStringAsync(html);
        return response;
    }

    /// <summary>
    /// Renders OpenAPI v3 document with all endpoints.
    /// </summary>
    [Function(nameof(RenderOpenApiDocument))]
    [OpenApiIgnore]
    public async Task<HttpResponseData> RenderOpenApiDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger.{extension}")] HttpRequestData req,
        string extension)
    {
        _logger.LogInformation("OpenAPI document requested: {Extension}", extension);

        var response = req.CreateResponse(HttpStatusCode.OK);

        var contentType = extension.ToLowerInvariant() switch
        {
            "json" => "application/json",
            "yaml" => "text/yaml",
            "yml" => "text/yaml",
            _ => "application/json"
        };

        response.Headers.Add("Content-Type", contentType);

        try
        {
            var document = GenerateCompleteOpenApiDocument();

            await using var writer = new StringWriter();

            if (extension.ToLowerInvariant() == "yaml" || extension.ToLowerInvariant() == "yml")
            {
                document.SerializeAsV3(new OpenApiYamlWriter(writer));
            }
            else
            {
                document.SerializeAsV3(new OpenApiJsonWriter(writer));
            }

            await response.WriteStringAsync(writer.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OpenAPI document");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync($"Error generating OpenAPI document: {ex.Message}");
        }

        return response;
    }

    private OpenApiDocument GenerateCompleteOpenApiDocument()
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Version = "2.0.0",
                Title = "Jarvis API",
                Description = """
                    Complete API documentation for the Jarvis ECS framework.
                    
                    ## Authentication
                    Most endpoints require authentication via Bearer token in the Authorization header:
                    ```
                    Authorization: Bearer <your-token>
                    ```
                    
                    ## Permissions
                    Endpoints marked with 🔒 require specific permissions:
                    - `admin.*` - Full administrative access
                    - `admin.users.*` - User management
                    - `admin.roles.*` - Role management
                    - `navigation.write` - Navigation management
                    
                    ## Component-Based Design
                    All endpoints work with IComponent objects following the ECS pattern.
                    """,
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@jarvis.dev",
                    Url = new Uri("https://github.com/jarvis-framework/jarvis")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            },
            Servers = new List<OpenApiServer>
            {
                new OpenApiServer { Url = "/api", Description = "Local development server" }
            },
            Tags = new List<OpenApiTag>
            {
                new OpenApiTag { Name = "Authentication", Description = "Authentication and token management" },
                new OpenApiTag { Name = "Account", Description = "User account and profile management" },
                new OpenApiTag { Name = "Roles", Description = "Role management (requires admin permissions)" },
                new OpenApiTag { Name = "Navigation", Description = "Navigation menu management" },
                new OpenApiTag { Name = "Admin", Description = "Administrative operations" }
            },
            Paths = GenerateAllPaths(),
            Components = new OpenApiComponents
            {
                Schemas = GenerateAllSchemas(),
                SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
                {
                    ["bearerAuth"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "JWT Authorization header using the Bearer scheme. Example: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                    }
                }
            }
        };

        return document;
    }

    private OpenApiPaths GenerateAllPaths()
    {
        var paths = new OpenApiPaths();

        // Authentication endpoints
        paths["/security/auth"] = GenerateAuthEndpoint();
        paths["/auth/register"] = GenerateRegisterEndpoint();
        paths["/security/refresh"] = GenerateRefreshEndpoint();
        paths["/security/validate"] = GenerateValidateEndpoint();
        paths["/security/deauth"] = GenerateDeauthEndpoint();

        // Account endpoints
        paths["/accounts/me"] = GenerateAccountMeEndpoint();
        paths["/accounts/navigation"] = GenerateAccountNavigationEndpoint();

        // Role endpoints
        paths["/security/roles"] = GenerateRolesEndpoint();
        paths["/security/roles/{id}"] = GenerateRoleByIdEndpoint();
        paths["/security/roles/{id}/permissions"] = GenerateRolePermissionsEndpoint();
        paths["/security/roles/ensure-defaults"] = GenerateEnsureDefaultRolesEndpoint();

        // Role example endpoints (with permissions)
        paths["/security/roles/example"] = GenerateRolesExampleEndpoint();
        paths["/security/roles/example/{roleId}"] = GenerateRoleExampleByIdEndpoint();
        paths["/security/roles/example/public"] = GenerateRolesExamplePublicEndpoint();

        // Navigation endpoints
        paths["/security/navigation"] = GenerateNavigationEndpoint();
        paths["/security/navigation/ensure-defaults"] = GenerateEnsureDefaultNavigationEndpoint();

        return paths;
    }

    private OpenApiPathItem GenerateAuthEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Authentication" } },
                    Summary = "Authenticate user",
                    Description = "Authenticates a user with email and password",
                    OperationId = "authenticate",
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Authentication credentials",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "Account"
                                    }
                                },
                                Example = new OpenApiObject
                                {
                                    ["email"] = new OpenApiString("user@example.com"),
                                    ["password"] = new OpenApiString("SecurePassword123!")
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Authentication successful",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "AuthToken"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Invalid credentials"),
                        ["429"] = GenerateErrorResponse("Too many authentication attempts")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateRegisterEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Authentication" } },
                    Summary = "Register new user",
                    Description = "Creates a new user account",
                    OperationId = "register",
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Registration information",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "Account"
                                    }
                                },
                                Example = new OpenApiObject
                                {
                                    ["email"] = new OpenApiString("newuser@example.com"),
                                    ["password"] = new OpenApiString("SecurePassword123!")
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Registration successful",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "AuthToken"
                                        }
                                    }
                                }
                            }
                        },
                        ["400"] = GenerateErrorResponse("Invalid registration data or email already exists"),
                        ["429"] = GenerateErrorResponse("Too many registration attempts")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateRefreshEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Authentication" } },
                    Summary = "Refresh tokens",
                    Description = "Refreshes access and refresh tokens",
                    OperationId = "refreshTokens",
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Refresh token request",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "RefreshTokenRequest"
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Token refresh successful",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "AuthToken"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Invalid or expired refresh token")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateValidateEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Authentication" } },
                    Summary = "Validate token (GET)",
                    Description = "Validates token from Authorization header",
                    OperationId = "validateTokenGet",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Token is valid",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "TokenValidation"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Invalid or expired token")
                    }
                },
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Authentication" } },
                    Summary = "Validate token (POST)",
                    Description = "Validates token from request body",
                    OperationId = "validateTokenPost",
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Token to validate",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "ValidateTokenRequest"
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Token is valid",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "TokenValidation"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Invalid or expired token")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateDeauthEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Authentication" } },
                    Summary = "Logout user",
                    Description = "Invalidates the current session",
                    OperationId = "logout",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Session ID to invalidate",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "uuid",
                                    Example = new OpenApiString("550e8400-e29b-41d4-a716-446655440000")
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Logout successful" },
                        ["401"] = GenerateErrorResponse("Invalid or expired token")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateAccountMeEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Account" } },
                    Summary = "Get current user",
                    Description = "Returns the currently authenticated user's account information",
                    OperationId = "getCurrentUser",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Current user account",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "Account"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated")
                    }
                },
                [OperationType.Put] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Account" } },
                    Summary = "Update user profile",
                    Description = "Updates the current user's profile information",
                    OperationId = "updateUserProfile",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Updated profile information",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "SecurityProfile"
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Profile updated successfully",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "SecurityProfile"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["400"] = GenerateErrorResponse("Invalid profile data")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateAccountNavigationEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Account" } },
                    Summary = "Get user navigation",
                    Description = "Returns navigation items available to the current user based on permissions",
                    OperationId = "getUserNavigation",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Navigation items",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new OpenApiSchema
                                        {
                                            Reference = new OpenApiReference
                                            {
                                                Type = ReferenceType.Schema,
                                                Id = "NavigationItem"
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateRolesEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "List all roles",
                    Description = "Returns all roles in the system. 🔒 Requires `admin.roles.read` permission",
                    OperationId = "listRoles",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "List of roles",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new OpenApiSchema
                                        {
                                            Reference = new OpenApiReference
                                            {
                                                Type = ReferenceType.Schema,
                                                Id = "Role"
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions")
                    }
                },
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "Create new role",
                    Description = "Creates a new role. 🔒 Requires `admin.roles.write` permission",
                    OperationId = "createRole",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Role to create",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "Role"
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Role created",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "Role"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions"),
                        ["400"] = GenerateErrorResponse("Invalid role data")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateRoleByIdEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "Get role by ID",
                    Description = "Returns a specific role. 🔒 Requires `admin.roles.read` permission",
                    OperationId = "getRoleById",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Parameters = new List<OpenApiParameter>
                    {
                        new OpenApiParameter
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                            Description = "Role ID"
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Role details",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "Role"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions"),
                        ["404"] = GenerateErrorResponse("Role not found")
                    }
                },
                [OperationType.Put] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "Update role",
                    Description = "Updates a role. 🔒 Requires `admin.roles.write` permission",
                    OperationId = "updateRole",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Parameters = new List<OpenApiParameter>
                    {
                        new OpenApiParameter
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                            Description = "Role ID"
                        }
                    },
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Updated role",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "Role"
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Role updated",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "Role"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions"),
                        ["404"] = GenerateErrorResponse("Role not found")
                    }
                },
                [OperationType.Delete] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "Delete role",
                    Description = "Deletes a role. 🔒 Requires `admin.roles.delete` permission",
                    OperationId = "deleteRole",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Parameters = new List<OpenApiParameter>
                    {
                        new OpenApiParameter
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                            Description = "Role ID"
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["204"] = new OpenApiResponse { Description = "Role deleted" },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions"),
                        ["404"] = GenerateErrorResponse("Role not found")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateRolePermissionsEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Put] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "Update role permissions",
                    Description = "Updates permissions for a role. 🔒 Requires `admin.roles.write` permission",
                    OperationId = "updateRolePermissions",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Parameters = new List<OpenApiParameter>
                    {
                        new OpenApiParameter
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                            Description = "Role ID"
                        }
                    },
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Permission IDs",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "array",
                                    Items = new OpenApiSchema { Type = "string" },
                                    Example = new OpenApiArray
                                    {
                                        new OpenApiString("admin.users.read"),
                                        new OpenApiString("admin.users.write")
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Permissions updated" },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions"),
                        ["404"] = GenerateErrorResponse("Role not found")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateEnsureDefaultRolesEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Admin" } },
                    Summary = "Ensure default roles",
                    Description = "Creates default system roles if they don't exist. 🔒 Requires `admin` permission",
                    OperationId = "ensureDefaultRoles",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Default roles ensured" },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateNavigationEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Navigation" } },
                    Summary = "List navigation items",
                    Description = "Returns all navigation items visible to the current user",
                    OperationId = "listNavigation",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Navigation items",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new OpenApiSchema
                                        {
                                            Reference = new OpenApiReference
                                            {
                                                Type = ReferenceType.Schema,
                                                Id = "NavigationItem"
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated")
                    }
                },
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Navigation" } },
                    Summary = "Create navigation item",
                    Description = "Creates a new navigation item. 🔒 Requires `navigation.write` permission",
                    OperationId = "createNavigation",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Navigation item",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "NavigationItem"
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Navigation item created",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "NavigationItem"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateEnsureDefaultNavigationEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Admin" } },
                    Summary = "Ensure default navigation",
                    Description = "Creates default navigation items if they don't exist. 🔒 Requires `admin` permission",
                    OperationId = "ensureDefaultNavigation",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse { Description = "Default navigation items ensured" },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions")
                    }
                }
            }
        };
    }

    private Dictionary<string, OpenApiSchema> GenerateAllSchemas()
    {
        return new Dictionary<string, OpenApiSchema>
        {
            ["Account"] = GenerateSchemaForType(typeof(Models.Account)),
            ["AuthToken"] = GenerateSchemaForType(typeof(Models.AuthToken)),
            ["SecurityProfile"] = GenerateSchemaForType(typeof(Models.SecurityProfile)),
            ["Role"] = GenerateSchemaForType(typeof(Models.Role)),
            ["Permission"] = GenerateSchemaForType(typeof(Models.Permission)),
            ["NavigationItem"] = GenerateSchemaForType(typeof(Models.NavigationItem)),
            ["TokenValidation"] = GenerateSchemaForType(typeof(Models.TokenValidation)),
            ["RefreshTokenRequest"] = GenerateSchemaForType(typeof(Models.RefreshTokenRequest)),
            ["ValidateTokenRequest"] = GenerateSchemaForType(typeof(Models.ValidateTokenRequest)),
            ["Error"] = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["code"] = new OpenApiSchema { Type = "string" },
                    ["message"] = new OpenApiSchema { Type = "string" },
                    ["details"] = new OpenApiSchema { Type = "object" }
                }
            }
        };
    }

    private OpenApiResponse GenerateErrorResponse(string description)
    {
        return new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = "Error"
                        }
                    }
                }
            }
        };
    }

    private OpenApiSchema GenerateSchemaForType(Type type)
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>()
        };

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propSchema = new OpenApiSchema();

            // Determine property type
            var propType = prop.PropertyType;
            var isNullable = false;

            // Handle nullable types
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                isNullable = true;
                propType = Nullable.GetUnderlyingType(propType)!;
            }

            // Handle arrays
            if (propType.IsArray)
            {
                propSchema.Type = "array";
                var elementType = propType.GetElementType()!;
                if (elementType == typeof(string))
                {
                    propSchema.Items = new OpenApiSchema { Type = "string" };
                }
                else if (elementType == typeof(Guid))
                {
                    propSchema.Items = new OpenApiSchema { Type = "string", Format = "uuid" };
                }
            }
            else if (propType == typeof(string))
            {
                propSchema.Type = "string";
                
                // Add format hints
                if (prop.Name.ToLower().Contains("email"))
                {
                    propSchema.Format = "email";
                }
                else if (prop.Name.ToLower().Contains("password"))
                {
                    propSchema.Format = "password";
                }
                else if (prop.Name.ToLower().Contains("url") || prop.Name.ToLower().Contains("uri"))
                {
                    propSchema.Format = "uri";
                }
            }
            else if (propType == typeof(Guid))
            {
                propSchema.Type = "string";
                propSchema.Format = "uuid";
            }
            else if (propType == typeof(DateTime) || propType == typeof(DateTimeOffset))
            {
                propSchema.Type = "string";
                propSchema.Format = "date-time";
            }
            else if (propType == typeof(bool))
            {
                propSchema.Type = "boolean";
            }
            else if (propType == typeof(int) || propType == typeof(short))
            {
                propSchema.Type = "integer";
                propSchema.Format = "int32";
            }
            else if (propType == typeof(long))
            {
                propSchema.Type = "integer";
                propSchema.Format = "int64";
            }
            else if (propType == typeof(float))
            {
                propSchema.Type = "number";
                propSchema.Format = "float";
            }
            else if (propType == typeof(double) || propType == typeof(decimal))
            {
                propSchema.Type = "number";
                propSchema.Format = "double";
            }

            propSchema.Nullable = isNullable;

            // Add descriptions for common properties
            if (prop.Name == "Id")
            {
                propSchema.Description = "Unique identifier";
                propSchema.Example = new OpenApiString("550e8400-e29b-41d4-a716-446655440000");
            }
            else if (prop.Name == "OwnerEntityId")
            {
                propSchema.Description = "Entity ID that owns this component";
                propSchema.Example = new OpenApiString("550e8400-e29b-41d4-a716-446655440000");
            }
            else if (prop.Name == "UpdatedAt")
            {
                propSchema.Description = "Last update timestamp";
                propSchema.Example = new OpenApiString("2024-01-01T00:00:00Z");
            }
            else if (prop.Name == "CreatedAt")
            {
                propSchema.Description = "Creation timestamp";
                propSchema.Example = new OpenApiString("2024-01-01T00:00:00Z");
            }

            // Convert property name to camelCase for JSON
            var jsonPropertyName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
            schema.Properties[jsonPropertyName] = propSchema;
        }

        // All IComponent models have these required fields
        if (type.GetInterfaces().Any(i => i.Name == "IComponent"))
        {
            schema.Required = new HashSet<string> { "id", "ownerEntityId", "updatedAt" };
        }

        return schema;
    }

    private OpenApiPathItem GenerateRolesExampleEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "List roles (example with permissions)",
                    Description = "Example endpoint showing permission requirements. 🔒 Requires `admin.roles.read` permission",
                    OperationId = "listRolesExample",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "List of roles",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new OpenApiSchema
                                        {
                                            Reference = new OpenApiReference
                                            {
                                                Type = ReferenceType.Schema,
                                                Id = "Role"
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions - requires admin.roles.read")
                    }
                },
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "Create role (example with permissions)",
                    Description = "Example endpoint showing permission requirements. 🔒 Requires `admin.roles.write` permission",
                    OperationId = "createRoleExample",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Role to create",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "Role"
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["201"] = new OpenApiResponse
                        {
                            Description = "Role created",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "Role"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions - requires admin.roles.write")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateRoleExampleByIdEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Put] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "Update role (example with permissions)",
                    Description = "Example endpoint showing permission requirements. 🔒 Requires `admin.roles.write` permission",
                    OperationId = "updateRoleExample",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Parameters = new List<OpenApiParameter>
                    {
                        new OpenApiParameter
                        {
                            Name = "roleId",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                            Description = "Role ID"
                        }
                    },
                    RequestBody = new OpenApiRequestBody
                    {
                        Description = "Updated role data",
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "Role"
                                    }
                                }
                            }
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Role updated",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "Role"
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions - requires admin.roles.write"),
                        ["404"] = GenerateErrorResponse("Role not found")
                    }
                },
                [OperationType.Delete] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "Delete role (example with permissions)",
                    Description = "Example endpoint showing permission requirements. 🔒 Requires `admin.roles.delete` permission",
                    OperationId = "deleteRoleExample",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Parameters = new List<OpenApiParameter>
                    {
                        new OpenApiParameter
                        {
                            Name = "roleId",
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                            Description = "Role ID to delete"
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["204"] = new OpenApiResponse { Description = "Role deleted successfully" },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions - requires admin.roles.delete"),
                        ["404"] = GenerateErrorResponse("Role not found")
                    }
                }
            }
        };
    }

    private OpenApiPathItem GenerateRolesExamplePublicEndpoint()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Roles" } },
                    Summary = "List roles (public with OR permissions)",
                    Description = "Example endpoint showing OR permission requirements. 🔒 Requires `roles.read` OR `admin` permission",
                    OperationId = "listRolesPublic",
                    Security = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } }] = new List<string>()
                        }
                    },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "List of roles",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new OpenApiSchema
                                        {
                                            Reference = new OpenApiReference
                                            {
                                                Type = ReferenceType.Schema,
                                                Id = "Role"
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["401"] = GenerateErrorResponse("Not authenticated"),
                        ["403"] = GenerateErrorResponse("Insufficient permissions - requires roles.read OR admin")
                    }
                }
            }
        };
    }
}