using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.tests.Components;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Helpers;

public class VelocityTestHandler : ComponentHandler<VelocityComponent>
{
    public VelocityTestHandler(
        IDataContext dataContext,
        ILogger<VelocityTestHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task Create(float dx, float dy)
    {
        var component = new VelocityComponent
        {
            OwnerEntityId = OwnerEntityId,
            DeltaX = dx,
            DeltaY = dy
        };

        var success = await DataContext.TryCommit(component);
        if (!success)
        {
            throw new ConcurrencyException("Component creation failed due to concurrent modification");
        }
    }

    public new async Task<VelocityComponent?> GetOrDefault()
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