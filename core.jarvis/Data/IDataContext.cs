namespace core.jarvis.Data;

/// <summary>
/// Provides the main entry point for handler-based operations on entities.
/// This interface enables fluent, plugin-safe access to component handlers
/// without the core project knowing about specific component types.
/// </summary>
public interface IDataContext
{
    /// <summary>
    /// Resolves a handler for a specific component type and entity.
    /// Used when the component type is only known at runtime.
    /// </summary>
    /// <param name="componentType">The type of component to handle.</param>
    /// <param name="entityId">The entity ID to operate on.</param>
    /// <returns>A component handler instance.</returns>
    IComponentHandler For(Type componentType, Guid entityId);

    /// <summary>
    /// Resolves a strongly-typed handler for a specific entity.
    /// This is the primary method for accessing component operations.
    /// </summary>
    /// <typeparam name="THandler">The handler type to resolve.</typeparam>
    /// <param name="entityId">The entity ID to operate on.</param>
    /// <returns>The resolved handler instance.</returns>
    THandler For<THandler>(Guid entityId) where THandler : class, IComponentHandler;

    /// <summary>
    /// Creates a query builder for cross-component entity queries.
    /// </summary>
    /// <returns>An entity query builder.</returns>
    IEntityQuery Query();

    /// <summary>
    /// Creates a new Entity with a generated ID.
    /// </summary>
    /// <returns>A new Entity instance with a unique ID.</returns>
    Entity NewEntity();

    /// <summary>
    /// Attempts to save a component through the centralized DataContext with optimistic concurrency control.
    /// Use this instead of direct Supabase client operations.
    /// </summary>
    /// <typeparam name="TComponent">The component type to save.</typeparam>
    /// <param name="component">The component instance to save.</param>
    /// <returns>True if the save succeeded, false if there was a concurrency conflict.</returns>
    Task<bool> TryCommit<TComponent>(TComponent component) 
        where TComponent : class, IComponent, new();

    /// <summary>
    ///  Commits a component through the centralized DataContext with audit/logging.
    /// </summary>
    /// <param name="component"></param>
    /// <typeparam name="TComponent"></typeparam>
    /// <returns></returns>
    Task Commit<TComponent>(TComponent component)
        where TComponent : class, IComponent, new();
    
    /// <summary>
    /// Removes components by entity ID through the centralized DataContext with audit/logging.
    /// Use this instead of direct Supabase client operations.
    /// </summary>
    /// <typeparam name="TComponent">The component type to remove.</typeparam>
    /// <param name="entityId">The entity ID whose components should be removed.</param>
    /// <returns>A task representing the remove operation.</returns>
    Task Remove<TComponent>(Guid entityId) 
        where TComponent : class, IComponent, new();

  

    /// <summary>
    /// Inserts an audit event through the centralized DataContext.
    /// Use this instead of direct Supabase client operations for audit logging.
    /// </summary>
    /// <typeparam name="TModel">The model type to insert (typically AuditEvent).</typeparam>
    /// <param name="model">The model instance to insert.</param>
    /// <returns>A task representing the insert operation.</returns>
    Task Insert<TModel>(TModel model) 
        where TModel : class, new();
    
    /// <summary>
    /// Creates a query builder for accessing component snapshots.
    /// </summary>
    /// <returns>A snapshot query builder.</returns>
    Query.ISnapshotQuery Snapshots();

    /// <summary>
    /// Creates a GraphQL query builder for executing arbitrary GraphQL queries.
    /// Requires authentication via WithAuth() before execution.
    /// </summary>
    /// <param name="query">The GraphQL query string</param>
    /// <returns>A GraphQL query builder for fluent configuration</returns>
    GraphQL.IGraphQLQuery GraphQL(string query);

    /// <summary>
    /// Gets the parent entity ID for the specified entity.
    /// Returns null if the entity has no parent.
    /// </summary>
    /// <param name="entityId">The entity to get the parent for.</param>
    /// <returns>The parent entity ID or null.</returns>
    Task<Guid?> Parent(Guid entityId);

    /// <summary>
    /// Gets all child entity IDs for the specified entity.
    /// Returns empty list if the entity has no children.
    /// </summary>
    /// <param name="entityId">The entity to get children for.</param>
    /// <returns>List of child entity IDs.</returns>
    Task<List<Guid>> Children(Guid entityId);

    /// <summary>
    /// Checks if the specified entity is a child of the parent entity.
    /// </summary>
    /// <param name="childId">The potential child entity ID.</param>
    /// <param name="parentId">The potential parent entity ID.</param>
    /// <returns>True if childId is a child of parentId.</returns>
    Task<bool> ChildOf(Guid childId, Guid parentId);

    /// <summary>
    /// Adds a parent-child relationship between two entities.
    /// Updates both the parent's children list and the child's parent reference.
    /// </summary>
    /// <param name="parentId">The parent entity ID.</param>
    /// <param name="childId">The child entity ID.</param>
    /// <param name="parentType">Optional parent entity type for validation.</param>
    /// <param name="childType">Optional child entity type for validation.</param>
    /// <returns>Task representing the operation.</returns>
    Task LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null);

    /// <summary>
    /// Removes a parent-child relationship between two entities.
    /// Updates both the parent's children list and the child's parent reference.
    /// </summary>
    /// <param name="parentId">The parent entity ID.</param>
    /// <param name="childId">The child entity ID.</param>
    /// <returns>Task representing the operation.</returns>
    Task UnlinkRelationship(Guid parentId, Guid childId);

    /// <summary>
    /// Gets all ancestor entity IDs for the specified entity (parent, grandparent, etc.).
    /// </summary>
    /// <param name="entityId">The entity to get ancestors for.</param>
    /// <returns>List of ancestor entity IDs ordered from immediate parent to root.</returns>
    Task<List<Guid>> Ancestors(Guid entityId);

    /// <summary>
    /// Gets all descendant entity IDs for the specified entity (children, grandchildren, etc.).
    /// </summary>
    /// <param name="entityId">The entity to get descendants for.</param>
    /// <returns>List of all descendant entity IDs.</returns>
    Task<List<Guid>> Descendants(Guid entityId);

    /// <summary>
    /// Emit a single event through the configured event emitter.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="event">The event to emit.</param>
    /// <returns>Task representing the emission operation.</returns>
    Task Emit<TEvent>(TEvent @event) where TEvent : Events.IEvent;

    /// <summary>
    /// Emit multiple events as a batch through the configured event emitter.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="events">The events to emit.</param>
    /// <returns>Task representing the batch emission operation.</returns>
    Task EmitBatch<TEvent>(IEnumerable<TEvent> events) where TEvent : Events.IEvent;

    /// <summary>
    /// Ensures the Entity table exists for relationship management.
    /// This method is public to allow test initialization to create the table.
    /// </summary>
    /// <returns>Task representing the table creation operation.</returns>
    Task EnsureEntityTableExists();

    /// <summary>
    /// Executes the provided operation within a database transaction.
    /// If the operation succeeds, the transaction is committed.
    /// If an exception is thrown, the transaction is rolled back.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute within the transaction.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation);

    /// <summary>
    /// Executes the provided operation within a database transaction.
    /// If the operation succeeds, the transaction is committed.
    /// If an exception is thrown, the transaction is rolled back.
    /// </summary>
    /// <param name="operation">The operation to execute within the transaction.</param>
    /// <returns>Task representing the transaction operation.</returns>
    Task ExecuteInTransaction(Func<Task> operation);
}