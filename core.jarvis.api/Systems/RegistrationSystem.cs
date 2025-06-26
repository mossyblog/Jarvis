using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace core.jarvis.api.Systems;

/// <summary>
/// System that orchestrates user registration workflow.
/// Creates Account and SecurityProfile components atomically.
/// Returns a list containing both created components.
/// </summary>
public class RegistrationSystem
{
    private readonly IDataContext _dataContext;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly ISecurityAuditService _securityAudit;
    private readonly ILogger<RegistrationSystem> _logger;

    public RegistrationSystem(
        IDataContext dataContext,
        IPasswordPolicyService passwordPolicy,
        ISecurityAuditService securityAudit,
        ILogger<RegistrationSystem> logger)
    {
        _dataContext = dataContext;
        _passwordPolicy = passwordPolicy;
        _securityAudit = securityAudit;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user from JSON input.
    /// Returns a list containing [Account, SecurityProfile] components.
    /// </summary>
    public async Task<List<IComponent>> RegisterUser(string requestBody, string? ipAddress)
    {
        // 1. Parse and validate request
        var request = ParseRegistrationRequest(requestBody);
        await ValidateRegistration(request);
        
        // 2. Check email availability
        await CheckEmailAvailability(request.Email);
        
        // 3. Generate new entity ID for the user
        var userEntityId = Guid.NewGuid();
        
        // TODO: Wrap in transaction when available
        
        // 4. Create account component through handler
        var accountHandler = _dataContext.For<AccountHandler>(userEntityId);
        var account = await accountHandler.CreateAccount(new Account
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            Password = string.Empty, // Never store plain password
            AuthMethod = "password",
            IsActive = true,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        
        // 5. Create security profile using handler
        var profileHandler = _dataContext.For<AccountProfileHandler>(userEntityId);
        var profile = await profileHandler.CreateWithDefaults(request.Email);
        
        // 6. Update profile with name if provided
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            var updated = profile with 
            { 
                Name = request.FullName.Trim(),
                UpdatedAt = DateTime.UtcNow
            };
            await _dataContext.Commit(updated);
            profile = updated;
        }
        
        // 7. Log successful registration
        await _securityAudit.LogSuccessfulAuthentication(
            userEntityId,
            request.Email,
            ipAddress ?? "unknown",
            "Registration"
        );
        
        _logger.LogInformation("User registered successfully: {Email}", request.Email);
        
        // 8. Return both components as a flat list
        return new List<IComponent> { account, profile };
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
        var existingAccounts = await _dataContext.Query()
            .WithAll<Account>(a => a.Email.ToLower() == email.ToLower())
            .ToEntityIds();
            
        if (existingAccounts.Any())
        {
            _logger.LogInformation("Registration attempted with existing email: {Email}", email);
            throw new BusinessRuleException("EMAIL_EXISTS", "EMAIL_EXISTS");
        }
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