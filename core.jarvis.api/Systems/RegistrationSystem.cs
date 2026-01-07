using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace core.jarvis.api.Systems;

/// <summary>
/// System that orchestrates user registration workflow with full transaction support.
/// Creates Account and SecurityProfile components atomically within a database transaction.
/// Ensures data consistency by rolling back all changes if any operation fails.
/// Returns a list containing both created components on successful registration.
/// </summary>
public class RegistrationSystem
{
    /// <summary>
    /// BCrypt cost factor for password hashing during registration.
    /// Should match the cost factor used in PasswordPolicyService.
    /// </summary>
    private const int BCryptCostFactor = 12;

    /// <summary>
    /// Maximum allowed length for user full name field.
    /// Prevents excessively long names that could impact database performance.
    /// </summary>
    private const int MaxFullNameLength = 255;

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
    /// All operations are performed atomically within a database transaction.
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

        // 4. Execute registration within a transaction for data consistency
        return await _dataContext.ExecuteInTransaction(async () =>
        {
            _logger.LogDebug("Starting transactional user registration for email: {Email}", request.Email);

            // Create account component through handler
            var accountHandler = _dataContext.For<AccountHandler>(userEntityId);
            var account = await accountHandler.CreateAccount(new Account
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCryptCostFactor),
                Password = string.Empty, // Never store plain password
                AuthMethod = "password",
                IsActive = true,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            });

            // Create security profile using handler with the provided name
            var profileHandler = _dataContext.For<AccountProfileHandler>(userEntityId);
            var profile = await profileHandler.CreateWithDefaults(request.Email, request.FullName);

            // Log successful registration - wrapped in try-catch to prevent transaction failure
            try
            {
                await _securityAudit.LogSuccessfulAuthentication(
                    userEntityId,
                    request.Email,
                    ipAddress ?? "unknown",
                    "Registration"
                );
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the registration
                _logger.LogWarning(ex, "Failed to log security audit event for registration, but continuing with registration");
            }

            _logger.LogInformation("User registered successfully within transaction: {Email}", request.Email);

            // Return both components as a flat list
            return new List<IComponent> { account, profile };
        });
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
        if (!string.IsNullOrWhiteSpace(request.FullName) && request.FullName.Length > MaxFullNameLength)
        {
            errors["fullName"] = new[] { $"Name is too long (max {MaxFullNameLength} characters)" };
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }

    private async Task CheckEmailAvailability(string email)
    {
        // Normalize email to lowercase for comparison
        var normalizedEmail = email.ToLower();

        // Get all accounts and filter in memory to avoid SQL translation issues
        var allAccounts = await _dataContext.Query()
            .WithAll<Account>(Filter<Account>.All())
            .ToEntityComponents();

        var existingAccount = allAccounts
            .Select(kvp => kvp.Value.Get<Account>())
            .FirstOrDefault(a => a != null && a.Email.ToLower() == normalizedEmail);

        if (existingAccount != null)
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
