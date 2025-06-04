using core.jarvis.Exceptions;
using core.jarvis.Validation;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Base class for component handlers that use DataContext for all operations.
/// This ensures all data operations go through the centralized audit/logging layer.
/// </summary>
/// <typeparam name="TComponent">The type of component this handler manages.</typeparam>
public abstract class DataContextComponentHandler<TComponent> : IComponentHandler<TComponent> 
    where TComponent : class, IComponent, new()
{
    private Guid _entityId;
    
    /// <summary>
    /// Gets the entity ID this handler is operating on.
    /// </summary>
    public Guid EntityId => _entityId;

    /// <summary>
    /// Gets the DataContext for all data operations.
    /// </summary>
    protected IDataContext DataContext { get; }

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }
    
    /// <summary>
    /// Gets the event subscription manager for real-time subscriptions.
    /// </summary>
    protected EventSubscriptionManager? EventManager { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContextComponentHandler{TComponent}"/> class.
    /// </summary>
    /// <param name="entityId">The entity ID to operate on.</param>
    /// <param name="dataContext">The DataContext for all operations.</param>
    /// <param name="logger">The logger instance.</param>
    protected DataContextComponentHandler(
        IDataContext dataContext,
        ILogger logger)
    {
        //Guard.AgainstEmptyGuid(entityId, nameof(entityId));
        Guard.AgainstNull(dataContext, nameof(dataContext));
        Guard.AgainstNull(logger, nameof(logger));

       // EntityId = entityId;
        DataContext = dataContext;
        Logger = logger;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DataContextComponentHandler{TComponent}"/> class with event support.
    /// </summary>
    /// <param name="entityId">The entity ID to operate on.</param>
    /// <param name="dataContext">The DataContext for all operations.</param>
    /// <param name="eventManager">The event subscription manager.</param>
    /// <param name="logger">The logger instance.</param>
    protected DataContextComponentHandler(
        IDataContext dataContext,
        EventSubscriptionManager eventManager,
        ILogger logger)
        : this(dataContext, logger)
    {
        EventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
    }
    
    /// <inheritdoc/>
    public void InitializeContext(Guid entityId)
    {
        Guard.AgainstEmptyGuid(entityId, nameof(entityId));
        _entityId = entityId;
    }

    /// <inheritdoc/>
    public virtual async Task<TComponent> Get()
    {
        try
        {
            // Use DataContext.Query() to get the component for this entity
            var query = DataContext.Query()
                .With<TComponent>(c => c.OwnerEntityId == EntityId);
            var componentsByEntity = await query.ToEntityComponents();

            if (!componentsByEntity.TryGetValue(EntityId, out var entityComponents))
            {
                throw new EntityNotFoundException(EntityId, typeof(TComponent).Name);
            }

            var component = entityComponents.Get<TComponent>();
            if (component == null)
            {
                throw new EntityNotFoundException(EntityId, typeof(TComponent).Name);
            }

            return component;
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Logger.LogError(ex, "Failed to get {ComponentType} for entity {OwnerEntityId}", 
                typeof(TComponent).Name, EntityId);
            throw new ComponentOperationException(
                typeof(TComponent).Name, 
                "GET", 
                $"Failed to retrieve {typeof(TComponent).Name}", 
                ex);
        }
    }

    /// <inheritdoc/>
    async Task<IComponent> IComponentHandler.Get()
    {
        return await Get();
    }

    /// <summary>
    /// Saves a component using DataContext (with audit/logging).
    /// </summary>
    /// <param name="component">The component to save.</param>
    protected async Task<bool> TryCommit(TComponent component)
    {
        return await DataContext.TryCommit(component);
    }

    /// <summary>
    /// Removes all components for this entity using DataContext (with audit/logging).
    /// </summary>
    protected async Task Remove()
    {
        await DataContext.Remove<TComponent>(EntityId);
    }

    /// <summary>
    /// Removes a specific component using DataContext (with audit/logging).
    /// </summary>
    /// <param name="componentId">The component ID to remove.</param>
    protected async Task Remove(Guid componentId)
    {
        await DataContext.Remove<TComponent>(componentId);
    }

    /// <summary>
    /// Validates a business rule and throws if violated.
    /// </summary>
    /// <param name="condition">The condition that must be true.</param>
    /// <param name="message">The error message if the condition is false.</param>
    /// <exception cref="BusinessRuleException">Thrown when the condition is false.</exception>
    protected void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new BusinessRuleException(GetType().Name, message);
        }
    }

    /// <summary>
    /// Subscribes to real-time changes for a specific component type.
    /// </summary>
    /// <typeparam name="TOtherComponent">The component type to subscribe to.</typeparam>
    /// <param name="onChanged">The action to invoke when the component changes.</param>
    protected async Task SubscribeToComponent<TOtherComponent>(Action<TOtherComponent> onChanged)
        where TOtherComponent : class, IComponent, new()
    {
        if (EventManager == null)
        {
            throw new InvalidOperationException("EventManager is not configured. Use constructor with EventSubscriptionManager parameter.");
        }
        
        await EventManager.SubscribeToComponent<TOtherComponent>(EntityId, onChanged);
    }
}