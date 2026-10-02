using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for invalidating an access token in the Yggdrasil API.
/// </summary>
public class YigInvalidateRequest
{
    /// <summary>
    /// Gets or sets the access token to be invalidated.
    /// This field is required.
    /// </summary>
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }
    
    [JsonPropertyName("clientToken")]
    public string? ClientToken { get; set; }
}
