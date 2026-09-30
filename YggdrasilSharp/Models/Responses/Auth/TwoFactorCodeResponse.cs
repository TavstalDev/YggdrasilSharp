using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

/// <summary>
/// Represents the response body returned when a user successfully generated a two-factor
/// authentication code and its recovery codes.
/// </summary>
public class TwoFactorCodeResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the user the code was generated for.
    /// </summary>
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the email address of the user the code was generated for.
    /// </summary>
    [JsonPropertyName("Email")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the single-use recovery codes that can replace the two-factor code.
    /// </summary>
    [JsonPropertyName("RecoveryCodes")]
    public IEnumerable<string>? RecoveryCodes { get; set; }
}
