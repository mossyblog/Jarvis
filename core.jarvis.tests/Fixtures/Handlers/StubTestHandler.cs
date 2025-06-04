using core.jarvis.Data;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.Validation;

namespace core.jarvis.tests.Fixtures.Handlers;

/// <summary>
/// Stub handler for testing that returns pre-configured responses.
/// </summary>
public class StubTestHandler : IComponentHandler<TestComponent>
{
    private Guid _entityId;
    private TestComponent? _component;
    private Exception? _exceptionToThrow;

    public Guid OwnerEntityId => _entityId;

    public StubTestHandler()
    {
       
    }
    
    public void InitializeContext(Guid entityId)
    {
        Guard.AgainstEmptyGuid(entityId, nameof(entityId));
        _entityId = entityId;
    }
    public void SetupGet(TestComponent component)
    {
        _component = component;
    }

    public void SetupGetThrows(Exception exception)
    {
        _exceptionToThrow = exception;
    }

    public async Task<TestComponent> Get()
    {
        await Task.CompletedTask;
        
        if (_exceptionToThrow != null)
            throw _exceptionToThrow;

        if (_component != null)
            return _component;

        throw new InvalidOperationException("Handler not configured. Use SetupGet or SetupGetThrows.");
    }

    async Task<IComponent> IComponentHandler.Get() => await Get();
}