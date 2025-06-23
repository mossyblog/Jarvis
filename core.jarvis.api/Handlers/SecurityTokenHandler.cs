using core.jarvis.api.Models;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing security tokens in the database.
/// </summary>
public class SecurityTokenHandler : ComponentHandler<SecurityToken>
{
    private readonly IEntityQuery _entityQuery;
    
    public SecurityTokenHandler(
        IDataContext dataContext,
        IEntityQuery entityQuery,
        ILogger<SecurityTokenHandler> logger) 
        : base(dataContext, logger)
    {
        _entityQuery = entityQuery;
    }
    
    /// <summary>
    /// Create a new security token.
    /// </summary>
    public async Task<SecurityToken> CreateAsync(SecurityToken token)
    {
        await DataContext.Commit(token);
        return token;
    }
    
    /// <summary>
    /// Update an existing security token.
    /// </summary>
    public async Task UpdateAsync(SecurityToken token)
    {
        await DataContext.Commit(token);
    }
    
    /// <summary>
    /// Get a security token by its entity ID.
    /// </summary>
    public async Task<SecurityToken?> GetAsync(Guid entityId)
    {
        try
        {
            InitializeContext(entityId);
            return await GetOrDefault();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting security token: {EntityId}", entityId);
            return null;
        }
    }
    
    /// <summary>
    /// Find a security token by session ID.
    /// </summary>
    public async Task<SecurityToken?> GetBySessionIdAsync(Guid sessionId)
    {
        try
        {
            // Query for entities that have SecurityToken components
            var entityIds = await _entityQuery
                .WithAll<SecurityToken>(t => true) // Get all security tokens
                .ToEntityIds();
            
            foreach (var entityId in entityIds)
            {
                InitializeContext(entityId);
                var token = await GetOrDefault();
                if (token != null && token.SessionId == sessionId)
                {
                    return token;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting security token by session ID: {SessionId}", sessionId);
            return null;
        }
    }
    
    /// <summary>
    /// Find a security token by user ID.
    /// </summary>
    public async Task<SecurityToken?> GetByUserIdAsync(Guid userId)
    {
        try
        {
            // Query for entities that have SecurityToken components
            var entityIds = await _entityQuery
                .WithAll<SecurityToken>(t => true) // Get all security tokens
                .ToEntityIds();
            
            foreach (var entityId in entityIds)
            {
                InitializeContext(entityId);
                var token = await GetOrDefault();
                if (token != null && token.UserId == userId && !token.IsRevoked)
                {
                    return token;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting security token by user ID: {UserId}", userId);
            return null;
        }
    }
    
    /// <summary>
    /// Get all active (non-revoked) security tokens.
    /// </summary>
    public async Task<IEnumerable<SecurityToken>> GetActiveTokensAsync()
    {
        try
        {
            var entityIds = await _entityQuery
                .WithAll<SecurityToken>(t => true) // Get all security tokens
                .ToEntityIds();
            
            var activeTokens = new List<SecurityToken>();
            
            foreach (var entityId in entityIds)
            {
                InitializeContext(entityId);
                var token = await GetOrDefault();
                if (token != null && !token.IsRevoked && token.RefreshExpiresAt > DateTime.UtcNow)
                {
                    activeTokens.Add(token);
                }
            }
            
            return activeTokens;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting active security tokens");
            return Enumerable.Empty<SecurityToken>();
        }
    }
    
    /// <summary>
    /// Revoke a security token.
    /// </summary>
    public async Task<bool> RevokeTokenAsync(Guid tokenId)
    {
        try
        {
            InitializeContext(tokenId);
            var token = await GetOrDefault();
            if (token == null)
            {
                return false;
            }
            
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            
            await UpdateAsync(token);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error revoking security token: {TokenId}", tokenId);
            return false;
        }
    }
}