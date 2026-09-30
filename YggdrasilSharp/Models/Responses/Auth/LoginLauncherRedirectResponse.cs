using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

/// <summary>
/// Represents the response body returned to the launcher when a login attempt requires a redirect,
/// such as when the user must complete two-factor authentication before signing in.
/// </summary>
public class LoginLauncherRedirectResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the user that must complete the additional step.
    /// </summary>
    [JsonPropertyName("UserId")]
    public string UserId { get; set; }  = string.Empty;
    
    /// <summary>
    /// Gets or sets the session token of the interrupted login attempt.
    /// </summary>
    [JsonPropertyName("Token")]
    public string? Token { get; set; } 
    
    /// <summary>
    /// Gets or sets the URL the launcher must redirect the user to in order to continue the login.
    /// </summary>
    [JsonPropertyName("Url")]
    public string Url { get; set; }  = string.Empty;
}
