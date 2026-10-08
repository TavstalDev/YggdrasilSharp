using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for joining a server in the Yggdrasil API.
/// </summary>
public class YigJoinServerRequest
{
    /// <summary>
    /// Gets or sets the access token used for authentication.
    /// This field is required.
    /// </summary>
    [Required]
    [StringLength(1024, MinimumLength = 32)]
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the UUID of the selected profile.
    /// This field is required.
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 32)]
    [JsonPropertyName("selectedProfile")]
    public required string SelectedProfile { get; set; }

    /// <summary>
    /// Gets or sets the server ID to join.
    /// This field is required.
    /// </summary>
    [Required]
    [JsonPropertyName("ServerId")]
    public required string ServerId { get; set; }
}
