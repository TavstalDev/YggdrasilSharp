using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for refreshing an access token in the Yggdrasil API.
/// </summary>
public class YigRefreshRequest
{
    /// <summary>
    /// Gets or sets the access token to be refreshed.
    /// This field is required.
    /// </summary>
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the client token associated with the session.
    /// This field is optional.
    /// </summary>
    [JsonPropertyName("clientToken")]
    public string? ClientToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include user information in the response.
    /// </summary>
    [JsonPropertyName("requestUser")]
    public bool RequestUser { get; set; }

    /// <summary>
    /// Gets or sets the selected profile for the request.
    /// </summary>
    [JsonPropertyName("selectedProfile")]
    public required YigProfileBody SelectedProfile { get; set; }
}
