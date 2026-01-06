using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Components;
using core.jarvis.Validation;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Fixtures.Handlers;

/// <summary>
/// Test handler for unit tests.
/// </summary>
public class TestHandler : ComponentHandler<TestComponent>
{
    public TestHandler(
        IDataContext dataContext,
        ILogger<TestHandler> logger)
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
        Guard.AgainstEmpty(reason, nameof(reason));
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

    public async Task Create(string name, string status)
    {
        Guard.AgainstEmpty(name, nameof(name));
        Guard.AgainstEmpty(status, nameof(status));
        var component = new TestComponent
        {
            OwnerEntityId = base.OwnerEntityId,
            Name = name,
            Status = status
        };
        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("Component creation failed due to concurrent modification");
        }
    }
}

