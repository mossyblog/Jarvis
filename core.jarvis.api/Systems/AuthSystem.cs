using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.Exceptions;
using core.jarvis.Data;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Systems;

/// <summary>
/// System for orchestrating authentication operations
/// </summary>
public class AuthSystem
{
    private readonly IDataContext _dataContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthSystem> _logger;

    public AuthSystem(
        IDataContext dataContext, 
        ITokenService tokenService,
        ILogger<AuthSystem> logger)
    {
        _dataContext = dataContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns auth token.
    /// Delegates to AuthHandler for authentication logic and validates the result.
    /// </summary>
    /// <param name="accountCredentials">Account credentials containing email, password, and optional metadata</param>
    /// <returns>AuthToken with access and refresh tokens if authentication succeeds</returns>
    /// <exception cref="ValidationException">Thrown when accountCredentials is null</exception>
    /// <exception cref="UnauthorizedException">Thrown when authentication fails</exception>
    public async Task<AuthToken> AuthenticateUser(Account accountCredentials)
    {
        // Validate input component
        if (accountCredentials == null)
        {
            throw new ValidationException("Account credentials are required");
        }

        // All authentication logic handled by handler
        var authEntityId = Guid.NewGuid();
        var authHandler = _dataContext.For<AuthHandler>(authEntityId);
        var authToken = await authHandler.Authenticate(accountCredentials);

        if (authToken == null || string.IsNullOrEmpty(authToken.AccessToken) || authToken.OwnerEntityId == Guid.Empty)
        {
            throw new UnauthorizedException("Authentication failed");
        }

        _logger.LogInformation("User authenticated successfully: {EntityId}", authToken.OwnerEntityId);
        return authToken;
    }

    /// <summary>
    /// Refreshes an authentication token using a valid refresh token.
    /// Delegates to AuthHandler for token refresh logic and validates the result.
    /// </summary>
    /// <param name="refreshToken">The refresh token string to use for generating new tokens</param>
    /// <returns>New AuthToken with fresh access and refresh tokens if refresh succeeds</returns>
    /// <exception cref="ValidationException">Thrown when refreshToken is null or empty</exception>
    /// <exception cref="UnauthorizedException">Thrown when token refresh fails</exception>
    public async Task<AuthToken> RefreshToken(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new ValidationException("Refresh token is required");
        }

        var authEntityId = Guid.NewGuid();
        var authHandler = _dataContext.For<AuthHandler>(authEntityId);
        var newToken = await authHandler.RefreshToken(refreshToken);

        if (newToken == null || string.IsNullOrEmpty(newToken.AccessToken))
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        return newToken;
    }

    /// <summary>
    /// Validates a token and returns validation result.
    /// Extracts claims information and validates JWT structure and signature.
    /// </summary>
    /// <param name="token">The JWT token string to validate</param>
    /// <returns>TokenValidationResult containing user information and validation status</returns>
    /// <exception cref="ValidationException">Thrown when token is null or empty</exception>
    /// <exception cref="UnauthorizedException">Thrown when token validation fails</exception>
    public Task<TokenValidationResult> ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ValidationException("Token is required");
        }

        var principal = _tokenService.Validate(token);
        if (principal == null)
        {
            _logger.LogWarning("Token validation failed");
            throw new UnauthorizedException("Invalid token");
        }

        // Extract claims
        var userId = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;
        var roles = principal.FindFirst("roles")?.Value;

        var result = new TokenValidationResult
        {
            IsValid = true,
            UserId = userId,
            Email = email,
            Roles = roles?.Split(',').ToList() ?? new List<string>()
        };

        return Task.FromResult(result);
    }
}