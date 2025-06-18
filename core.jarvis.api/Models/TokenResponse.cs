using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a token response.
/// </summary>
public record TokenResponse : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// JWT access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;
    
    /// <summary>
    /// Refresh token for getting new tokens.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
    
    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }
    
    /// <summary>
    /// Token type (typically "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";
}