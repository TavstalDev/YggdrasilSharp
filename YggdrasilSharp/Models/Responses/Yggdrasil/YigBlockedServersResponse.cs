using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;

/// <summary>
/// Represents the response body returned by the blocked servers endpoint.
/// </summary>
public class YigBlockedServersResponse
{
    /// <summary>
    /// Gets or sets the list of servers that are blocked from multiplayer play,
    /// as configured in <c>Yggdrasil:BlockedServers</c>.
    /// </summary>
    [JsonPropertyName("blockedServers")]
    public required string[] BlockedServers { get; set; } = [];
}
