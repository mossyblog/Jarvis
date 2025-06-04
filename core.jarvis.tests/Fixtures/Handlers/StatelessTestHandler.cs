using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Fixtures.Components;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Fixtures.Handlers;

/// <summary>
/// Stateless test handler that can be registered as a singleton in DI.
/// </summary>
public class StatelessTestHandler : ComponentHandler<TestComponent>
{
    public StatelessTestHandler(
        IDataContext dataContext,
        ILogger<StatelessTestHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task<bool> Activate()
    {
        var component = await GetRequired();
        Ensure(component.Status != "ACTIVE", "Component is already active");
        component.Status = "ACTIVE";
        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("Component activation failed due to concurrent modification");
        }
        return true;
    }

    public async Task<bool> Deactivate(string reason)
    {
        if (string.IsNullOrEmpty(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));
            
        var component = await GetRequired();
        Ensure(component.Status == "ACTIVE", "Component must be active to deactivate");
        component.Status = "INACTIVE";
        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("Component deactivation failed due to concurrent modification");
        }
        Logger.LogInformation("Deactivated component {ComponentId} for reason: {Reason}", 
            component.Id, reason);
        return true;
    }
}

/// <summary>
/// Stateless position handler that can be registered as a singleton in DI.
/// </summary>
public class StatelessPositionHandler : ComponentHandler<PositionComponent>
{
    public StatelessPositionHandler(
        IDataContext dataContext,
        ILogger<StatelessPositionHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task SetPosition(float x, float y)
    {
        var component = await GetOrDefault() ?? new PositionComponent { OwnerEntityId = OwnerEntityId };
        component.X = x;
        component.Y = y;
        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("PositionComponent update failed due to concurrent modification");
        }
    }
}

/// <summary>
/// Stateless velocity handler that can be registered as a singleton in DI.
/// </summary>
public class StatelessVelocityHandler : ComponentHandler<VelocityComponent>
{
    public StatelessVelocityHandler(
        IDataContext dataContext,
        ILogger<StatelessVelocityHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task SetVelocity(float deltaX, float deltaY)
    {
        var component = await GetOrDefault() ?? new VelocityComponent { OwnerEntityId = OwnerEntityId };
        component.DeltaX = deltaX;
        component.DeltaY = deltaY;
        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("VelocityComponent update failed due to concurrent modification");
        }
    }
}