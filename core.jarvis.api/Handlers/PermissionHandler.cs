using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing Permission components.
/// </summary>
public class PermissionHandler : ComponentHandler<Permission>
{
    public PermissionHandler(
        IDataContext dataContext,
        ILogger<PermissionHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Checks if this permission grants a specific action.
    /// Verifies if the action string is contained in the permission's Actions collection.
    /// </summary>
    /// <param name="action">The action string to check for authorization</param>
    /// <returns>True if the permission grants the specified action; false if permission not found or action not granted</returns>
    public async Task<bool> AllowsAction(string action)
    {
        var permission = await GetOrDefault();
        return permission?.Actions.Contains(action) ?? false;
    }
}