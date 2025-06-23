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
    /// </summary>
    public async Task<bool> AllowsAction(string action)
    {
        var permission = await GetOrDefault();
        return permission?.Actions.Contains(action) ?? false;
    }
}