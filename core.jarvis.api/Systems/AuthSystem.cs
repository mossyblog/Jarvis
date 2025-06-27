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
    /// Authenticates a user and returns auth token
    /// </summary>
    public async Task<AuthToken> AuthenticateUser(string requestBody, string? ipAddress, string? userAgent)
    {
        // All validation and logic handled by handler
        var authEntityId = Guid.NewGuid();
        var authHandler = _dataContext.For<AuthHandler>(authEntityId);
        var authToken = await authHandler.AuthenticateFromJson(requestBody, ipAddress, userAgent);

        if (authToken == null || string.IsNullOrEmpty(authToken.AccessToken) || authToken.OwnerEntityId == Guid.Empty)
        {
            throw new UnauthorizedException("Authentication failed");
        }

        _logger.LogInformation("User authenticated successfully: {EntityId}", authToken.OwnerEntityId);
        return authToken;
    }

    /// <summary>
    /// Refreshes an authentication token
    /// </summary>
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
    /// Validates a token and returns validation result
    /// </summary>
    public Task<TokenValidationResult> ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ValidationException("Token is required");
        }

        var principal = _tokenService.ValidateToken(token);
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