using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using BCrypt.Net;
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
    /// Activates this account by setting IsActive to true.
    /// Used to enable account access after creation or reactivation.
    /// </summary>
    /// <returns>The updated Account with IsActive set to true</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Account component is not found</exception>
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
            LastUpdated = DateTime.UtcNow 
        };

        await DataContext.TryCommit(activated);
        Logger.LogInformation("Activated account {AccountId}", OwnerEntityId);
        return activated;
    }

    /// <summary>
    /// Deactivates this account by setting IsActive to false.
    /// Used to prevent account access while maintaining the account record.
    /// </summary>
    /// <returns>The updated Account with IsActive set to false</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Account component is not found</exception>
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
            LastUpdated = DateTime.UtcNow 
        };

        await DataContext.TryCommit(deactivated);
        Logger.LogInformation("Deactivated account {AccountId}", OwnerEntityId);
        return deactivated;
    }

    /// <summary>
    /// Registers a new user account with proper validation and password hashing.
    /// Account starts as inactive and must be manually activated.
    /// </summary>
    public async Task<Account> Register(Account accountComponent)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(accountComponent.Email))
        {
            throw new ValidationException(new Dictionary<string, string[]> { { "email", new[] { "Email is required" } } });
        }
        
        if (string.IsNullOrWhiteSpace(accountComponent.Password))
        {
            throw new ValidationException(new Dictionary<string, string[]> { { "password", new[] { "Password is required" } } });
        }

        // Check if email already exists
        var existingAccountQuery = DataContext.Query()
            .With<Account>(a => a.Email == accountComponent.Email);
        var existingAccounts = await existingAccountQuery.ToEntityComponents();
        
        if (existingAccounts.Any())
        {
            throw new BusinessRuleException("EMAIL_EXISTS", "An account with this email already exists");
        }

        // Hash the password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(accountComponent.Password, 12);

        // Create the account component - starts INACTIVE
        var account = accountComponent with 
        { 
            Id = Guid.NewGuid(),
            OwnerEntityId = OwnerEntityId,
            PasswordHash = passwordHash,
            Password = "", // Clear plain password
            IsActive = false, // Starts inactive - must be manually activated
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.TryCommit(account);
        Logger.LogInformation("Registered new account {AccountId} for entity {EntityId} with email {Email} - INACTIVE", 
            account.Id, OwnerEntityId, account.Email);
        
        return account;
    }

    /// <summary>
    /// Creates a new account from an Account component.
    /// </summary>
    public async Task<Account> CreateAccount(Account newAccount)
    {
        // Ensure the account has the correct owner entity
        var account = newAccount with { OwnerEntityId = OwnerEntityId };
        
        await DataContext.TryCommit(account);
        Logger.LogInformation("Created account {AccountId} for entity {EntityId}", account.Id, OwnerEntityId);
        return account;
    }
}