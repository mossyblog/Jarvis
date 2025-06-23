using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using core.jarvis.api.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(workerApplication =>
    {
        // Configure middleware pipeline in order
        workerApplication.UseMiddleware<core.jarvis.api.Middleware.SecurityHeadersMiddleware>();
        workerApplication.UseMiddleware<core.jarvis.api.Middleware.RateLimitingMiddleware>();
        workerApplication.UseMiddleware<core.jarvis.api.Middleware.InputValidationMiddleware>();
        workerApplication.UseMiddleware<core.jarvis.api.Middleware.ComponentValidationMiddleware>();
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