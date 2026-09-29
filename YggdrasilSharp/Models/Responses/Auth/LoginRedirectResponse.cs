using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

/// <summary>
/// Represents the response body returned when a login attempt requires a redirect,
/// such as when the user must complete two-factor authentication before signing in.
/// </summary>
public class LoginRedirectResponse : ResponseBase
{
    [JsonPropertyName("Email")]
    public string Email { get; set; }
    
    [JsonPropertyName("Url")]
    public string Url { get; set; }
}