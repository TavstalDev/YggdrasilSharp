using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

/// <summary>
/// Represents the response body returned when a login attempt requires a redirect,
/// such as when the user must complete two-factor authentication before signing in.
/// </summary>
public class LoginRedirectResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the signed-in user.
    /// </summary>
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user that must complete the additional step.
    /// </summary>
    [JsonPropertyName("Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL the user must be redirected to in order to continue the login.
    /// </summary>
    [JsonPropertyName("Url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or initializes the session token that must be sent back when confirming the two-factor code.
    /// </summary>
    [Required]
    [StringLength(255)]
    public required string SessionToken { get; init; }
}
