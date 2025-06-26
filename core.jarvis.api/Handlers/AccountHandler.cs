using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing Account components.
/// </summary>
public class AccountHandler : ComponentHandler<Account>
{
    public AccountHandler(
        IDataContext dataContext,
        ILogger<AccountHandler> logger)
        : base(dataContext, logger)
    {
    }


    /// <summary>
    /// Activates this account.
    /// </summary>
    public async Task<Account> Activate()
    {
        var account = await GetOrDefault() ?? throw new InvalidOperationException("Account component not found");
        
        if (account.IsActive)
        {
            Logger.LogInformation("Account {AccountId} is already active", OwnerEntityId);
            return account;
        }

        var activated = account with 
        { 
            IsActive = true, 
            UpdatedAt = DateTime.UtcNow 
        };

        await DataContext.Commit(activated);
        Logger.LogInformation("Activated account {AccountId}", OwnerEntityId);
        return activated;
    }

    /// <summary>
    /// Deactivates this account.
    /// </summary>
    public async Task<Account> Deactivate()
    {
        var account = await GetOrDefault() ?? throw new InvalidOperationException("Account component not found");
        
        if (!account.IsActive)
        {
            Logger.LogInformation("Account {AccountId} is already inactive", OwnerEntityId);
            return account;
        }

        var deactivated = account with 
        { 
            IsActive = false, 
            UpdatedAt = DateTime.UtcNow 
        };

        await DataContext.Commit(deactivated);
        Logger.LogInformation("Deactivated account {AccountId}", OwnerEntityId);
        return deactivated;
    }

}