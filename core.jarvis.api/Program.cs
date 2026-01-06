using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using core.jarvis.api.Extensions;
using DotNetEnv;

// Load environment variables from .env.local if it exists (for local development)
if (File.Exists(".env.local"))
{
    Env.Load(".env.local");
}

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(workerApplication =>
    {
        // Configure middleware pipeline in order
        // Exception handling middleware should be first to catch all exceptions
        workerApplication.UseMiddleware<core.jarvis.api.Middleware.ExceptionHandlingMiddleware>();
        workerApplication.UseMiddleware<core.jarvis.api.Middleware.SecurityHeadersMiddleware>();
        // TEMPORARILY DISABLED: These middlewares consume request body
        // workerApplication.UseMiddleware<core.jarvis.api.Middleware.RateLimitingMiddleware>();
        workerApplication.UseMiddleware<core.jarvis.api.Middleware.AuthorizationMiddleware>();
        // workerApplication.UseMiddleware<core.jarvis.api.Middleware.InputValidationMiddleware>();
        // workerApplication.UseMiddleware<core.jarvis.api.Middleware.ComponentValidationMiddleware>();
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        // Ensure local.settings.json is loaded
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add Jarvis services
        services.AddJarvisApiServices();
    })
    .Build();

host.Run();

// Make Program class accessible for testing
public partial class Program { }