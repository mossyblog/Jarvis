using System.Linq;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for token validation operations.
/// </summary>
public class TokenValidationHandler : ComponentHandler<TokenValidation>
{
    private readonly IServiceProvider _serviceProvider;

    public TokenValidationHandler(
        IDataContext dataContext,
        ILogger<TokenValidationHandler> logger,
        IServiceProvider serviceProvider)
        : base(dataContext, logger)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Validates the token specified in the bound TokenValidation component.
    /// </summary>
    public async Task<TokenValidation> ValidateToken()
    {
        var validationRequest = await GetOrDefault();
        if (validationRequest == null)
        {
            return new TokenValidation
            {
                OwnerEntityId = OwnerEntityId,
                IsValid = false,
                ErrorMessage = "Validation request not found"
            };
        }

        // Extract the token from claims
        var token = validationRequest.Claims?.GetValueOrDefault("token");
        if (string.IsNullOrEmpty(token))
        {
            return validationRequest with
            {
                IsValid = false,
                ErrorMessage = "No token provided",
                UpdatedAt = DateTime.UtcNow
            };
        }

        try
        {
            // Validate the JWT token
            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
            var principal = tokenService.ValidateToken(token);
            if (principal == null)
            {
                return validationRequest with
                {
                    IsValid = false,
                    ErrorMessage = "Invalid or expired token",
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Extract claims
            var userIdClaim = principal.FindFirst("sub")?.Value;
            var sessionIdClaim = principal.FindFirst("session_id")?.Value;
            var expClaim = principal.FindFirst("exp")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return validationRequest with
                {
                    IsValid = false,
                    ErrorMessage = "Invalid user ID in token",
                    UpdatedAt = DateTime.UtcNow
                };
            }

            DateTime? expiresAt = null;
            if (long.TryParse(expClaim, out var exp))
            {
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            }

            // Return validated result
            var result = validationRequest with
            {
                IsValid = true,
                ExpiresAt = expiresAt,
                Claims = new Dictionary<string, string>
                {
                    ["sub"] = userIdClaim!,
                    ["session_id"] = sessionIdClaim ?? string.Empty,
                    ["email"] = principal.FindFirst("email")?.Value ?? string.Empty
                },
                ErrorMessage = null,
                UpdatedAt = DateTime.UtcNow
            };

            await DataContext.Commit(result);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during token validation");
            return validationRequest with
            {
                IsValid = false,
                ErrorMessage = "Validation error occurred",
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}