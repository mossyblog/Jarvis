namespace core.jarvis.api.Models;

/// <summary>
/// Refresh token request model.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Validate token request model.
/// </summary>
public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}