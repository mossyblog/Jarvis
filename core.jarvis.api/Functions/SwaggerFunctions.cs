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
/// Azure Functions for serving OpenAPI documentation.
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
                <title>Jarvis Security API - Swagger UI</title>
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
                        layout: "StandaloneLayout"
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
    /// Renders OpenAPI v3 document.
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
            var document = GenerateOpenApiDocument();

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

    private OpenApiDocument GenerateOpenApiDocument()
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Version = "1.0.0",
                Title = "Jarvis Security API",
                Description = "Authentication and authorization API for the Jarvis ECS framework. All endpoints accept and return IComponent-based objects.",
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
            Paths = new OpenApiPaths
            {
                ["/security/auth"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Post] = new OpenApiOperation
                        {
                            Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Security" } },
                            Summary = "Authenticate user",
                            Description = "Authenticates a user with email and password, optionally with 2FA code",
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
                                                Id = "Auth"
                                            }
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
                                                    Id = "Auth"
                                                }
                                            }
                                        }
                                    }
                                },
                                ["401"] = new OpenApiResponse
                                {
                                    Description = "Invalid credentials"
                                },
                                ["429"] = new OpenApiResponse
                                {
                                    Description = "Too many authentication attempts"
                                }
                            }
                        }
                    }
                },
                ["/security/deauth"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Post] = new OpenApiOperation
                        {
                            Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Security" } },
                            Summary = "Deauthenticate session",
                            Description = "Invalidates the current authentication session",
                            OperationId = "deauthenticate",
                            Parameters = new List<OpenApiParameter>
                            {
                                new OpenApiParameter
                                {
                                    Name = "Authorization",
                                    In = ParameterLocation.Header,
                                    Required = true,
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "string"
                                    },
                                    Description = "Bearer token"
                                }
                            },
                            RequestBody = new OpenApiRequestBody
                            {
                                Description = "Session ID to deauthenticate",
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
                                ["200"] = new OpenApiResponse
                                {
                                    Description = "Deauthentication successful"
                                },
                                ["401"] = new OpenApiResponse
                                {
                                    Description = "Invalid or expired token"
                                }
                            }
                        }
                    }
                },
                ["/security/refresh"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Post] = new OpenApiOperation
                        {
                            Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Security" } },
                            Summary = "Refresh tokens",
                            Description = "Refreshes access and refresh tokens using a valid refresh token",
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
                                                Id = "Auth"
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
                                                    Id = "Auth"
                                                }
                                            }
                                        }
                                    }
                                },
                                ["401"] = new OpenApiResponse
                                {
                                    Description = "Invalid or expired refresh token"
                                }
                            }
                        }
                    }
                },
                ["/security/validate"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Get] = new OpenApiOperation
                        {
                            Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Security" } },
                            Summary = "Validate token",
                            Description = "Validates the provided authentication token",
                            OperationId = "validateToken",
                            Parameters = new List<OpenApiParameter>
                            {
                                new OpenApiParameter
                                {
                                    Name = "Authorization",
                                    In = ParameterLocation.Header,
                                    Required = true,
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "string"
                                    },
                                    Description = "Bearer token"
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
                                ["401"] = new OpenApiResponse
                                {
                                    Description = "Invalid or expired token"
                                }
                            }
                        }
                    }
                }
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>
                {
                    ["User"] = GenerateSchemaForType(typeof(Models.User)),
                    ["AuthToken"] = GenerateSchemaForType(typeof(Models.AuthToken)),
                    ["TokenValidation"] = GenerateSchemaForType(typeof(Models.TokenValidation))
                }
            }
        };

        return document;
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

            if (prop.PropertyType == typeof(string))
            {
                propSchema.Type = "string";
                if (prop.Name == "Email")
                {
                    propSchema.Format = "email";
                }
                else if (prop.Name == "Password")
                {
                    propSchema.Format = "password";
                }
            }
            else if (prop.PropertyType == typeof(Guid))
            {
                propSchema.Type = "string";
                propSchema.Format = "uuid";
            }
            else if (prop.PropertyType == typeof(DateTime))
            {
                propSchema.Type = "string";
                propSchema.Format = "date-time";
            }
            else if (prop.PropertyType == typeof(bool))
            {
                propSchema.Type = "boolean";
            }
            else if (prop.PropertyType == typeof(int))
            {
                propSchema.Type = "integer";
                propSchema.Format = "int32";
            }
            else if (prop.PropertyType == typeof(long))
            {
                propSchema.Type = "integer";
                propSchema.Format = "int64";
            }

            // Add example values
            if (prop.Name == "Id" || prop.Name == "OwnerEntityId" || prop.Name == "SessionId")
            {
                propSchema.Example = new OpenApiString("550e8400-e29b-41d4-a716-446655440000");
            }
            else if (prop.Name == "Email")
            {
                propSchema.Example = new OpenApiString("user@example.com");
            }
            else if (prop.Name == "UpdatedAt")
            {
                propSchema.Example = new OpenApiString("2024-01-01T00:00:00Z");
            }

            // Handle nullable types
            var nullableType = Nullable.GetUnderlyingType(prop.PropertyType);
            if (nullableType != null || (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                propSchema.Nullable = true;
            }

            schema.Properties[char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1)] = propSchema;
        }

        // All our models implement IComponent, so they have these required fields
        schema.Required = new HashSet<string> { "id", "ownerEntityId", "updatedAt" };

        return schema;
    }
}