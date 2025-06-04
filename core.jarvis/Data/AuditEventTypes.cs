namespace core.jarvis.Data;

/// <summary>
/// Provides standardized audit event type constants.
/// </summary>
public static class AuditEventTypes
{
    // Component lifecycle events
    public const string ComponentCreated = "COMPONENT_CREATED";
    public const string ComponentUpdated = "COMPONENT_UPDATED";
    public const string ComponentDeleted = "COMPONENT_DELETED";
    public const string ComponentVersionConflict = "COMPONENT_VERSION_CONFLICT";
    public const string ComponentConcurrencyConflict = "COMPONENT_CONCURRENCY_CONFLICT";

    // Entity lifecycle events
    public const string EntityCreated = "ENTITY_CREATED";
    public const string EntityUpdated = "ENTITY_UPDATED";
    public const string EntityDeleted = "ENTITY_DELETED";
    public const string EntityQueried = "ENTITY_QUERIED";
    public const string EntityNotFound = "ENTITY_NOT_FOUND";

    // Relationship events
    public const string RelationshipCreated = "RELATIONSHIP_CREATED";
    public const string RelationshipRemoved = "RELATIONSHIP_REMOVED";
    public const string RelationshipQueried = "RELATIONSHIP_QUERIED";
    
    // GraphQL events
    public const string GraphQLQuery = "GRAPHQL_QUERY";
    public const string GraphQLMutation = "GRAPHQL_MUTATION";
    public const string GraphQLError = "GRAPHQL_ERROR";
    public const string GraphQLUnauthorized = "GRAPHQL_UNAUTHORIZED";
    
    // Security events
    public const string AccessDenied = "ACCESS_DENIED";
    public const string AuthenticationFailed = "AUTHENTICATION_FAILED";
    public const string AuthorizationFailed = "AUTHORIZATION_FAILED";
    public const string InvalidToken = "INVALID_TOKEN";
    
    // Data integrity events
    public const string DataValidationFailed = "DATA_VALIDATION_FAILED";
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    
    // System events
    public const string SystemError = "SYSTEM_ERROR";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string TransactionStarted = "TRANSACTION_STARTED";
    public const string TransactionCommitted = "TRANSACTION_COMMITTED";
    public const string TransactionRolledBack = "TRANSACTION_ROLLED_BACK";
    
    // Snapshot events
    public const string SnapshotCreated = "SNAPSHOT_CREATED";
    public const string SnapshotRestored = "SNAPSHOT_RESTORED";
    public const string SnapshotFailed = "SNAPSHOT_FAILED";
    
    // Handler events
    public const string HandlerExecuted = "HANDLER_EXECUTED";
    public const string HandlerFailed = "HANDLER_FAILED";
    public const string HandlerValidationFailed = "HANDLER_VALIDATION_FAILED";

    /// <summary>
    /// Creates a component-specific event type.
    /// </summary>
    /// <param name="componentType">The component type name.</param>
    /// <param name="action">The action performed.</param>
    /// <returns>A formatted event type string.</returns>
    public static string ForComponent(string componentType, string action)
    {
        return $"{componentType.ToUpperInvariant()}_{action.ToUpperInvariant()}";
    }
    
    /// <summary>
    /// Creates a handler-specific event type.
    /// </summary>
    /// <param name="handlerType">The handler type name.</param>
    /// <param name="operation">The operation performed.</param>
    /// <returns>A formatted event type string.</returns>
    public static string ForHandler(string handlerType, string operation)
    {
        return $"HANDLER_{handlerType.ToUpperInvariant()}_{operation.ToUpperInvariant()}";
    }
}