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
}

