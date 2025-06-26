using core.jarvis.Data;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Systems;

/// <summary>
/// Basic system implementation that orchestrates handler execution
/// </summary>
public class HandlerSystem : ISystem
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<HandlerSystem> _logger;
    
    public HandlerSystem(IDataContext dataContext, ILogger<HandlerSystem> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }
    
    public async Task<Guid> ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task<Guid>> handlerMethod) 
        where THandler : class, IComponentHandler
    {
        _logger.LogDebug("Executing {HandlerType} for entity {EntityId}", typeof(THandler).Name, entityId);
        
        var handler = _dataContext.For<THandler>(entityId);
        var result = await handlerMethod(handler);
        
        _logger.LogDebug("Handler operation completed: {EntityId}", result);
        return result;
    }
    
    public async Task<TComponent> ExecuteHandler<THandler, TComponent>(Guid entityId, Func<THandler, Task<TComponent>> handlerMethod) 
        where THandler : class, IComponentHandler
        where TComponent : IComponent
    {
        _logger.LogDebug("Executing {HandlerType} for entity {EntityId}", typeof(THandler).Name, entityId);
        
        var handler = _dataContext.For<THandler>(entityId);
        var result = await handlerMethod(handler);
        
        _logger.LogDebug("Handler operation completed");
        return result;
    }
    
    public async Task ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task> handlerMethod) 
        where THandler : class, IComponentHandler
    {
        _logger.LogDebug("Executing {HandlerType} for entity {EntityId}", typeof(THandler).Name, entityId);
        
        var handler = _dataContext.For<THandler>(entityId);
        await handlerMethod(handler);
        
        _logger.LogDebug("Handler operation completed");
    }
    
    public async Task<Guid> CreateComponent<TComponent>(TComponent component) 
        where TComponent : class, IComponent, new()
    {
        _logger.LogDebug("Creating {ComponentType}", typeof(TComponent).Name);
        
        var entityId = Guid.NewGuid();
        
        // Create a new instance with the IDs set
        var newComponent = new TComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId
        };
        
        // Copy other properties from the input component
        foreach (var property in typeof(TComponent).GetProperties())
        {
            if (property.Name != "Id" && property.Name != "OwnerEntityId" && property.CanWrite)
            {
                property.SetValue(newComponent, property.GetValue(component));
            }
        }
        
        await _dataContext.Commit(newComponent);
        
        _logger.LogDebug("Component created: {EntityId}", entityId);
        return entityId;
    }
    
    public async Task<TResult> ExecuteHandlerWithResult<THandler, TResult>(Guid entityId, Func<THandler, Task<TResult>> handlerMethod) 
        where THandler : class, IComponentHandler
    {
        _logger.LogDebug("Executing {HandlerType} for entity {EntityId} with result type {ResultType}", 
            typeof(THandler).Name, entityId, typeof(TResult).Name);
        
        var handler = _dataContext.For<THandler>(entityId);
        var result = await handlerMethod(handler);
        
        _logger.LogDebug("Handler operation completed");
        return result;
    }
}