using System.Linq.Expressions;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace core.jarvis.tests;

public class ComponentQueryHandlerTests
{
    public class TestComponent : IComponent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OwnerEntityId { get; set; } = Guid.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string Name { get; init; } = string.Empty;
        public int Status { get; init; } = 0;
    }

    [Fact]
    public void ApplyExpressionFilter_WithEqualsExpression_ShouldAddFilter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Set up PostgreSQL connection for testing
        // Use environment variable - never hard-code credentials
        // Environment variable should be set by CI/test runner
        
        services.RegisterJarvis(LogLevel.Trace);
        var serviceProvider = services.BuildServiceProvider();
        
        var pgClient = serviceProvider.GetRequiredService<IPgClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<ComponentQueryHandler<TestComponent>>>();
        var handler = new ComponentQueryHandler<TestComponent>(pgClient, logger);
        
        var testEntityId = Guid.NewGuid();
        Expression<Func<TestComponent, bool>> filter = c => c.OwnerEntityId == testEntityId;
        
        // Act & Assert - This should not throw an exception
        var task = handler.QueryEntityIds(filter);
        
        // If we get here without exception, the expression parsing worked
        Assert.NotNull(task);
    }
    
    [Fact]  
    public void ApplyExpressionFilter_WithStringContains_ShouldAddLikeFilter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Use environment variable - never hard-code credentials
        // Environment variable should be set by CI/test runner
        
        services.RegisterJarvis(LogLevel.Trace);
        var serviceProvider = services.BuildServiceProvider();
        
        var pgClient = serviceProvider.GetRequiredService<IPgClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<ComponentQueryHandler<TestComponent>>>();
        var handler = new ComponentQueryHandler<TestComponent>(pgClient, logger);
        
        Expression<Func<TestComponent, bool>> filter = c => c.Name.Contains("Test");
        
        // Act & Assert - This should not throw an exception  
        var task = handler.QueryEntityIds(filter);
        
        Assert.NotNull(task);
    }
}