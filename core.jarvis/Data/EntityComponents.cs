namespace core.jarvis.Data;

/// <summary>
/// Container for components belonging to a single entity.
/// </summary>
public class EntityComponents
{
    private readonly Dictionary<Type, IComponent> _components = new();

    /// <summary>
    /// Gets a component of the specified type if it exists.
    /// </summary>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <returns>The component instance or null if not found.</returns>
    public T? Get<T>() where T : class, IComponent
        => _components.TryGetValue(typeof(T), out var c) ? c as T : null;

    /// <summary>
    /// Checks if the entity has a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <returns>True if the component exists, false otherwise.</returns>
    public bool Has<T>() where T : IComponent
        => _components.ContainsKey(typeof(T));

    /// <summary>
    /// Internal method to add a component to the collection.
    /// </summary>
    internal void Add(Type type, IComponent component)
        => _components[type] = component;
}