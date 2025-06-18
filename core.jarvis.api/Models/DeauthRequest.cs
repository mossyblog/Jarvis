using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a deauthentication request.
/// </summary>
public record DeauthRequest : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The session ID to deauthenticate.
    /// </summary>
    public Guid SessionId { get; init; }
    
    /// <summary>
    /// Reason for deauthentication.
    /// </summary>
    public string? Reason { get; init; }
}