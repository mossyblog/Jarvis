using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing NavigationItem components.
/// </summary>
public class NavigationItemHandler : ComponentHandler<NavigationItem>
{
    public NavigationItemHandler(IDataContext dataContext, ILogger<NavigationItemHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Saves a navigation item.
    /// </summary>
    public async Task<NavigationItem> Save()
    {
        var item = await GetOrDefault() ?? throw new InvalidOperationException("NavigationItem component not found");
        
        item = item with { OwnerEntityId = OwnerEntityId, LastUpdated = DateTime.UtcNow };
        await DataContext.TryCommit(item);
        return item;
    }

}