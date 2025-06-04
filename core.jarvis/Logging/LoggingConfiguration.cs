using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace core.jarvis.Logging;

/// <summary>
/// Provides consistent Serilog configuration for the Jarvis framework.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog with standard settings for the Jarvis framework.
    /// </summary>
    public static IServiceCollection AddJarvisSerilog(
        this IServiceCollection services,
        Action<LoggerConfiguration>? configureLogger = null)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}");

        // Allow custom configuration
        configureLogger?.Invoke(loggerConfiguration);

        Log.Logger = loggerConfiguration.CreateLogger();

        // Replace the default logger factory with Serilog
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        return services;
    }

    /// <summary>
    /// Creates a logger configuration for testing with minimal output.
    /// </summary>
    public static LoggerConfiguration CreateTestLoggerConfiguration()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Warning()
            .MinimumLevel.Override("core.jarvis", LogEventLevel.Debug)
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "[{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .Enrich.FromLogContext();
    }
}