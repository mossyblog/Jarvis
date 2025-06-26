using System.Text.Json;
using System.Linq;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for user registration flow.
/// Orchestrates creation of Account and SecurityProfile components.
/// </summary>
public class RegistrationHandler : IComponentHandler
{
    private readonly ILogger<RegistrationHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly ISecurityAuditService _securityAudit;
    
    public Guid OwnerEntityId { get; set; }

    public IDataContext DataContext { get; private set; }

    public RegistrationHandler(
        IDataContext dataContext,
        ILogger<RegistrationHandler> logger,
        IServiceProvider serviceProvider)
    {
        DataContext = dataContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _passwordPolicy = serviceProvider.GetRequiredService<IPasswordPolicyService>();
        _securityAudit = serviceProvider.GetRequiredService<ISecurityAuditService>();
    }

    /// <summary>
    /// Registers a new user from JSON input.
    /// Creates both Account and SecurityProfile in a transaction.
    /// </summary>
    public async Task<RegistrationResult> RegisterFromJson(string requestBody, string? ipAddress)
    {
        try
        {
            // Parse request
            var request = ParseRegistrationRequest(requestBody);
            
            // Validate request
            await ValidateRegistration(request);
            
            // Check if email already exists
            await CheckEmailAvailability(request.Email);
            
            // Create account and profile
            // TODO: Use transaction when InTransaction is implemented
            
            // Generate new entity ID for the user
            var userEntityId = Guid.NewGuid();
            
            // Create account
            var account = await CreateAccount(DataContext, userEntityId, request, ipAddress);
            
            // Create security profile
            var profile = await CreateSecurityProfile(DataContext, userEntityId, request);
            
            // Log successful registration
            await _securityAudit.LogSuccessfulAuthentication(
                userEntityId,
                request.Email,
                ipAddress ?? "unknown",
                "Registration"
            );
            
            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            
            return new RegistrationResult
            {
                Success = true,
                AccountId = userEntityId,
                Email = account.Email,
                Message = "Registration successful"
            };
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Registration validation failed: {Errors}", ex.Errors);
            return new RegistrationResult
            {
                Success = false,
                Message = "Validation failed",
                Errors = ex.Errors
            };
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning("Registration business rule failed: {Message}", ex.Message);
            return new RegistrationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed unexpectedly");
            return new RegistrationResult
            {
                Success = false,
                Message = "Registration failed"
            };
        }
    }

    private RegistrationRequest ParseRegistrationRequest(string requestBody)
    {
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["body"] = new[] { "Request body is required" } });
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var request = JsonSerializer.Deserialize<RegistrationRequest>(requestBody, options);
            if (request == null)
            {
                throw new ValidationException(new Dictionary<string, string[]> { ["body"] = new[] { "Invalid request format" } });
            }
            
            return request;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Failed to parse registration request: {Message}", ex.Message);
            throw new ValidationException(new Dictionary<string, string[]> { ["body"] = new[] { "Invalid request format" } });
        }
    }

    private async Task ValidateRegistration(RegistrationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        
        // Validate email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = new[] { "Email is required" };
        }
        else if (!IsValidEmail(request.Email))
        {
            errors["email"] = new[] { "Invalid email format" };
        }
        
        // Validate password
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = new[] { "Password is required" };
        }
        else
        {
            var passwordResult = _passwordPolicy.ValidatePassword(request.Password);
            if (!passwordResult.IsValid && passwordResult.Errors.Any())
            {
                errors["password"] = passwordResult.Errors.ToArray();
            }
        }
        
        // Validate name (optional but if provided, must be valid)
        if (!string.IsNullOrWhiteSpace(request.FullName) && request.FullName.Length > 255)
        {
            errors["fullName"] = new[] { "Name is too long (max 255 characters)" };
        }
        
        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }

    private async Task CheckEmailAvailability(string email)
    {
        var existingAccounts = await DataContext.Query()
            .WithAll<Account>(a => a.Email.ToLower() == email.ToLower())
            .ToEntityIds();
            
        if (existingAccounts.Any())
        {
            _logger.LogInformation("Registration attempted with existing email: {Email}", email);
            throw new BusinessRuleException("EMAIL_EXISTS", "EMAIL_EXISTS");
        }
    }

    private async Task<Account> CreateAccount(IDataContext dataContext, Guid userEntityId, RegistrationRequest request, string? ipAddress)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = userEntityId,
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            Password = string.Empty, // Never store plain password
            AuthMethod = "password",
            IsActive = true,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        await dataContext.Commit(account);
        _logger.LogInformation("Created account for user {UserId} with email {Email}", userEntityId, account.Email);
        
        return account;
    }

    private async Task<SecurityProfile> CreateSecurityProfile(IDataContext dataContext, Guid userEntityId, RegistrationRequest request)
    {
        // Use AccountProfileHandler to create profile with defaults
        var profileHandler = dataContext.For<AccountProfileHandler>(userEntityId);
        var profile = await profileHandler.CreateWithDefaults(request.Email);
        
        // Update with any provided name
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            var updated = profile with 
            { 
                Name = request.FullName.Trim(),
                UpdatedAt = DateTime.UtcNow
            };
            await dataContext.Commit(updated);
            return updated;
        }
        
        return profile;
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Initializes the handler with an owner entity ID.
    /// </summary>
    public void InitializeContext(Guid ownerEntityId)
    {
        OwnerEntityId = ownerEntityId;
    }

    /// <summary>
    /// Not applicable for registration handler - throws NotSupportedException.
    /// </summary>
    public Task<IComponent> Get()
    {
        throw new NotSupportedException("RegistrationHandler does not support Get operation");
    }
}

/// <summary>
/// Request model for user registration
/// </summary>
public class RegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
}

/// <summary>
/// Result of registration attempt
/// </summary>
public class RegistrationResult
{
    public bool Success { get; set; }
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
}