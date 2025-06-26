using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Systems;

/// <summary>
/// Extension methods for registering System services
/// </summary>
public static class SystemServiceExtensions
{
    /// <summary>
    /// Adds the Jarvis System layer to the service collection
    /// </summary>
    public static IServiceCollection AddJarvisSystem(this IServiceCollection services)
    {
        // Register system based on configuration
        services.AddScoped<ISystem>(sp =>
        {
            var dataContext = sp.GetRequiredService<Data.IDataContext>();
            var logger = sp.GetRequiredService<ILogger<HandlerSystem>>();
            var config = sp.GetRequiredService<IConfiguration>();
            
            // Use transactional system if configured (once InTransaction is implemented)
            // For now, always use HandlerSystem
            return new HandlerSystem(dataContext, logger);
        });
        
        return services;
    }
}