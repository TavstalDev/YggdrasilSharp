using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for signing out in the Yggdrasil API.
/// </summary>
public class YigSignoutRequest
{
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
}
