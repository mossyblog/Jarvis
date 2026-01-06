using core.jarvis.Exceptions;

namespace core.jarvis.Data;

/// <summary>
/// Represents the core data structure for an entity within the Jarvis system.
/// Holds identity, naming, and relationship identifiers.
/// Properties are automatically mapped to snake_case by PgTable.
/// </summary>
public class Entity
{
    private IDataContext? _context;

    public Entity()
    {
    }

    public Entity(Guid id)
    {
        Id = id;
    }

    public Entity(Guid id, string name) : this(id)
    {
        Name = name ?? string.Empty;
    }

    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the entity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent entity ID. Maps to parentid in database.
    /// </summary>
    public Guid ParentId { get; set; } // Guid.Empty = no parent

    /// <summary>
    /// Array of child entity IDs. Maps to children_ids in database.
    /// </summary>
    public Guid[] ChildrenIds { get; set; } = new Guid[0];

    /// <summary>
    /// Sets the DataContext for this entity, enabling component resolution.
    /// </summary>
    internal void SetContext(IDataContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Asynchronously retrieves a component of the specified type for this entity.
    /// </summary>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <returns>The component instance or null if not found.</returns>
    public async Task<T?> Get<T>() where T : class, IComponent, new()
    {
        if (_context == null)
            throw new InvalidOperationException($"Entity {Id} is not bound to a DataContext. Entity must be retrieved through a query.");

        try
        {
            var handler = _context.For(typeof(T), Id) as IComponentHandler<T>;
            if (handler == null)
                throw new ComponentNotFoundException($"No handler found for component type {typeof(T).Name}");

            return await handler.Get();
        }
        catch (EntityNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Asynchronously checks if the entity has a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <returns>True if the component exists, false otherwise.</returns>
    public async Task<bool> Has<T>() where T : class, IComponent, new()
    {
        var component = await Get<T>();
        return component != null;
    }
}