using core.jarvis.Data;
using core.jarvis.tests.Components;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Fixtures.Handlers;

/// <summary>
/// Concrete implementation of ComponentHandler for testing.
/// </summary>
public class ConcreteTestHandler : ComponentHandler<TestComponent>
{
    public ConcreteTestHandler(
        IDataContext dataContext, 
        ILogger<ConcreteTestHandler> logger) 
        : base(dataContext, logger)
    {
    }

    // Expose protected methods for testing
    public new async Task<TestComponent?> GetOrDefault() => await base.GetOrDefault();
    public new async Task<TestComponent> GetRequired() => await base.GetRequired();
    public new void Ensure(bool condition, string message) => base.Ensure(condition, message);

    // Expose protected properties for testing
    public Guid TestEntityId => OwnerEntityId;
    public ILogger TestLogger => Logger;

    // Add a private helper for test usage if needed
    private TestComponent CreateComponent()
    {
        // Implementation of CreateComponent logic
        throw new NotImplementedException(); // Placeholder return, actual implementation needed
    }
}