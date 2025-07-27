using core.jarvis.ErrorHandling;
using core.jarvis.Exceptions;
using core.jarvis.Validation;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Base class for component handlers that provides common functionality.
/// </summary>
/// <typeparam name="TComponent">The type of component this handler manages.</typeparam>
public abstract class ComponentHandler<TComponent> : IComponentHandler<TComponent> 
    where TComponent : class, IComponent, new()
{
    private Guid _ownerEntityId;
    
    /// <summary>
    /// Gets the entity ID this handler is operating on.
    /// </summary>
    public Guid OwnerEntityId => _ownerEntityId;

    /// <summary>
    /// Gets the DataContext for all data operations.
    /// </summary>
    protected IDataContext DataContext { get; }

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentHandler{TComponent}"/> class
    /// without an entity ID. The entity ID must be set via SetEntityId before use.
    /// </summary>
    /// <param name="dataContext">The DataContext for all operations.</param>
    /// <param name="logger">The logger instance.</param>
    protected ComponentHandler(
        IDataContext dataContext,
        ILogger logger)
    {
        Guard.AgainstNull(dataContext, nameof(dataContext));
        Guard.AgainstNull(logger, nameof(logger));

        DataContext = dataContext;
        Logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentHandler{TComponent}"/> class.
    /// </summary>
    /// <param name="ownerEntityId">The entity ID to operate on.</param>
    /// <param name="dataContext">The DataContext for all operations.</param>
    /// <param name="logger">The logger instance.</param>
    protected ComponentHandler(
        Guid ownerEntityId, 
        IDataContext dataContext,
        ILogger logger)
    {
        Guard.AgainstEmptyGuid(ownerEntityId, nameof(ownerEntityId));
        Guard.AgainstNull(dataContext, nameof(dataContext));
        Guard.AgainstNull(logger, nameof(logger));

        _ownerEntityId = ownerEntityId;
        DataContext = dataContext;
        Logger = logger;
    }

    /// <inheritdoc/>
    public virtual void InitializeContext(Guid entityId)
    {
        Guard.AgainstEmptyGuid(entityId, nameof(entityId));
        _ownerEntityId = entityId;
    }

    /// <inheritdoc/>
    public virtual async Task<TComponent> Get()
    {
        try
        {
            var query = DataContext.Query()
                .With<TComponent>(c => c.OwnerEntityId == OwnerEntityId);
            var componentsByEntity = await query.ToEntityComponents();

            if (!componentsByEntity.TryGetValue(OwnerEntityId, out var entityComponents))
            {
                throw new EntityNotFoundException(OwnerEntityId, typeof(TComponent).Name);
            }

            var component = entityComponents.Get<TComponent>();
            if (component == null)
            {
                throw new EntityNotFoundException(OwnerEntityId, typeof(TComponent).Name);
            }

            return component;
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            ErrorHandlingPolicy.WrapAndRethrow(
                ex,
                $"Failed to retrieve {typeof(TComponent).Name}",
                (msg, inner) => new ComponentOperationException(
                    typeof(TComponent).Name,
                    "GET",
                    msg,
                    inner),
                new { ComponentType = typeof(TComponent).Name, OwnerEntityId });
            throw; // Unreachable, but required by compiler
        }
    }

    /// <inheritdoc/>
    async Task<IComponent> IComponentHandler.Get()
    {
        return await Get();
    }

    /// <summary>
    /// Gets the component or returns null if not found.
    /// </summary>
    /// <returns>The component or null.</returns>
    protected async Task<TComponent?> GetOrDefault()
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

    /// <summary>
    /// Gets the component or throws if not found.
    /// </summary>
    /// <returns>The component.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the component is not found.</exception>
    protected async Task<TComponent> GetRequired()
    {
        return await Get();
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
    /// Attempts to get a component of the specified type without throwing exceptions.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <returns>The component if found, null otherwise.</returns>
    protected async Task<T?> TryGetComponent<T>() where T : class, IComponent, new()
    {
        try
        {
            var query = DataContext.Query()
                .With<T>(c => c.OwnerEntityId == OwnerEntityId);
            var result = await query.ToEntityComponents();
            
            if (result.TryGetValue(OwnerEntityId, out var components))
            {
                return components.Get<T>();
            }
            return null;
        }
        catch (EntityNotFoundException)
        {
            // Expected when component doesn't exist
            return null;
        }
        catch (InvalidOperationException ex)
        {
            // Log and return null for query issues
            Logger.LogWarning(ex, "Failed to query component {ComponentType} for entity {EntityId}", typeof(T).Name, OwnerEntityId);
            return null;
        }
        catch (Exception ex)
        {
            // Log unexpected exceptions but don't rethrow to maintain method contract
            Logger.LogError(ex, "Unexpected error retrieving component {ComponentType} for entity {EntityId}", typeof(T).Name, OwnerEntityId);
            return null;
        }
    }

    /// <summary>
    /// Attempts to get a component of the specified type and returns a tuple indicating success.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <returns>A tuple containing success flag and the component if found.</returns>
    protected async Task<(bool success, T? component)> TryGet<T>() where T : class, IComponent, new()
    {
        var component = await TryGetComponent<T>();
        return (component != null, component);
    }

    /// <summary>
    /// Gets all child components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of child components to retrieve.</typeparam>
    /// <returns>List of child components if found, null otherwise.</returns>
    protected async Task<List<T>?> Children<T>() where T : class, IComponent, new()
    {
        try 
        {
            var childIds = await DataContext.Children(OwnerEntityId);
            if (childIds == null || childIds.Count == 0)
                return null;
                
            var query = DataContext.Query()
                .With<T>(c => childIds.Contains(c.OwnerEntityId));
            var result = await query.ToEntityComponents();
            
            var children = new List<T>();
            foreach (var kvp in result)
            {
                var child = kvp.Value.Get<T>();
                if (child != null)
                    children.Add(child);
            }
            
            return children.Count > 0 ? children : null;
        }
        catch
        {
            return null;
        }
    }
}

