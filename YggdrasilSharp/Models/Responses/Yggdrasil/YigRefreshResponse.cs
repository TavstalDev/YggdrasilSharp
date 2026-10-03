using System.Text.Json.Serialization;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

namespace Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;

/// <summary>
/// Represents the response body returned by a successful Yggdrasil token refresh request.
/// </summary>
public class YigRefreshResponse
{
    /// <summary>
    /// Gets or sets the reissued access token.
    /// </summary>
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the client token bound to the session.
    /// </summary>
    [JsonPropertyName("clientToken")]
    public required string ClientToken { get; set; }

    /// <summary>
    /// Gets or sets the profile selected for the session.
    /// </summary>
    [JsonPropertyName("selectedProfile")]
    public required YigProfileBody SelectedProfile { get; set; }
}
