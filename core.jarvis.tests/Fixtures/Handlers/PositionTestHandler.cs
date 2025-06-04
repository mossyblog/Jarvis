using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Fixtures.Components;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Fixtures.Handlers;

public class PositionTestHandler : ComponentHandler<PositionComponent>
{
    public PositionTestHandler(
        IDataContext dataContext,
        ILogger<PositionTestHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task Create(float x, float y)
    {
        var component = new PositionComponent
        {
            OwnerEntityId = OwnerEntityId,
            X = x,
            Y = y
        };

        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("Component creation failed due to concurrent modification");
        }
    }

    public async Task UpdatePosition(float x, float y)
    {
        var component = await GetRequired();
        component.X = x;
        component.Y = y;
        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("Component update failed due to concurrent modification");
        }
    }

    public new async Task<PositionComponent?> GetOrDefault()
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