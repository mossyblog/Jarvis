namespace core.jarvis.api.Models;

/// <summary>
/// Token validation result model.
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
}