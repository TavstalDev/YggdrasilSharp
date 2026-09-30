using System.Text.Json.Serialization;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

/// <summary>
/// Represents the response body returned when a user successfully signed in.
/// </summary>
public class LoginResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the signed-in user.
    /// </summary>
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } 
    
    /// <summary>
    /// Gets or sets the session token issued for the signed-in user.
    /// </summary>
    [JsonPropertyName("Token")]
    public string? Token { get; set; } 
    
    /// <summary>
    /// Gets or sets the date and time at which the session token expires.
    /// </summary>
    [JsonPropertyName("Expires")]
    public string Expires { get; set; } 
}
