using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing User components.
/// </summary>
public class UserHandler : ComponentHandler<User>
{
    public UserHandler(
        IDataContext dataContext,
        ILogger<UserHandler> logger)
        : base(dataContext, logger)
    {
    }


    /// <summary>
    /// Activates this user account.
    /// </summary>
    public async Task<User> Activate()
    {
        var user = await GetOrDefault() ?? throw new InvalidOperationException("User component not found");
        
        if (user.IsActive)
        {
            Logger.LogInformation("User {UserId} is already active", OwnerEntityId);
            return user;
        }

        var activated = user with 
        { 
            IsActive = true, 
            UpdatedAt = DateTime.UtcNow 
        };

        await DataContext.Commit(activated);
        Logger.LogInformation("Activated user {UserId}", OwnerEntityId);
        return activated;
    }

    /// <summary>
    /// Deactivates this user account.
    /// </summary>
    public async Task<User> Deactivate()
    {
        var user = await GetOrDefault() ?? throw new InvalidOperationException("User component not found");
        
        if (!user.IsActive)
        {
            Logger.LogInformation("User {UserId} is already inactive", OwnerEntityId);
            return user;
        }

        var deactivated = user with 
        { 
            IsActive = false, 
            UpdatedAt = DateTime.UtcNow 
        };

        await DataContext.Commit(deactivated);
        Logger.LogInformation("Deactivated user {UserId}", OwnerEntityId);
        return deactivated;
    }

}