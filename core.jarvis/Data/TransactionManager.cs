using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Manages database transactions and audit wrapping.
/// </summary>
public class TransactionManager : ITransactionManager
{
    private readonly IPgClient _pgClient;
    private readonly IAuditService _auditService;
    private readonly ILogger<TransactionManager> _logger;

    private readonly Dictionary<Guid, Guid> _pendingRelationships = new();

    public TransactionManager(
        IPgClient pgClient,
        IAuditService auditService,
        ILogger<TransactionManager> logger)
    {
        _pgClient = pgClient;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<T> ExecuteWithAudit<T>(Func<Task<T>> operation, Guid entityId, string operationName, object details)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await operation();

            await _auditService.LogEvent(
                AuditEventTypes.HandlerExecuted,
                entityId,
                new
                {
                    Operation = operationName,
                    Details = details,
                    Duration = (DateTime.UtcNow - startTime).TotalMilliseconds
                });

            return result;
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.HandlerFailed,
                entityId,
                ex,
                new { Operation = operationName, Details = details });

            throw;
        }
    }

    public async Task ExecuteWithAudit(Func<Task> operation, Guid entityId, string operationName, object details)
    {
        await ExecuteWithAudit(async () =>
        {
            await operation();
            return true;
        }, entityId, operationName, details);
    }

    public void TrackPendingRelationship(Guid childId, Guid parentId)
    {
        _pendingRelationships[childId] = parentId;
    }

    public async Task EnsureEntityTableExists()
    {
        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS entity (
                id UUID PRIMARY KEY,
                name TEXT DEFAULT '',
                parent_id UUID DEFAULT '00000000-0000-0000-0000-000000000000',
                children_ids UUID[] DEFAULT '{}'
            )";

        try
        {
            await _pgClient.Execute(createTableSql);
            _logger.LogDebug("Entity table ensured");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure entity table exists");
        }
    }

    public async Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation)
    {
        // For now, just execute the operation
        // Full transaction support requires connection-level transaction management
        try
        {
            _logger.LogDebug("Starting transaction");
            var result = await operation();
            _logger.LogDebug("Transaction completed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transaction failed");
            throw;
        }
    }

    public async Task ExecuteInTransaction(Func<Task> operation)
    {
        await ExecuteInTransaction(async () =>
        {
            await operation();
            return true;
        });
    }
}
