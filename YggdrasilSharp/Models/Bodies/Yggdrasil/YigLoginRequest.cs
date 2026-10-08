using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for logging in to the Yggdrasil API.
/// </summary>
public class YigLoginRequest
{
    /// <summary>
    /// Gets or sets information about the client performing the login.
    /// This field is required.
    /// </summary>
    [JsonPropertyName("agent")]
    public required YigAgent Agent { get; set; }

    /// <summary>
    /// Gets or sets the username of the user.
    /// This field is required.
    /// </summary>
    [JsonPropertyName("username")]
    [StringLength(16, MinimumLength = 3)]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the password of the user.
    /// This field is required.
    /// </summary>
    [JsonPropertyName("password")]
    [StringLength(64, MinimumLength = 8)]
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the client token for the session.
    /// This field is optional.
    /// </summary>
    [JsonPropertyName("clientToken")]
    [StringLength(36, MinimumLength = 32)]
    public string? ClientToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include user information in the response.
    /// </summary>
    [JsonPropertyName("requestUser")]
    public bool RequestUser { get; set; }
}
