using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Components;
using core.jarvis.Validation;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Fixtures.Handlers;

public class TransactionTestHandler : ComponentHandler<TestComponent>
{
    public TransactionTestHandler(
        IDataContext dataContext,
        ILogger<TransactionTestHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task Create(string name, string status, int value = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(nameof(name), "Name cannot be empty");
        }
        Guard.AgainstEmpty(status, nameof(status));

        var component = new TestComponent
        {
            OwnerEntityId = OwnerEntityId,
            Name = name,
            Status = status,
            Value = value
        };

        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("Component creation failed due to concurrent modification");
        }
    }

    public async Task UpdateStatus(string newStatus)
    {
        var component = await GetRequired();
        component.Status = newStatus;
        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("Component update failed due to concurrent modification");
        }
    }

    public new async Task<TestComponent?> GetOrDefault()
    {
        try
        {
            return await Get();
        }
        catch (EntityNotFoundException)
        {
            return null;
        }
    }
}