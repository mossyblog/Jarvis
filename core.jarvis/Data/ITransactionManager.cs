namespace core.jarvis.Data;

/// <summary>
/// Transaction management interface.
/// </summary>
public interface ITransactionManager
{
    Task<T> ExecuteWithAudit<T>(Func<Task<T>> operation, Guid entityId, string operationName, object details);
    Task ExecuteWithAudit(Func<Task> operation, Guid entityId, string operationName, object details);
    void TrackPendingRelationship(Guid childId, Guid parentId);
    Task EnsureEntityTableExists();
    Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation);
    Task ExecuteInTransaction(Func<Task> operation);
}
