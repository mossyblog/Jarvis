using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using core.jarvis.api.Extensions;
using core.jarvis.api.Processors;
using DotNetEnv;

// Load environment variables from .env.local if it exists (for local development)
if (File.Exists(".env.local"))
{
    Env.Load(".env.local");
}

var builder = WebApplication.CreateBuilder(args);

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Add JWT authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrEmpty(jwtSecretKey))
{
    // Allow test environment to use a default key (set by WebApplicationFactory)
    if (builder.Environment.EnvironmentName == "Test")
    {
        jwtSecretKey = "TEST_JARVIS_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_MINIMUM_256_BITS_LONG_TO_MEET_ALL_REQUIREMENTS";
    }
    else
    {
        throw new InvalidOperationException("JWT secret key not configured. Please set Jwt__SecretKey in environment variables.");
    }
}

builder.Services.AddAuthenticationJwtBearer(options =>
{
    options.SigningKey = jwtSecretKey;
});
builder.Services.AddAuthorization();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // In production, replace with specific origins
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "https://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Swagger
builder.Services.SwaggerDocument(options =>
{
    options.DocumentSettings = settings =>
    {
        settings.Title = "Jarvis API";
        settings.Version = "v1";
        settings.Description = "Jarvis ECS API powered by FastEndpoints";
    };
});

// Add Jarvis services
builder.Services.AddJarvisApiServices();

var app = builder.Build();

// Security middleware - order matters!
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    await next();
});

app.UseCors();

// Configure middleware pipeline
app.UseAuthentication();
app.UseAuthorization();

// Add FastEndpoints with processors
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";

    config.Errors.UseProblemDetails();

    config.Serializer.Options.PropertyNamingPolicy = null; // PascalCase to match existing API

    // Register global pre-processors
    // Order: RateLimiting -> UserValidation -> Permission
    // UserValidation must run before Permission to ensure user exists
    config.Endpoints.Configurator = ep =>
    {
        ep.PreProcessor<RateLimitingPreProcessor>(Order.Before);
        ep.PreProcessor<UserValidationPreProcessor>(Order.Before);
        ep.PreProcessor<PermissionPreProcessor>(Order.Before);
    };
});

// Add Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }
