using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

/// <summary>
/// Represents the response body returned when two-factor authentication is enabled for a user,
/// containing the shared secret used to generate authentication codes.
/// </summary>
public class TwoFactorSecretResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the user the secret belongs to.
    /// </summary>
    [JsonPropertyName("UserId")]
    public string UserId { get; set; }  = string.Empty;
    
    /// <summary>
    /// Gets or sets the email address of the user the secret belongs to.
    /// </summary>
    [JsonPropertyName("Email")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the shared secret used to generate two-factor authentication codes.
    /// </summary>
    [JsonPropertyName("Secret")]
    public string Secret { get; set; } = string.Empty;
}
